using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 饱和度滤镜：直通色空间按亮度加权调节饱和度，v' = l + (v - l) * s / 100（s=100 恒等）。
/// 参数：saturation(0~200 默认 100，0=去色，200=加倍)。
/// </summary>
public sealed class SaturationFilter : IFilterProcessor
{
    /// <summary>参数键：饱和度（0~200，100 为原样）。</summary>
    public const string ParamSaturation = "saturation";

    /// <inheritdoc />
    public string Id => "fpter.saturation";

    /// <inheritdoc />
    public string DisplayName => L10n.T("饱和度");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamSaturation] = 100,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamSaturation,
            Label = L10n.T("饱和度"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 200,
            DefaultValue = 100,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int saturation = Math.Clamp(parameters.Get(ParamSaturation, 100), 0, 200);

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

            // BT.601 亮度
            int luma = (r * 299 + g * 587 + b * 114) / 1000;
            uint adjusted = PixelColor.Premultiply(PixelColor.PackBgra(
                Adjust(r, luma), Adjust(g, luma), Adjust(b, luma), alpha));

            dst[i] = (byte)(adjusted & 0xFF);
            dst[i + 1] = (byte)((adjusted >> 8) & 0xFF);
            dst[i + 2] = (byte)((adjusted >> 16) & 0xFF);
        }

        progress?.Report(100, L10n.T("饱和度完成"));
        return editor.Commit();

        // 单通道：v' = l + (v - l) * s / 100，钳制 [0,255]。
        byte Adjust(byte channel, int luma)
            => (byte)Math.Clamp(luma + (channel - luma) * saturation / 100, 0, 255);
    }
}