using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 亮度对比度滤镜：直通色空间内先做对比度（经典因子公式）再叠加亮度偏移后重新预乘。
/// 参数：brightness(-100~100 默认 0，映射为 ±255 偏移)、contrast(-100~100 默认 0)。
/// </summary>
public sealed class BrightnessContrastFilter : IFilterProcessor
{
    /// <summary>参数键：亮度（-100~100）。</summary>
    public const string ParamBrightness = "brightness";

    /// <summary>参数键：对比度（-100~100）。</summary>
    public const string ParamContrast = "contrast";

    /// <inheritdoc />
    public string Id => "fpter.brightnessContrast";

    /// <inheritdoc />
    public string DisplayName => L10n.T("亮度对比度");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamBrightness] = 0,
        [ParamContrast] = 0,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamBrightness,
            Label = L10n.T("亮度"),
            Kind = FilterParameterKind.Int,
            Min = -100,
            Max = 100,
            DefaultValue = 0,
        },
        new()
        {
            Key = ParamContrast,
            Label = L10n.T("对比度"),
            Kind = FilterParameterKind.Int,
            Min = -100,
            Max = 100,
            DefaultValue = 0,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int brightness = Math.Clamp(parameters.Get(ParamBrightness, 0), -100, 100);
        int contrast = Math.Clamp(parameters.Get(ParamContrast, 0), -100, 100);

        // 对比度因子（经典公式，c=0 时 factor=1 恒等）：factor = 259*(c+255)/(255*(259-c))
        double factor = contrast == 0
            ? 1.0
            : (259.0 * (contrast + 255.0)) / (255.0 * (259.0 - contrast));
        int offset = brightness * 255 / 100;   // -255 ~ 255

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
            uint adjusted = PixelColor.Premultiply(PixelColor.PackBgra(
                Adjust(PixelColor.R(straight)), Adjust(PixelColor.G(straight)), Adjust(PixelColor.B(straight)), alpha));

            dst[i] = (byte)(adjusted & 0xFF);
            dst[i + 1] = (byte)((adjusted >> 8) & 0xFF);
            dst[i + 2] = (byte)((adjusted >> 16) & 0xFF);
        }

        progress?.Report(100, L10n.T("亮度对比度完成"));
        return editor.Commit();

        // 单通道调整：先对比度（以 128 为轴）再叠加亮度偏移，钳制到 [0,255]。
        byte Adjust(byte channel)
            => (byte)Math.Clamp((int)Math.Round(factor * (channel - 128.0) + 128.0 + offset), 0, 255);
    }
}