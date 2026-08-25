using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 反色滤镜：RGB 各通道取反（直通色空间 255 - c 后重新预乘，保持 Alpha 不变），
/// 透明像素（alpha=0）原样保持。无参数。
/// </summary>
public sealed class InvertFilter : IFilterProcessor
{
    /// <inheritdoc />
    public string Id => "fpter.invert";

    /// <inheritdoc />
    public string DisplayName => L10n.T("反色");

    /// <inheritdoc />
    public FilterParameters Defaults => new();

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters => [];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;
        int length = dst.Length;

        for (int i = 0; i + 3 < length; i += 4)
        {
            ct.ThrowIfCancellationRequested();

            byte alpha = dst[i + 3];
            if (alpha == 0)
                continue;   // 透明像素保持全零

            uint premul = (uint)(dst[i] | (dst[i + 1] << 8) | (dst[i + 2] << 16) | (dst[i + 3] << 24));
            uint straight = PixelColor.Unpremultiply(premul);
            uint inverted = PixelColor.Premultiply(PixelColor.PackBgra(
                (byte)(255 - PixelColor.R(straight)),
                (byte)(255 - PixelColor.G(straight)),
                (byte)(255 - PixelColor.B(straight)),
                alpha));

            dst[i] = (byte)(inverted & 0xFF);
            dst[i + 1] = (byte)((inverted >> 8) & 0xFF);
            dst[i + 2] = (byte)((inverted >> 16) & 0xFF);
        }

        progress?.Report(100, L10n.T("反色完成"));
        return editor.Commit();
    }
}