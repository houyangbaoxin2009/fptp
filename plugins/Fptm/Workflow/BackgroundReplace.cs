using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;
using Osiris.Algorithms;

namespace Fptm.Workflow;

/// <summary>
/// 换底色工作流（原 Fptp.Plugins.Builtin.ReplaceBackgroundFilter 迁移而来）：
/// 四角采样背景色 → 按容差（曼哈顿距离）把接近背景色的像素替换为目标背景。
/// 含边缘羽化（feather）与背景图片（background，PixelSurface）支持。
/// 本类实现 IFilterProcessor 仅复用 Apply 签名；fptm **不注册为 IFilterPlugin**，
/// 故不出现在滤镜窗口，仅由 fptm 工作流命令/面板调用。
/// </summary>
public sealed class BackgroundReplace : IFilterProcessor
{
    /// <summary>参数键：目标颜色（uint PackBgra，默认蓝）。</summary>
    public const string ParamColor = "color";

    /// <summary>参数键：容差（0~200，默认 60）。</summary>
    public const string ParamTolerance = "tolerance";

    /// <summary>参数键：边缘羽化宽度（0~20，默认 3；0 关闭硬边）。</summary>
    public const string ParamFeather = "feather";

    /// <summary>参数键：背景图片（PixelSurface?；null 时用纯色 ParamColor）。</summary>
    public const string ParamBackground = "background";

    /// <summary>默认目标色：蓝色 0,0,255（直通色，PackBgra 值）。</summary>
    public static uint DefaultColor => PackBgra(0, 0, 255);

    /// <inheritdoc />
    public string Id => "fpter.replaceBackground";

