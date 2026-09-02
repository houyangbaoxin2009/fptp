using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Osiris.Core.Tie;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// tie 脚本模块测试（P5 宿主原生加载 tie 模块）：
/// - TieRunner 端到端：真实 tiec 编译运行 tie 脚本（import fptp_sdk.tie），像素数组文本亮度变换往返；
/// - TieModuleAdapter：脚本模块经 ModuleLoader 加载 → 贡献脚本滤镜 → Apply 重建像素面；
/// - 协议（fptp_sdk.tie 滤镜桥）：输入/输出均为 tie:data 文本（pixels 为 BGRA 字节逗号分隔）。
/// tiec.exe 随测试输出 tools/tie/ 分发（模拟宿主随包路径）。
/// </summary>
public class TieModuleTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "osiris-tie-tests", Guid.NewGuid().ToString("N"));

    public TieModuleTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    /// <summary>tie 脚本滤镜模块的 main.tie（v2 行帧桥：亮度增强 RGB 加 delta，Alpha 不变；参数自描述探测）。</summary>
    private const string BrightnessMainTie = """
        type tie<logic>

        import "fptp_sdk.tie" as fptp

        func process(src: string) -> string {
            if fptp.data_get(src, "action", "") == "params" {
                return "params\n" + fptp.param_int("delta", "亮度增量", -255, 255, 20)
            }
            var delta = fptp.data_get_int(src, "delta", 20)
            var pixels = fptp.data_get(src, "pixels", "")
            return fptp.data_make("pixels", fptp.pixel_add(pixels, delta))
        }

        func main() {
            fptp.bridge(process)
        }
        """;

    /// <summary>脚本模块清单（tie:data 角色，type=script + language=tie + entryPoint=main.tie）。</summary>
    private const string ScriptManifestTieData = """
        type tie<data>

        [
            "id": "tie.probe",
            "name": "tie 探头模块",
            "version": "1.0.0",
            "kind": "extension",
            "type": "script",
            "language": "tie",
            "entryPoint": "main.tie",
            "minHostVersion": "1.0.0",
        ]
        """;

    /// <summary>写临时 tie 模块目录：main.tie + fptp_sdk.tie + std/tink.tie + rdu/crc.tie（从测试输出复制）。</summary>
    private string WriteTieModule(string name, string mainTie)
    {
        string dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "main.tie"), mainTie, new System.Text.UTF8Encoding(false));

        // v2 行帧桥资源随测试输出分发（Link=tie/...）：fptp_sdk.tie + std/rdu（tink 帧层），
        // 复制到模块目录供 import（tiec 按 CWD=插件目录解析）
        string tieRoot = Path.Combine(AppContext.BaseDirectory, "tie");
        Assert.True(File.Exists(Path.Combine(tieRoot, "fptp_sdk.tie")), "未找到测试分发的 fptp_sdk.tie");
        File.Copy(Path.Combine(tieRoot, "fptp_sdk.tie"), Path.Combine(dir, "fptp_sdk.tie"), overwrite: true);
        Directory.CreateDirectory(Path.Combine(dir, "std"));
        Directory.CreateDirectory(Path.Combine(dir, "rdu"));
        File.Copy(Path.Combine(tieRoot, "std", "tink.tie"), Path.Combine(dir, "std", "tink.tie"), overwrite: true);
        File.Copy(Path.Combine(tieRoot, "rdu", "crc.tie"), Path.Combine(dir, "rdu", "crc.tie"), overwrite: true);
        return dir;
    }

    /// <summary>构造全图填色的像素面（BGRA 预乘字节）。</summary>
    private static PixelSurface FillSurface(int width, int height, byte b, byte g, byte r, byte a)
    {
        PixelSurfaceEditor editor = PixelSurface.Create(width, height).CreateEditor();
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i + 3 < pixels.Length; i += 4)
        {
            pixels[i] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
            pixels[i + 3] = a;
        }
        return editor.Commit();
    }

    [Fact]
    public void TieRunner_亮度脚本_像素文本往返()
    {
        // 意图：真实 tiec 编译运行亮度脚本（fptp_sdk.tie pixel_add），输入协议文本 → 输出加 delta 的像素文本。
        if (Tie.TieRunner.FindTiec() is null)
            return;   // tiec 未随包分发时跳过

        string dir = WriteTieModule("runner", BrightnessMainTie);
        string input = "[\"width\": 2, \"height\": 2, \"delta\": 10, \"pixels\": \"100,101,102,255,200,201,202,255,50,51,52,255,0,1,2,255\"]";

        TieResult result = Tie.TieRunner.Run(Path.Combine(dir, "main.tie"), input);

        Assert.True(result.Ok, result.Message);
        Assert.Equal("[\"pixels\": \"110,111,112,255,210,211,212,255,60,61,62,255,10,11,12,255\"]", result.Output);
    }

    [Fact]
    public void TieModuleAdapter_模块加载_脚本滤镜应用()
    {
        // 意图：ModuleLoader 加载 tie 脚本模块（module.data.tie type=script）→ GetInstances 得 TieModuleAdapter →
        // 其脚本滤镜 Apply 像素面（BGRA 不透明），每个通道 +delta（Alpha 不变）重建。
        if (Tie.TieRunner.FindTiec() is null)
            return;   // tiec 未随包分发时跳过

        string moduleDir = WriteTieModule("module", BrightnessMainTie);
        File.WriteAllText(Path.Combine(moduleDir, "module.data.tie"), ScriptManifestTieData,
            new System.Text.UTF8Encoding(false));

        var registry = new ModuleRegistry(
            Path.Combine(_tempDir, "modules.data.tie"),
            Path.Combine(_tempDir, "settings.data.tie"),
            Path.Combine(_tempDir, "secure.data.tie"),
            new TieDataConfigStore());
        int loaded = ModuleLoader.LoadFromDirectory(_tempDir, registry, new StubHost(),
            (name, ex) => throw new InvalidOperationException($"模块加载失败 [{name}]: {ex.Message}"));

        Assert.Equal(1, loaded);
        var adapter = Assert.Single(registry.GetInstances().OfType<TieModuleAdapter>());
        Assert.Equal("tie.probe", adapter.Id);

        IFilterProcessor filter = Assert.Single(adapter.Filters);
        Assert.Equal("tie.probe.script", filter.Id);

        // 2×2 不透明像素，delta 默认 20 → 每通道 +20
        PixelSurface input = FillSurface(2, 2, b: 100, g: 100, r: 100, a: 255);
        PixelSurface output = filter.Apply(input, filter.Defaults, progress: null, CancellationToken.None);

        Assert.Equal(2, output.Width);
        Assert.Equal(2, output.Height);
        Assert.Equal(new byte[] { 120, 120, 120, 255 }, output.Row(0)[0..4].ToArray());
        Assert.Equal(new byte[] { 120, 120, 120, 255 }, output.Row(1)[0..4].ToArray());
    }

    [Fact]
    public void TieModuleAdapter_脚本声明参数_宿主动态生成()
    {
        // 意图：脚本经 param_int 自描述参数（探测帧 ["action": "params"]）→ 宿主动态生成
        // Parameters/Defaults；Apply 把用户调参后的真实值并入输入协议文本（delta=10 → 110）。
        if (Tie.TieRunner.FindTiec() is null)
            return;

        string moduleDir = WriteTieModule("params", BrightnessMainTie);
        File.WriteAllText(Path.Combine(moduleDir, "module.data.tie"), ScriptManifestTieData,
            new System.Text.UTF8Encoding(false));

        var registry = new ModuleRegistry(
            Path.Combine(_tempDir, "modules.data.tie"),
            Path.Combine(_tempDir, "settings.data.tie"),
            Path.Combine(_tempDir, "secure.data.tie"),
            new TieDataConfigStore());
        int loaded = ModuleLoader.LoadFromDirectory(_tempDir, registry, new StubHost(), null);
        Assert.Equal(1, loaded);

        var adapter = Assert.Single(registry.GetInstances().OfType<TieModuleAdapter>());
        IFilterProcessor filter = Assert.Single(adapter.Filters);

        // 探测 → 动态参数声明（Int、范围 -255~255、默认 20）
        FilterParameterDescriptor delta = Assert.Single(filter.Parameters);
        Assert.Equal("delta", delta.Key);
        Assert.Equal(FilterParameterKind.Int, delta.Kind);
        Assert.Equal(-255, delta.Min);
        Assert.Equal(255, delta.Max);
        Assert.Equal(20, delta.DefaultValue);
        Assert.Equal(20, filter.Defaults.Get<int>("delta", -1));   // 默认参数同步生成

        // Apply 传入用户调参后的参数（delta=10）→ 运行时并入输入协议文本 → 100+10=110
        PixelSurface input = FillSurface(2, 2, b: 100, g: 100, r: 100, a: 255);
        FilterParameters userParams = new() { [delta.Key] = 10 };
        PixelSurface output = filter.Apply(input, new FilterParameters().Merge(userParams), progress: null, CancellationToken.None);

        Assert.Equal(new byte[] { 110, 110, 110, 255 }, output.Row(0)[0..4].ToArray());
        Assert.Equal(new byte[] { 110, 110, 110, 255 }, output.Row(1)[0..4].ToArray());
    }

    [Fact]
    public void TieRunner_编译缓存_复用与失效()
    {
        // 意图：同一脚本树重复 Run 命中产物缓存（不重编译，结果正确）；
        // 脚本内容变更 → 缓存键变化 → 自动重编译（输出跟随新逻辑）。
        if (Tie.TieRunner.FindTiec() is null)
            return;

        string dir = WriteTieModule("cache", BrightnessMainTie);
        string input = "[\"width\": 1, \"height\": 1, \"delta\": 5, \"pixels\": \"100,100,100,255\"]";

        // 首次：编译入缓存
        TieResult r1 = Tie.TieRunner.Run(Path.Combine(dir, "main.tie"), input);
        Assert.True(r1.Ok, r1.Message);
        Assert.Contains("105,105,105,255", r1.Output);

        // 二次：脚本树未变 → 缓存命中，改输入参数仍正确（delta 10 → 110）
        TieResult r2 = Tie.TieRunner.Run(Path.Combine(dir, "main.tie"),
            "[\"width\": 1, \"height\": 1, \"delta\": 10, \"pixels\": \"100,100,100,255\"]");
        Assert.True(r2.Ok, r2.Message);
        Assert.Contains("110,110,110,255", r2.Output);

        // 篡改脚本（亮度加固定 0）→ 内容哈希变化 → 重编译，输出跟随新逻辑
        File.WriteAllText(Path.Combine(dir, "main.tie"),
            BrightnessMainTie.Replace("fptp.pixel_add(pixels, delta)", "fptp.pixel_add(pixels, 0)"),
            new System.Text.UTF8Encoding(false));
        TieResult r3 = Tie.TieRunner.Run(Path.Combine(dir, "main.tie"), input);
        Assert.True(r3.Ok, r3.Message);
        Assert.Contains("100,100,100,255", r3.Output);
    }

    [Fact]
    public void TieModuleAdapter_超大图_原样返回()
    {
        // 意图：像素数超过 MaxPixels（v2 4_000_000）时脚本桥不执行，原样返回（不崩、尺寸不变）。
        if (Tie.TieRunner.FindTiec() is null)
            return;

        string moduleDir = WriteTieModule("big", BrightnessMainTie);
        File.WriteAllText(Path.Combine(moduleDir, "module.data.tie"), ScriptManifestTieData,
            new System.Text.UTF8Encoding(false));

        var registry = new ModuleRegistry(
            Path.Combine(_tempDir, "modules.data.tie"),
            Path.Combine(_tempDir, "settings.data.tie"),
            Path.Combine(_tempDir, "secure.data.tie"),
            new TieDataConfigStore());
        ModuleLoader.LoadFromDirectory(_tempDir, registry, new StubHost(), null);

        var adapter = Assert.Single(registry.GetInstances().OfType<TieModuleAdapter>());
        IFilterProcessor filter = Assert.Single(adapter.Filters);

        PixelSurface input = FillSurface(2049, 2049, b: 100, g: 100, r: 100, a: 255);   // 2049² > 4M
        PixelSurface output = filter.Apply(input, filter.Defaults, progress: null, CancellationToken.None);

        Assert.Equal(2049, output.Width);
        Assert.Equal(2049, output.Height);
        Assert.Equal(new byte[] { 100, 100, 100, 255 }, output.Row(0)[0..4].ToArray());   // 原样
    }

    /// <summary>测试宿主上下文（无 UI/文档；Ui=null 使模块跳过 UI 注册）。</summary>
    private sealed class StubHost : IHostContext
    {
        public StubHost() => Services = new ServiceRegistry();

        public Osiris.Abstractions.Document.OsirisDocument? ActiveDocument => null;
        public IServiceRegistry Services { get; }
        public Osiris.Abstractions.Ui.IUiService? Ui => null;
        public IProgress Report => new NullProgress();

        private sealed class NullProgress : IProgress
        {
            public void Report(double percent, string message)
            {
                // 测试不关心进度
            }
        }
    }
}