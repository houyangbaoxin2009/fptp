using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 动漫模式滤镜（2.1 复刻 2.0 Core AnimeFilter，去掉可选平滑简化参数）：
/// 照片转二次元卡通风格。两步：颜色量化（每通道映射到 levels 级色块，消除渐变）
/// → 边缘检测描边（Sobel 梯度幅值超阈值处画暗色粗轮廓）。
/// 纯 PixelSurface 像素循环，禁止 Skia 等渲染后端。参数：levels(2~16 默认 8)、
/// outline(0~200 默认 60，越大描边越少)。
/// </summary>
public sealed class AnimeFilter : IFilterProcessor
{
    /// <summary>参数键：每通道量化级数（2~16，默认 8）。</summary>
    public const string ParamLevels = "levels";

    /// <summary>参数键：描边强度（0~200，默认 60；越大越不敏感、描边越少）。</summary>
    public const string ParamOutline = "outline";

    /// <inheritdoc />
    public string Id => "fptp.anime";

    /// <inheritdoc />
    public string DisplayName => L10n.T("动漫模式");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamLevels] = 8,
        [ParamOutline] = 60,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamLevels,
            Label = L10n.T("色彩层次"),
            Kind = FilterParameterKind.Int,
            Min = 2,
            Max = 16,
            DefaultValue = 8,
        },
        new()
        {
            Key = ParamOutline,
            Label = L10n.T("描边强度"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 200,
            DefaultValue = 60,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        // 读取并钳制参数
        int levels = Math.Clamp(parameters.Get(ParamLevels, 8), 2, 16);
        int outline = Math.Clamp(parameters.Get(ParamOutline, 60), 0, 200);

        int width = input.Width;
        int height = input.Height;

        // 第 1 步：颜色量化（直通色空间计算后重新预乘，避免半透明边缘被量到错误色）
        var quantized = Quantize(input, levels, ct);

        // 第 2 步：Sobel 边缘检测描边
        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;

        if (outline == 0)
        {
            // 描边关闭：直接输出量化结果
            quantized.CopyTo(dst);
        }
        else
        {
            // 梯度幅值阈值：outline 越大越不敏感（描边更少、更细）
            int gradThreshold = outline * 3;
            int rowBytes = width * 4;

            // 对量化结果做 Sobel 梯度，超阈值像素涂暗色（保留 30% 原亮度，呈勾线感）
            for (int y = 0; y < height; y++)
            {
                ct.ThrowIfCancellationRequested();
                int rowBase = y * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    int o = rowBase + x * 4;
                    int gx = SobelX(quantized, width, height, x, y);
                    int gy = SobelY(quantized, width, height, x, y);
                    int magnitude = Math.Abs(gx) + Math.Abs(gy);

                    if (magnitude > gradThreshold)
                    {
                        // 描边像素：暗化（量化值 30%）并拉满 alpha，形成深色轮廓线
                        dst[o] = (byte)(quantized[o] * 0.3);
                        dst[o + 1] = (byte)(quantized[o + 1] * 0.3);
                        dst[o + 2] = (byte)(quantized[o + 2] * 0.3);
                        dst[o + 3] = Math.Max(quantized[o + 3], (byte)255); // 描边不透明
                    }
                    else
                    {
                        // 非边缘：保持量化结果
                        dst[o] = quantized[o];
                        dst[o + 1] = quantized[o + 1];
                        dst[o + 2] = quantized[o + 2];
                        dst[o + 3] = quantized[o + 3];
                    }
                }
            }
        }

        editor.MarkAllDirty();
        progress?.Report(100, L10n.T("动漫模式完成"));
        return editor.Commit();
    }

    /// <summary>颜色量化：每通道映射到 levels 级色块中心（直通色空间，透明像素原样保持）。</summary>
    private static byte[] Quantize(PixelSurface input, int levels, CancellationToken ct)
    {
        int width = input.Width;
        int height = input.Height;
        var output = new byte[checked(width * height * 4)];
        int step = 256 / levels; // 量化步长

        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            ReadOnlySpan<byte> srcRow = input.Row(y);
            int rowBase = y * width * 4;

            for (int x = 0; x < width; x++)
            {
                int offset = x * 4;
                uint premul = ReadBgra(srcRow, offset);
                byte alpha = PixelColor.A(premul);

                int o = rowBase + offset;
                if (alpha == 0)
                {
                    // 全透明像素：预乘全零，量化无意义，原样保持
                    output[o] = output[o + 1] = output[o + 2] = output[o + 3] = 0;
                    continue;
                }

                // 反预乘 → 量化直通色 → 重新预乘（保证与量化前同处预乘空间）
                uint straight = PixelColor.Unpremultiply(premul);
                uint quantized = PixelColor.Premultiply(PixelColor.PackBgra(
                    (byte)((PixelColor.R(straight) / step) * step + step / 2),
                    (byte)((PixelColor.G(straight) / step) * step + step / 2),
                    (byte)((PixelColor.B(straight) / step) * step + step / 2),
                    alpha));

                output[o] = PixelColor.B(quantized);
                output[o + 1] = PixelColor.G(quantized);
                output[o + 2] = PixelColor.R(quantized);
                output[o + 3] = alpha;
            }
        }
        return output;
    }

    /// <summary>Sobel X 梯度（亮度用 BT.601 加权，边界越界取最近像素）。</summary>
    private static int SobelX(byte[] data, int w, int h, int x, int y)
    {
        int stride = w * 4;
        int p00 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
        int p10 = Luma(data, x, Clamp(y - 1, 0, h - 1), stride);
        int p20 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
        int p01 = Luma(data, Clamp(x - 1, 0, w - 1), y, stride);
        int p21 = Luma(data, Clamp(x + 1, 0, w - 1), y, stride);
        int p02 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
        int p12 = Luma(data, x, Clamp(y + 1, 0, h - 1), stride);
        int p22 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
        return -p00 - 2 * p01 - p02 + p20 + 2 * p21 + p22;
    }

    /// <summary>Sobel Y 梯度。</summary>
    private static int SobelY(byte[] data, int w, int h, int x, int y)
    {
        int stride = w * 4;
        int p00 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
        int p01 = Luma(data, Clamp(x - 1, 0, w - 1), y, stride);
        int p02 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
        int p10 = Luma(data, x, Clamp(y - 1, 0, h - 1), stride);
        int p12 = Luma(data, x, Clamp(y + 1, 0, h - 1), stride);
        int p20 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
        int p21 = Luma(data, Clamp(x + 1, 0, w - 1), y, stride);
        int p22 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
        return -p00 - 2 * p10 - p20 + p02 + 2 * p12 + p22;
    }

    /// <summary>像素亮度（BT.601 加权：0.299R + 0.587G + 0.114B，整数化 /1000）。</summary>
    private static int Luma(byte[] data, int x, int y, int stride)
    {
        int o = y * stride + x * 4;
        return (data[o + 2] * 299 + data[o + 1] * 587 + data[o] * 114) / 1000;
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static uint ReadBgra(ReadOnlySpan<byte> row, int offset)
        => (uint)(row[offset] | (row[offset + 1] << 8) | (row[offset + 2] << 16) | (row[offset + 3] << 24));
}
