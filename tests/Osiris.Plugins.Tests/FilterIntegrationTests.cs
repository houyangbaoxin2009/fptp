using System.Reflection;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Plugins;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 滤镜集成测试：经真实 ALC 反射实例化 BuiltinPlugin，
/// 对 4x4 测试图运行 4 个内置滤镜，验证输出尺寸与像素/参数生效。
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

    /// <summary>加载插件并从滤镜表取指定 Id 的滤镜（真实 ALC 路径）。</summary>
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
            assembly = alc.Assemblies.FirstOrDefault(a => a.GetName().Name == "Fptp.Plugins.Builtin");
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
    public void ReplaceBackground_BlueImage_Tolerance200_TurnsRed()
    {
        // 意图：蓝底图（容差 200 覆盖任意蓝色偏差）经换底色滤镜 → 输出变红底；
        // 验证"color" 参数生效（蓝→红）。
        IFilterProcessor filter = GetFilter("fptp.replaceBackground");
        PixelSurface input = FillSurface(4, 4, b: 255, g: 0, r: 0, a: 255); // 不透明蓝
        var parameters = new FilterParameters
        {
            ["color"] = 0xFFFF0000u,      // 目标色：不透明红（0xAARRGGBB，低位=蓝）
            ["tolerance"] = 200,
        };

        PixelSurface output = filter.Apply(input, parameters, progress: null, CancellationToken.None);

        Assert.Equal(4, output.Width);
        Assert.Equal(4, output.Height);
        byte[] pixel = output.Row(0)[0..4].ToArray();
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, pixel); // 红色（B=0,G=0,R=255,A=255）
    }

    [Fact]
    public void Grayscale_RedPixel_BecomesEqualChannels()
    {
        // 意图：灰度滤镜按 BT.601 亮度公式把红(255,0,0) 转灰 → R==G==B==76。
        IFilterProcessor filter = GetFilter("fptp.grayscale");
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
    public void SmartCrop_4x4Output_WidthShrinks()
    {
        // 意图：4x4 源图偏宽（4 > 4*35/45），智能裁切输出 3x4——输出宽小于输入宽。
        IFilterProcessor filter = GetFilter("fptp.smartCrop");
        PixelSurface input = FillSurface(4, 4, b: 255, g: 255, r: 255, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.True(output.Width < input.Width, $"输出宽 {output.Width} 应小于输入宽 {input.Width}");
        Assert.Equal(3, output.Width);
        Assert.Equal(4, output.Height);
    }

    [Fact]
    public void Anime_4x4Output_SizeUnchanged()
    {
        // 意图：动漫模式只改像素不改尺寸，输出与输入同尺寸。
        IFilterProcessor filter = GetFilter("fptp.anime");
        PixelSurface input = FillSurface(4, 4, b: 128, g: 128, r: 128, a: 255);

        PixelSurface output = filter.Apply(input, new FilterParameters(), progress: null, CancellationToken.None);

        Assert.Equal(input.Width, output.Width);
        Assert.Equal(input.Height, output.Height);
    }
}