    /// <inheritdoc />
    public string DisplayName => L10n.T("换底色");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamColor] = DefaultColor,
        [ParamTolerance] = 60,
        [ParamFeather] = 3,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new() { Key = ParamColor, Label = L10n.T("目标颜色"), Kind = FilterParameterKind.Color, DefaultValue = DefaultColor },
        new() { Key = ParamTolerance, Label = L10n.T("容差"), Kind = FilterParameterKind.Int, Min = 0, Max = 200, DefaultValue = 60 },
        new() { Key = ParamFeather, Label = L10n.T("边缘羽化"), Kind = FilterParameterKind.Int, Min = 0, Max = 20, DefaultValue = 3 },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        uint targetColor = parameters.Get(ParamColor, DefaultColor);
        int tolerance = Math.Clamp(parameters.Get(ParamTolerance, 60), 0, 200);
        double feather = Math.Clamp(parameters.Get(ParamFeather, 3), 0, 20.0);

        uint sample = SampleBackgroundColor(input);
        PixelSurface? backgroundImage = parameters.Get<PixelSurface?>(ParamBackground, null);
        uint targetPremul = PremultiplyColor(targetColor);

        int width = input.Width, height = input.Height;
        var editor = input.CreateEditor();

        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            Span<byte> row = editor.Row(y);
            int o = 0;
            for (int x = 0; x < width; x++, o += 4)
            {
                uint premul = ReadBgra(row, o);
                if (ColorUtil.A((int)premul) == 0)
                    continue; // 全透明像素：原样保持

                uint straight = (uint)ColorUtil.Unpremultiply((int)premul);
                int distance = Manhattan(straight, sample);
                double coverage = ComputeCoverage(distance, tolerance, feather);
                if (coverage <= 0)
                    continue; // 完全前景

                uint bgPremul = backgroundImage is not null
                    ? SampleBackground(backgroundImage, x, y, width, height)
                    : targetPremul;

                int r = Blend(ColorUtil.R((int)premul), ColorUtil.R((int)bgPremul), coverage);
                int g = Blend(ColorUtil.G((int)premul), ColorUtil.G((int)bgPremul), coverage);
                int b = Blend(ColorUtil.B((int)premul), ColorUtil.B((int)bgPremul), coverage);
                int a = Blend(ColorUtil.A((int)premul), ColorUtil.A((int)bgPremul), coverage);

                row[o] = (byte)b;
                row[o + 1] = (byte)g;
                row[o + 2] = (byte)r;
                row[o + 3] = (byte)a;
            }
        }

        editor.MarkAllDirty();
        progress?.Report(100, L10n.T("换底色完成"));
        return editor.Commit();
    }

    private static double ComputeCoverage(int distance, int tolerance, double feather)
    {
        if (feather <= 0)
            return distance < tolerance ? 1.0 : 0.0;
        if (distance < tolerance) return 1.0;
        if (distance >= tolerance + feather) return 0.0;
        return 1.0 - (distance - tolerance) / feather;
    }

    private static int Blend(int src, int bg, double coverage)
        => (int)Math.Round(src * (1.0 - coverage) + bg * coverage);

    private static uint SampleBackground(PixelSurface image, int x, int y, int width, int height)
    {
        double scale = Math.Max((double)width / image.Width, (double)height / image.Height);
        int sx = Math.Clamp((int)((x / (double)width) * image.Width * scale), 0, image.Width - 1);
        int sy = Math.Clamp((int)((y / (double)height) * image.Height * scale), 0, image.Height - 1);
        ReadOnlySpan<byte> row = image.Row(sy);
        int o = sx * 4;
        return ReadBgra(row, o);
    }

    private static uint SampleBackgroundColor(PixelSurface input)
    {
        ReadOnlySpan<byte> topLeft = input.Row(0);
        ReadOnlySpan<byte> bottomLeft = input.Row(input.Height - 1);

        uint c0 = ReadBgra(topLeft, 0);
        uint c1 = ReadBgra(topLeft, (input.Width - 1) * 4);
        uint c2 = ReadBgra(bottomLeft, 0);
        uint c3 = ReadBgra(bottomLeft, (input.Width - 1) * 4);
        return MostCommonCorner(
            Unpremul(c0), Unpremul(c1), Unpremul(c2), Unpremul(c3));
    }

    private static uint Unpremul(uint c) => (uint)ColorUtil.Unpremultiply((int)c);

    private static uint MostCommonCorner(uint c0, uint c1, uint c2, uint c3)
    {
        int d0 = Manhattan(c0, c1) + Manhattan(c0, c2) + Manhattan(c0, c3);
        int d1 = Manhattan(c1, c0) + Manhattan(c1, c2) + Manhattan(c1, c3);
        int d2 = Manhattan(c2, c0) + Manhattan(c2, c1) + Manhattan(c2, c3);
        int d3 = Manhattan(c3, c0) + Manhattan(c3, c1) + Manhattan(c3, c2);
        int min = Math.Min(Math.Min(d0, d1), Math.Min(d2, d3));
        if (min == d0) return c0;
        if (min == d1) return c1;
        if (min == d2) return c2;
        return c3;
    }

    private static int Manhattan(uint a, uint b)
        => Math.Abs(ColorUtil.R((int)a) - ColorUtil.R((int)b))
         + Math.Abs(ColorUtil.G((int)a) - ColorUtil.G((int)b))
         + Math.Abs(ColorUtil.B((int)a) - ColorUtil.B((int)b));

    /// <summary>打包 BGRA 为 uint（低位=蓝，0xAARRGGBB），参数为 R/G/B/A 字节。</summary>
    private static uint PackBgra(byte r, byte g, byte b, byte a = 255)
        => (uint)(b | (g << 8) | (r << 16) | (a << 24));

    private static uint PremultiplyColor(uint bgra)
    {
        byte a = ColorUtil.A((int)bgra);
        if (a == 255) return bgra;
        return (uint)ColorUtil.Premultiply((int)bgra);
    }

    private static uint ReadBgra(ReadOnlySpan<byte> row, int offset)
        => (uint)(row[offset] | (row[offset + 1] << 8) | (row[offset + 2] << 16) | (row[offset + 3] << 24));
}