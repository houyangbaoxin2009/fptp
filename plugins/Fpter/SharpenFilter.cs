using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 锐化滤镜：非锐化掩模（Unsharp Mask），out = src + t * (src - blur[src])，
/// blur 为 3×3 盒式平均；在直通色空间计算，仅对不透明像素（alpha=255）生效，
/// 半透明像素保持原样（避免透明边缘被卷积拉到黑边）。参数：amount(0~300 默认 100，/100 为强度)。
/// </summary>
public sealed class SharpenFilter : IFilterProcessor
{
    /// <summary>参数键：锐化强度（0~300，100 为标准量）。</summary>
    public const string ParamAmount = "amount";

    /// <inheritdoc />
    public string Id => "fpter.sharpen";

    /// <inheritdoc />
    public string DisplayName => L10n.T("锐化");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamAmount] = 100,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamAmount,
            Label = L10n.T("锐化强度"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 300,
            DefaultValue = 100,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int amount = Math.Clamp(parameters.Get(ParamAmount, 100), 0, 300);

        int width = input.Width;
        int height = input.Height;
        int rowBytes = width * 4;
        int length = checked(rowBytes * height);

        // 1) 整幅转直通（RGB 反预乘，alpha 原样）——卷积在直通空间做
        var straight = new byte[length];
        for (int i = 0; i + 3 < length; i += 4)
        {
            byte alpha = input.Pixels[i + 3];
            if (alpha == 0)
                continue;
            uint premul = (uint)(input.Pixels[i] | (input.Pixels[i + 1] << 8) | (input.Pixels[i + 2] << 16) | (input.Pixels[i + 3] << 24));
            uint s = PixelColor.Unpremultiply(premul);
            straight[i] = (byte)(s & 0xFF);
            straight[i + 1] = (byte)((s >> 8) & 0xFF);
            straight[i + 2] = (byte)((s >> 16) & 0xFF);
            straight[i + 3] = alpha;
        }

        // 2) 3×3 盒式模糊（直通 RGB，alpha 原样；越界取最近像素）
        var blur = new byte[length];
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            for (int x = 0; x < width; x++)
            {
                int o = y * rowBytes + x * 4;
                int sumB = 0, sumG = 0, sumR = 0, sumA = 0;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int yy = Math.Clamp(y + dy, 0, height - 1);
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = Math.Clamp(x + dx, 0, width - 1);
                        int so = yy * rowBytes + xx * 4;
                        sumB += straight[so];
                        sumG += straight[so + 1];
                        sumR += straight[so + 2];
                        sumA += straight[so + 3];
                    }
                }
                blur[o] = (byte)(sumB / 9);
                blur[o + 1] = (byte)(sumG / 9);
                blur[o + 2] = (byte)(sumR / 9);
                blur[o + 3] = (byte)(sumA / 9);
            }
        }

        // 3) 非锐化掩模：仅 alpha=255 像素应用，透明/半透明保持原值（预乘直通一致）
        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;
        double t = amount / 100.0;
        for (int i = 0; i + 3 < length; i += 4)
        {
            ct.ThrowIfCancellationRequested();
            if (dst[i + 3] != 255)
                continue;

            uint sharp = PixelColor.Premultiply(PixelColor.PackBgra(
                Adjust(straight[i + 2], blur[i + 2]),
                Adjust(straight[i + 1], blur[i + 1]),
                Adjust(straight[i], blur[i]),
                dst[i + 3]));

            dst[i] = (byte)(sharp & 0xFF);
            dst[i + 1] = (byte)((sharp >> 8) & 0xFF);
            dst[i + 2] = (byte)((sharp >> 16) & 0xFF);
        }

        editor.MarkAllDirty();
        progress?.Report(100, L10n.T("锐化完成"));
        return editor.Commit();

        // out = src + t * (src - blur)，钳制 [0,255]
        byte Adjust(byte src, byte blurred)
            => (byte)Math.Clamp((int)Math.Round(src + t * (src - blurred)), 0, 255);
    }
}