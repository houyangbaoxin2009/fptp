using System.Reflection;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Plugins;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Fptm.Workflow;
using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 工作流与滤镜集成测试：
/// - fpter 滤镜经真实 ALC 反射实例化，对 4x4 测试图运行灰度/动漫，验证像素/尺寸；
/// - fptm 工作流（换底色/智能裁切/排版）为算法类，直接实例化验证（不注册为滤镜，
///   不出现在滤镜窗口）。换底/排版代码迁移自原 Builtin，测试保留验证迁移未破坏。
/// </summary>
[Collection("Plugins")]
public class FilterIntegrationTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-filter-tests", Guid.NewGuid().ToString("N"));

    public FilterIntegrationTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
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

    /// <summary>加载 fpter 模块并从滤镜表取指定 Id 的滤镜（真实 ALC 路径）。</summary>
    private static IFilterProcessor GetFilter(string filterId)
    {
        ModuleRegistry registry = new(
            Path.Combine(Path.GetTempPath(), "fptp-filter-tests", Guid.NewGuid().ToString("N"), "modules.json"),
            Path.Combine(Path.GetTempPath(), "fptp-filter-tests", Guid.NewGuid().ToString("N"), "settings.json"),
            Path.Combine(Path.GetTempPath(), "fptp-filter-tests", Guid.NewGuid().ToString("N"), "secure.json"),
            new JsonConfigStore());
        var context = new TestHostContext();
        ModuleLoader.LoadFromDirectory(PluginsBinLocator.Path, registry, context, null);

        Assembly? assembly = null;
        foreach (var alc in System.Runtime.Loader.AssemblyLoadContext.All)
        {
            if (alc.Name is null || !alc.Name.StartsWith("osiris-module:", StringComparison.Ordinal))
                continue;
            assembly = alc.Assemblies.FirstOrDefault(a => a.GetName().Name == "Fpter");
            if (assembly is not null)
                break;
        }
        Assert.NotNull(assembly);

        Type? pluginType = assembly!.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<PluginExportAttribute>() is not null);
        Assert.NotNull(pluginType);

        var plugin = Activator.CreateInstance(pluginType!);
        var filters = (IReadOnlyList<IFilterProcessor>?)pluginType!.GetProperty("Filters")?.GetValue(plugin);
        Assert.NotNull(filters);

        IFilterProcessor? filter = filters!.FirstOrDefault(f => f.Id == filterId);
        Assert.NotNull(filter);
        return filter!;
    }

    [Fact]
    public void Grayscale_RedPixel_BecomesEqualChannels()
    {
        // 意图：灰度滤镜按 BT.601 亮度公式把红(255,0,0) 转灰 → R==G==B==76。
        IFilterProcessor filter = GetFilter("fpter.grayscale");
        PixelSurface input = FillSurface(4, 4, b: 0, g: 0, r: 255, a: 255); // 不透明红

        PixelSurface output = filter.Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.Equal(4, output.Width);
        Assert.Equal(4, output.Height);
        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(76, pixel[0]); // B == gray
        Assert.Equal(76, pixel[1]); // G == gray
        Assert.Equal(76, pixel[2]); // R == gray
        Assert.Equal(255, pixel[3]); // alpha 不变
    }

    [Fact]
    public void Anime_4x4Output_SizeUnchanged()
    {
        // 意图：动漫模式只改像素不改尺寸，输出与输入同尺寸。
        IFilterProcessor filter = GetFilter("fpter.anime");
        PixelSurface input = FillSurface(4, 4, b: 128, g: 128, r: 128, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.Equal(input.Width, output.Width);
        Assert.Equal(input.Height, output.Height);
    }

    // ---- fptm 工作流（迁移自原 Builtin 滤镜，验证迁移未破坏）----

    [Fact]
    public void ReplaceBackground_BlueImage_Tolerance200_TurnsRed()
    {
        // 意图：蓝底图（容差 200 覆盖任意蓝色偏差）经换底色工作流 → 输出变红底。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255); // 不透明蓝
        var parameters = new FilterParameters
        {
            ["color"] = 0xFFFF0000u, // 目标色：不透明红
            ["tolerance"] = 200,
        };

        PixelSurface output = new BackgroundReplace().Apply(input, parameters, progress: null, CancellationToken.None);

        Assert.Equal(4, output.Width);
        Assert.Equal(4, output.Height);
        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, pixel); // 红色（B=0,G=0,R=255,A=255）
    }

    [Fact]
    public void ReplaceBackground_SolidBlue_FeatherKeepsSaturatedColor()
    {
        // 意图：羽化只影响边缘过渡，纯蓝底（容差 200，羽化 10）仍整幅替换为目标色且不透明。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255);
        var parameters = new FilterParameters
        {
            ["color"] = 0xFFFF0000u,
            ["tolerance"] = 200,
            ["feather"] = 10,
        };

        PixelSurface output = new BackgroundReplace().Apply(input, parameters, progress: null, CancellationToken.None);

        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, pixel);
    }

    [Fact]
    public void ReplaceBackground_BackgroundImage_OverridesSolidColor()
    {
        // 意图：提供背景图片（背景像素面）时，背景像素被图片采样填充而非纯色。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255); // 蓝底
        PixelSurface bgImage = FillSurface(8, 8, b: 0, g: 255, r: 0, a: 255); // 绿背景图
        var parameters = new FilterParameters
        {
            ["tolerance"] = 200,
            ["background"] = bgImage,
        };

        PixelSurface output = new BackgroundReplace().Apply(input, parameters, progress: null, CancellationToken.None);

        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(new byte[] { 0, 255, 0, 255 }, pixel); // 绿背景图（R=0,G=255,B=0）
    }

    [Fact]
    public void SmartCrop_4x4Output_WidthShrinks()
    {
        // 意图：4x4 源图偏宽（4 > 4*35/45），智能裁切输出 3x4——输出宽小于输入宽。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 255, r: 255, a: 255);
        PixelSurface output = new SmartCrop().Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.True(output.Width < input.Width, $"输出宽 {output.Width} 应小于输入宽 {input.Width}");
        Assert.Equal(3, output.Width);
        Assert.Equal(4, output.Height);
    }

    [Fact]
    public void SmartCrop_OneInchPreset_ScalesTo295x413()
    {
        // 意图：尺寸预设 1（1寸）把任意比例源图按 295×413 输出（裁切 + 双线性缩放）。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 255, r: 255, a: 255);
        var parameters = new FilterParameters { ["sizePreset"] = 1 };

        PixelSurface output = new SmartCrop().Apply(input, parameters, progress: null, CancellationToken.None);

        Assert.Equal(295, output.Width);
        Assert.Equal(413, output.Height);
    }

    [Fact]
    public void LayoutComposer_5InchPaper_ReturnsPaperSizedSurface()
    {
        // 意图：排版把照片排到 5 寸相纸（1500×1050），返回相纸尺寸像素面，网格行列 ≥1。
        PixelSurface photo = FillSurface(300, 400, b: 255, g: 255, r: 255, a: 255);
        PixelSurface? paper = LayoutComposer.Compose(photo, "5寸", out int cols, out int rows);

        Assert.NotNull(paper);
        Assert.Equal(1500, paper!.Width);
        Assert.Equal(1050, paper.Height);
        Assert.True(cols >= 1);
        Assert.True(rows >= 1);
    }

    // ---- 新增常用滤镜（fpter）----

    [Theory]
    [InlineData("fpter.invert")]
    [InlineData("fpter.brightnessContrast")]
    [InlineData("fpter.saturation")]
    [InlineData("fpter.blur")]
    [InlineData("fpter.sharpen")]
    [InlineData("fpter.sepia")]
    public void NewFilters_RegisterInModule_AndKeepSize(string filterId)
    {
        // 意图：6 个新滤镜全部登记进 fpter 模块滤镜表（滤镜窗口/菜单可见），应用后尺寸不变。
        IFilterProcessor filter = GetFilter(filterId);
        PixelSurface input = FillSurface(4, 4, b: 128, g: 128, r: 128, a: 255);

        PixelSurface output = filter.Apply(input, filter.Defaults, progress: null, CancellationToken.None);

        Assert.Equal(input.Width, output.Width);
        Assert.Equal(input.Height, output.Height);
        Assert.NotNull(filter.Id);
    }

    [Fact]
    public void Invert_WhitePixel_BecomesBlack()
    {
        // 意图：反色把不透明白（255,255,255）转黑（0,0,0），Alpha 不变。
        IFilterProcessor filter = GetFilter("fpter.invert");
        PixelSurface input = FillSurface(4, 4, b: 255, g: 255, r: 255, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.Equal(new byte[] { 0, 0, 0, 255 }, output.Row(0)[0..4].ToArray());
    }

    [Fact]
    public void BrightnessContrast_NeutralParams_Unchanged()
    {
        // 意图：亮度/对比度均为 0（恒等）时输出与输入逐像素一致。
        IFilterProcessor filter = GetFilter("fpter.brightnessContrast");
        PixelSurface input = FillSurface(4, 4, b: 128, g: 128, r: 128, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters { ["brightness"] = 0, ["contrast"] = 0 },
            progress: null, CancellationToken.None);

        Assert.Equal(new byte[] { 128, 128, 128, 255 }, output.Row(0)[0..4].ToArray());
    }

    [Fact]
    public void Saturation_Zero_RedTurnsGray()
    {
        // 意图：饱和度 0 时红色去饱和为亮度灰（BT.601：R=255 → luma=76）。
        IFilterProcessor filter = GetFilter("fpter.saturation");
        PixelSurface input = FillSurface(4, 4, b: 0, g: 0, r: 255, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters { ["saturation"] = 0 },
            progress: null, CancellationToken.None);

        Assert.Equal(new byte[] { 76, 76, 76, 255 }, output.Row(0)[0..4].ToArray());
    }

    [Fact]
    public void Sepia_RedPixel_TurnsWarmTone()
    {
        // 意图：怀旧完全强度下红像素按 sepia 矩阵变换（B=69 < G=89 < R=100，波浪边缘），不再纯红。
        IFilterProcessor filter = GetFilter("fpter.sepia");
        PixelSurface input = FillSurface(4, 4, b: 0, g: 0, r: 255, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters { ["strength"] = 100 },
            progress: null, CancellationToken.None);

        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(69, pixel[0]); // B
        Assert.Equal(89, pixel[1]); // G
        Assert.Equal(100, pixel[2]); // R（红通道降低，不再 255）
        Assert.Equal(255, pixel[3]);
    }

    // ---- 红眼去除（fptm 工作流）----

    [Fact]
    public void RedEye_RedPupil_RedChannelReduced()
    {
        // 意图：高红像素（R=255,G=0,B=0）红眼去除后红通道显著降低（去红），其余通道不变。
        PixelSurface input = FillSurface(4, 4, b: 0, g: 0, r: 255, a: 255);
        var parameters = new FilterParameters { ["tolerance"] = 60, ["strength"] = 80 };

        PixelSurface output = new RedEyeRemove().Apply(input, parameters, progress: null, CancellationToken.None);

        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.True(pixel[2] < 100, $"红通道应被压暗（当前 {pixel[2]}）");
        Assert.Equal(new byte[] { 0, 0, pixel[2], 255 }, pixel);
    }

    [Fact]
    public void RedEye_BluePixel_Unchanged()
    {
        // 意图：非红像素（蓝）不满足红光判定，原样保持。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255); // 蓝（redScore<0）
        var parameters = new FilterParameters { ["tolerance"] = 60, ["strength"] = 80 };

        PixelSurface output = new RedEyeRemove().Apply(input, parameters, progress: null, CancellationToken.None);

        Assert.Equal(new byte[] { 255, 0, 0, 255 }, output.Row(0)[0..4].ToArray());
    }

    // ---- 拼版模板（fptm LayoutComposer）----

    [Fact]
    public void LayoutComposer_Template1Inchx8_FixedGrid()
    {
        // 意图：拼版模板「6寸·1寸×8」固定 4 列×2 行，照片缩放到 1 寸（295×413）排到 6 寸相纸（1800×1200）。
        PixelSurface photo = FillSurface(300, 400, b: 255, g: 255, r: 255, a: 255);

        PixelSurface? paper = LayoutComposer.Compose(photo, "6寸·1寸×8", out int cols, out int rows);

        Assert.NotNull(paper);
        Assert.Equal(1800, paper!.Width);
        Assert.Equal(1200, paper.Height);
        Assert.Equal(4, cols);
        Assert.Equal(2, rows);
    }

    [Fact]
    public void LayoutComposer_Template2Inchx4_FixedGrid()
    {
        // 意图：拼版模板「6寸·2寸×4」固定 2 列×2 行。
        PixelSurface photo = FillSurface(300, 400, b: 255, g: 255, r: 255, a: 255);

        PixelSurface? paper = LayoutComposer.Compose(photo, "6寸·2寸×4", out int cols, out int rows);

        Assert.NotNull(paper);
        Assert.Equal(2, cols);
        Assert.Equal(2, rows);
    }

    // ---- 一键证件照（fptm IdPhotoWizard）----

    [Fact]
    public void IdPhotoWizard_NoLayout_ReturnsCroppedReplacedPhoto()
    {
        // 意图：不排版时一键生成 = 智能裁切（1寸）→ 换底色（蓝底→红底），宽高 295×413、像素变红。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255); // 蓝底
        var options = new IdPhotoWizardOptions(
            PresetIndex: 1, Color: 0xFFFF0000u, Tolerance: 200, Feather: 0,
            Paper: IdPhotoWizard.NoLayoutPaper, CustomW: 0, CustomH: 0, Guides: false);

        PixelSurface result = IdPhotoWizard.Run(input, options, progress: null, CancellationToken.None);

        Assert.Equal(295, result.Width);
        Assert.Equal(413, result.Height);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, result.Row(0)[0..4].ToArray()); // 红底
    }

    [Fact]
    public void IdPhotoWizard_Template_ComposesPaper()
    {
        // 意图：向导带拼版模板时输出 6 寸相纸（1800×1200）。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255);
        var options = new IdPhotoWizardOptions(
            PresetIndex: 1, Color: 0xFFFF0000u, Tolerance: 200, Feather: 0,
            Paper: "6寸·1寸×8", CustomW: 0, CustomH: 0, Guides: false);

        PixelSurface result = IdPhotoWizard.Run(input, options, progress: null, CancellationToken.None);

        Assert.Equal(1800, result.Width);
        Assert.Equal(1200, result.Height);
    }

    [Fact]
    public void IdPhotoWizard_UnknownPaper_FallsBackToReplaced()
    {
        // 意图：未知相纸名称时排版失败 → 回退已换底/裁切结果（不崩）。
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255);
        var options = new IdPhotoWizardOptions(
            PresetIndex: 1, Color: 0xFFFF0000u, Tolerance: 200, Feather: 0,
            Paper: "不存在相纸", CustomW: 0, CustomH: 0, Guides: false);

        PixelSurface result = IdPhotoWizard.Run(input, options, progress: null, CancellationToken.None);

        Assert.Equal(295, result.Width);
        Assert.Equal(413, result.Height);
    }
}