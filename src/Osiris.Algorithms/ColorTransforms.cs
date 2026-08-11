using Osiris.Abstractions.Document;

namespace Osiris.Algorithms;

/// <summary>
/// 色彩变换算法：灰度、色彩量化（动漫风格）、Sobel 边缘检测。
/// 输入输出均为 BGRA 预乘 PixelSurface（COW）。
/// </summary>
public static class ColorTransforms
{
    /// <summary>灰度（亮度公式 0.299R+0.587G+0.114B），保持 Alpha 与预乘语义。</summary>
    public static PixelSurface Grayscale(PixelSurface source)
    {
        var editor = source.CreateEditor();
        for (int y = 0; y < source.Height; y++)
        {
            Span<byte> row = editor.Row(y);
            for (int x = 0; x < source.Width; x++)
            {
                int i = x * 4;
                byte a = row[i + 3];
                byte r, g, b;
                if (a == 0)
                {
                    r = g = b = 0;
                }
                else
                {
                    // 反预乘 → 灰度 → 再预乘（保持 Alpha）
                    byte ur = (byte)Math.Min(255, row[i + 2] * 255 / a);
                    byte ug = (byte)Math.Min(255, row[i + 1] * 255 / a);
                    byte ub = (byte)Math.Min(255, row[i] * 255 / a);
                    byte gray = (byte)Math.Clamp(ur * 299 / 1000 + ug * 587 / 1000 + ub * 114 / 1000, 0, 255);
                    r = g = b = (byte)(gray * a / 255);
                }
                row[i] = b;
                row[i + 1] = g;
                row[i + 2] = r;
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>色彩量化（动漫风格）：把 RGB 量化为 levels 级色阶，保留 Alpha。</summary>
    public static PixelSurface Quantize(PixelSurface source, int levels)
    {
        int lv = Math.Clamp(levels, 2, 256);
        int step = 256 / lv;
        var editor = source.CreateEditor();
        for (int y = 0; y < source.Height; y++)
        {
            Span<byte> row = editor.Row(y);
            for (int x = 0; x < source.Width; x++)
            {
                int i = x * 4;
                byte a = row[i + 3];
                if (a == 0) continue;
                byte ur = (byte)Math.Min(255, row[i + 2] * 255 / a);
                byte ug = (byte)Math.Min(255, row[i + 1] * 255 / a);
                byte ub = (byte)Math.Min(255, row[i] * 255 / a);
                // 量化到 step 的倍数（含半级偏移使量化边界居中）
                byte qr = (byte)((ur / step) * step + step / 2);
                byte qg = (byte)((ug / step) * step + step / 2);
                byte qb = (byte)((ub / step) * step + step / 2);
                row[i] = (byte)(qb * a / 255);
                row[i + 1] = (byte)(qg * a / 255);
                row[i + 2] = (byte)(qr * a / 255);
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>Sobel 边缘检测：返回边缘强度图（边缘白、平坦黑），Alpha 保持。</summary>
    public static PixelSurface SobelEdges(PixelSurface source, int threshold = 60)
    {
        // 先转灰度工作缓冲（预乘）
        PixelSurface gray = Grayscale(source);
        var editor = source.CreateEditor();
        for (int y = 0; y < source.Height; y++)
        {
            Span<byte> row = editor.Row(y);
            for (int x = 0; x < source.Width; x++)
            {
                int i = x * 4;
                byte a = row[i + 3];
                if (a == 0) continue;
                // 3x3 Sobel（基于灰度，边缘像素用最近邻）
                double gx = 0, gy = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int px = Math.Clamp(x + dx, 0, source.Width - 1);
                        int py = Math.Clamp(y + dy, 0, source.Height - 1);
                        byte l = GrayAt(gray, px, py);
                        double w = SobelWeight(dx, dy);
                        gx += l * w;
                        gy += l * SobelWeight(dy, dx);
                    }
                double mag = Math.Sqrt(gx * gx + gy * gy) / 4;
                byte v = mag > threshold ? (byte)255 : (byte)0;
                row[i] = (byte)(v * a / 255);
                row[i + 1] = row[i];
                row[i + 2] = row[i];
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>Sobel 核权重（Gx 方向；Gy 经转置使用）。</summary>
    private static double SobelWeight(int dx, int dy) => (dx, dy) switch
    {
        (-1, -1) or (1, -1) or (-1, 1) or (1, 1) => 1,
        (0, -1) or (0, 1) => 2,
        _ => 0,
    };

    /// <summary>取灰度像素亮度（预乘转直通）。</summary>
    private static byte GrayAt(PixelSurface surface, int x, int y)
    {
        ReadOnlySpan<byte> row = surface.Row(y);
        int i = x * 4;
        byte a = row[i + 3];
        if (a == 0) return 0;
        byte ur = (byte)Math.Min(255, row[i + 2] * 255 / a);
        byte ug = (byte)Math.Min(255, row[i + 1] * 255 / a);
        byte ub = (byte)Math.Min(255, row[i] * 255 / a);
        return (byte)(ur * 299 / 1000 + ug * 587 / 1000 + ub * 114 / 1000);
    }
}
