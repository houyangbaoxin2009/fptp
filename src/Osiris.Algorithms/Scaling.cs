using Osiris.Abstractions.Document;

namespace Osiris.Algorithms;

/// <summary>图像缩放算法（最近邻 / 双线性），供排版、缩略图等复用。</summary>
public static class Scaling
{
    /// <summary>最近邻缩放（速度快、硬边；放大像素感强）。</summary>
    public static PixelSurface ScaleNearest(PixelSurface source, int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) throw new ArgumentOutOfRangeException(nameof(newWidth), "目标尺寸必须为正。");
        var editor = PixelSurface.Create(newWidth, newHeight).CreateEditor();
        double sx = source.Width / (double)newWidth;
        double sy = source.Height / (double)newHeight;
        for (int y = 0; y < newHeight; y++)
        {
            int srcY = (int)(y * sy);
            srcY = Math.Clamp(srcY, 0, source.Height - 1);
            Span<byte> dst = editor.Row(y);
            ReadOnlySpan<byte> srcRow = source.Row(srcY);
            for (int x = 0; x < newWidth; x++)
            {
                int srcX = Math.Clamp((int)(x * sx), 0, source.Width - 1);
                int si = srcX * 4, di = x * 4;
                dst[di] = srcRow[si];
                dst[di + 1] = srcRow[si + 1];
                dst[di + 2] = srcRow[si + 2];
                dst[di + 3] = srcRow[si + 3];
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>双线性缩放（平滑；放大更柔和）。</summary>
    public static PixelSurface ScaleBilinear(PixelSurface source, int newWidth, int newHeight)
    {
        if (newWidth <= 0 || newHeight <= 0) throw new ArgumentOutOfRangeException(nameof(newWidth), "目标尺寸必须为正。");
        var editor = PixelSurface.Create(newWidth, newHeight).CreateEditor();
        for (int y = 0; y < newHeight; y++)
        {
            double gy = (y + 0.5) * source.Height / newHeight - 0.5;
            int y0 = Math.Clamp((int)Math.Floor(gy), 0, source.Height - 1);
            int y1 = Math.Clamp(y0 + 1, 0, source.Height - 1);
            double fy = gy - Math.Floor(gy);
            Span<byte> dst = editor.Row(y);
            for (int x = 0; x < newWidth; x++)
            {
                double gx = (x + 0.5) * source.Width / newWidth - 0.5;
                int x0 = Math.Clamp((int)Math.Floor(gx), 0, source.Width - 1);
                int x1 = Math.Clamp(x0 + 1, 0, source.Width - 1);
                double fx = gx - Math.Floor(gx);
                ReadOnlySpan<byte> r00 = source.Row(y0).Slice(x0 * 4, 4);
                ReadOnlySpan<byte> r01 = source.Row(y0).Slice(x1 * 4, 4);
                ReadOnlySpan<byte> r10 = source.Row(y1).Slice(x0 * 4, 4);
                ReadOnlySpan<byte> r11 = source.Row(y1).Slice(x1 * 4, 4);
                int di = x * 4;
                for (int c = 0; c < 4; c++)
                {
                    double top = r00[c] * (1 - fx) + r01[c] * fx;
                    double bottom = r10[c] * (1 - fx) + r11[c] * fx;
                    dst[di + c] = (byte)Math.Clamp(top * (1 - fy) + bottom * fy, 0, 255);
                }
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }
}
