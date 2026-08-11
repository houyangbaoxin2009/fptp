using Osiris.Abstractions.Document;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 证件照轻量排版器（ABI 红线：插件不得引用 Core 的 LayoutProcessor，故插件内复刻其核心算法）：
/// 把单张照片按网格居中排列到相纸（5寸/6寸/A4），只做像素拷贝，不画虚线引导线（2.1 简化）。
/// 照片大于相纸时先等比双线性缩小到相纸内（否则居中起点为负导致越界，见 2.0.9.1 修复）。
/// </summary>
internal static class LayoutComposer
{
    /// <summary>照片间隙（像素，与旧版 LayoutProcessor 一致）。</summary>
    private const int Gap = 40;

    /// <summary>相纸预设（与旧版一致：5寸 1500×1050 / 6寸 1800×1200 / A4 3508×2480）。</summary>
    public static readonly IReadOnlyDictionary<string, (int Width, int Height)> PaperPresets =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            ["5寸"] = (1500, 1050),
            ["6寸"] = (1800, 1200),
            ["A4"] = (3508, 2480),
        };

    /// <summary>
    /// 排版单张照片到相纸：返回排好版的相纸像素面与网格行列数；相纸名未知返回 null。
    /// </summary>
    public static PixelSurface? Compose(PixelSurface photo, string paperName, out int columns, out int rows)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(paperName);

        columns = 1;
        rows = 1;
        if (!PaperPresets.TryGetValue(paperName, out var paper))
            return null;

        // 照片大于相纸：等比缩小到相纸内（否则居中起点为负，BlockCopy 越界崩溃）
        if (photo.Width > paper.Width || photo.Height > paper.Height)
        {
            double scale = Math.Min((double)paper.Width / photo.Width, (double)paper.Height / photo.Height);
            int sw = Math.Max(1, (int)(photo.Width * scale));
            int sh = Math.Max(1, (int)(photo.Height * scale));
            photo = ScaleBilinear(photo, sw, sh);
        }

        int photoW = photo.Width;
        int photoH = photo.Height;

        // 网格行列数：每格 = 照片 + 间隙，尽量排满相纸
        columns = Math.Max(1, (paper.Width + Gap) / (photoW + Gap));
        rows = Math.Max(1, (paper.Height + Gap) / (photoH + Gap));

        // 网格整体居中对齐（避免偏左偏上）
        int contentWidth = columns * photoW + (columns - 1) * Gap;
        int contentHeight = rows * photoH + (rows - 1) * Gap;
        int startX = (paper.Width - contentWidth) / 2;
        int startY = (paper.Height - contentHeight) / 2;

        // 相纸白底
        var paperSurface = PixelSurface.Create(paper.Width, paper.Height);
        var editor = paperSurface.CreateEditor();
        FillWhite(editor);

        // 逐格拷贝照片（纯像素拷贝，不画辅助线）
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int dstX = startX + c * (photoW + Gap);
                int dstY = startY + r * (photoH + Gap);
                CopyPhoto(editor, photo, dstX, dstY);
            }
        }

        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>整幅填白（BGRA 不透明白）。</summary>
    private static void FillWhite(PixelSurfaceEditor editor)
    {
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;      // B
            pixels[i + 1] = 255;  // G
            pixels[i + 2] = 255;  // R
            pixels[i + 3] = 255;  // A
        }
    }

    /// <summary>整块拷贝照片到相纸指定位置（行内连续，逐行拷入，越界部分被编辑器裁剪丢弃）。</summary>
    private static void CopyPhoto(PixelSurfaceEditor paper, PixelSurface photo, int dstX, int dstY)
    {
        int rowBytes = photo.Width * 4;
        for (int r = 0; r < photo.Height; r++)
        {
            // 目标行可能落在相纸外（网格计算保证不会，此处防御性跳过）
            int dstRow = dstY + r;
            if (dstRow < 0 || dstRow >= paper.Height)
                continue;
            if (dstX >= paper.Width)
                continue;

            ReadOnlySpan<byte> src = photo.Row(r);
            Span<byte> dstRowSpan = paper.Row(dstRow);

            // 只拷相纸可见部分（避免源行宽超目标行宽）
            int copyStart = dstX < 0 ? -dstX : 0;
            int visible = Math.Min(rowBytes - copyStart * 4, paper.RowBytes - dstX * 4);
            if (visible <= 0)
                continue;
            src.Slice(copyStart * 4, visible).CopyTo(dstRowSpan.Slice(dstX * 4, visible));
        }
    }

    /// <summary>双线性缩放（照片大于相纸时降采样，BGRA 各通道插值）。</summary>
    private static PixelSurface ScaleBilinear(PixelSurface src, int outW, int outH)
    {
        var output = PixelSurface.Create(outW, outH);
        var editor = output.CreateEditor();
        Span<byte> dstData = editor.Pixels;

        double scaleX = (double)src.Width / outW;
        double scaleY = (double)src.Height / outH;

        for (int y = 0; y < outH; y++)
        {
            double sy = (y + 0.5) * scaleY - 0.5;
            int y0 = Clamp((int)Math.Floor(sy), 0, src.Height - 1);
            int y1 = Clamp(y0 + 1, 0, src.Height - 1);
            double fy = Clamp01(sy - y0);

            for (int x = 0; x < outW; x++)
            {
                double sx = (x + 0.5) * scaleX - 0.5;
                int x0 = Clamp((int)Math.Floor(sx), 0, src.Width - 1);
                int x1 = Clamp(x0 + 1, 0, src.Width - 1);
                double fx = Clamp01(sx - x0);

                int o = (y * outW + x) * 4;
                for (int c = 0; c < 4; c++)
                {
                    double v00 = src.Row(y0)[x0 * 4 + c];
                    double v10 = src.Row(y0)[x1 * 4 + c];
                    double v01 = src.Row(y1)[x0 * 4 + c];
                    double v11 = src.Row(y1)[x1 * 4 + c];
                    double top = v00 + (v10 - v00) * fx;
                    double bottom = v01 + (v11 - v01) * fx;
                    dstData[o + c] = (byte)(top + (bottom - top) * fy);
                }
            }
        }

        editor.MarkAllDirty();
        return editor.Commit();
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
}
