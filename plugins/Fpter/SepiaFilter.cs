using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 怀旧（棕褐）滤镜：直通色空间按经典 sepia 矩阵变换后与原始颜色按强度混合。
/// 参数：strength(0~100 默认 100，0=不变，100=完全怀旧)。
/// </summary>
public sealed class SepiaFilter : IFilterProcessor
{
    /// <summary>参数键：怀旧强度（0~100）。</summary>
    public const string ParamStrength = "strength";

    /// <inheritdoc />
    public string Id => "fpter.sepia";

    /// <inheritdoc />
    public string DisplayName => L10n.T("怀旧");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamStrength] = 100,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamStrength,
            Label = L10n.T("怀旧强度"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 100,
            DefaultValue = 100,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int strength = Math.Clamp(parameters.Get(ParamStrength, 100), 0, 100);

        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;
        int length = dst.Length;

        for (int i = 0; i + 3 < length; i += 4)
        {
            ct.ThrowIfCancellationRequested();

            byte alpha = dst[i + 3];
            if (alpha == 0)
                continue;

            uint premul = (uint)(dst[i] | (dst[i + 1] << 8) | (dst[i + 2] << 16) | (dst[i + 3] << 24));
            uint straight = PixelColor.Unpremultiply(premul);
            byte b = PixelColor.B(straight);
            byte g = PixelColor.G(straight);
            byte r = PixelColor.R(straight);

            // 经典 sepia 矩阵
            int sepiaR = (int)Math.Round(0.393 * r + 0.769 * g + 0.189 * b);
            int sepiaG = (int)Math.Round(0.349 * r + 0.686 * g + 0.168 * b);
            int sepiaB = (int)Math.Round(0.272 * r + 0.534 * g + 0.131 * b);

            uint adjusted = PixelColor.Premultiply(PixelColor.PackBgra(
                Mix(r, sepiaR), Mix(g, sepiaG), Mix(b, sepiaB), alpha));

            dst[i] = (byte)(adjusted & 0xFF);
            dst[i + 1] = (byte)((adjusted >> 8) & 0xFF);
            dst[i + 2] = (byte)((adjusted >> 16) & 0xFF);
        }

        progress?.Report(100, L10n.T("怀旧完成"));
        return editor.Commit();

        // 强度混合：out = src + (sepia - src) * strength / 100。
        byte Mix(byte src, int sepia)
            => (byte)Math.Clamp(src + (sepia - src) * strength / 100, 0, 255);
    }
}