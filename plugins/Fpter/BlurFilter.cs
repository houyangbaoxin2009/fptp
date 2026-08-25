using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 模糊滤镜：盒式模糊（滑动窗口平均），按预乘空间对 BGRA 四通道分别做水平+垂直两遍
/// 可分离模糊（预乘空间模糊等价于先反预乘再模糊再预乘的近似，半透明边缘不偏色）。
/// 参数：radius(0~20 默认 3，0 为不模糊)。
/// </summary>
public sealed class BlurFilter : IFilterProcessor
{
    /// <summary>参数键：模糊半径（0~20）。</summary>
    public const string ParamRadius = "radius";

    /// <inheritdoc />
    public string Id => "fpter.blur";

    /// <inheritdoc />
    public string DisplayName => L10n.T("模糊");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamRadius] = 3,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamRadius,
            Label = L10n.T("模糊半径"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 20,
            DefaultValue = 3,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int radius = Math.Clamp(parameters.Get(ParamRadius, 3), 0, 20);
        if (radius == 0)
        {
            progress?.Report(100, L10n.T("模糊完成"));
            return input.CreateEditor().Commit();   // 半径 0：原样输出
        }

        int width = input.Width;
        int height = input.Height;
        int rowBytes = width * 4;
        int length = checked(rowBytes * height);

        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;

        // 第 1 遍：水平模糊（每行独立滑动窗口）→ 中间缓冲
        var horizontal = new byte[length];
        int kernel = radius * 2 + 1;
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            int rowBase = y * rowBytes;
            Span<byte> srcRow = dst.Slice(rowBase, rowBytes);
            Span<byte> outRow = horizontal.AsSpan(rowBase, rowBytes);

            // 4 通道滑动窗口累加（B,G,R,A）
            var sums = new int[4];
            for (int x = -radius; x <= radius; x++)
            {
                int clamped = Math.Clamp(x, 0, width - 1);
                int o = clamped * 4;
                for (int c = 0; c < 4; c++)
                    sums[c] += srcRow[o + c];
            }
            for (int x = 0; x < width; x++)
            {
                int o = x * 4;
                for (int c = 0; c < 4; c++)
                    outRow[o + c] = (byte)(sums[c] / kernel);

                // 窗口右移：减掉将离开的最左列，加入新进入的最右列（越界取最近像素）
                int removeIdx = Math.Clamp(x - radius, 0, width - 1);
                int addIdx = Math.Clamp(x + radius + 1, 0, width - 1);
                for (int c = 0; c < 4; c++)
                {
                    sums[c] -= srcRow[removeIdx * 4 + c];
                    sums[c] += srcRow[addIdx * 4 + c];
                }
            }
        }

        // 第 2 遍：垂直模糊（逐列滑动窗口）→ 直接写回 dst
        for (int x = 0; x < width; x++)
        {
            ct.ThrowIfCancellationRequested();
            var sums = new int[4];
            for (int y = -radius; y <= radius; y++)
            {
                int clamped = Math.Clamp(y, 0, height - 1);
                int o = clamped * rowBytes + x * 4;
                for (int c = 0; c < 4; c++)
                    sums[c] += horizontal[o + c];
            }
            for (int y = 0; y < height; y++)
            {
                int o = y * rowBytes + x * 4;
                for (int c = 0; c < 4; c++)
                    dst[o + c] = (byte)(sums[c] / kernel);

                int removeIdx = Math.Clamp(y - radius, 0, height - 1);
                int addIdx = Math.Clamp(y + radius + 1, 0, height - 1);
                for (int c = 0; c < 4; c++)
                {
                    sums[c] -= horizontal[removeIdx * rowBytes + x * 4 + c];
                    sums[c] += horizontal[addIdx * rowBytes + x * 4 + c];
                }
            }
        }

        editor.MarkAllDirty();
        progress?.Report(100, L10n.T("模糊完成"));
        return editor.Commit();
    }
}