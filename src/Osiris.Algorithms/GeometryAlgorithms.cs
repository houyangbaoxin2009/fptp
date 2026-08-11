using Osiris.Abstractions.Document;

namespace Osiris.Algorithms;

/// <summary>
/// 几何绘制算法：Bresenham 线段、硬边/软边笔刷印记、多毛刷痕（现实刷子效果）。
/// 供画笔工具/绘图插件复用；像素合成（source-over 预乘）见 PaintPixel。
/// </summary>
public static class GeometryAlgorithms
{
    /// <summary>整数 Bresenham 线段：逐点回调（含端点）。</summary>
    public static void BresenhamLine(Point2 from, Point2 to, Action<int, int> plot)
    {
        int x0 = from.X, y0 = from.Y, x1 = to.X, y1 = to.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            plot(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>在编辑器上以 coverage(0~1) 把颜色"源上合成"到目标像素（预乘 source-over；越界忽略）。</summary>
    public static void PaintPixel(PixelSurfaceEditor editor, int x, int y, double coverage, uint color)
    {
        if (coverage <= 0 || (uint)x >= (uint)editor.Width || (uint)y >= (uint)editor.Height) return;
        Span<byte> row = editor.Row(y);
        int i = x * 4;
        byte sa = (byte)(((color >> 24) & 0xFF) * coverage); // 源 alpha（含笔刷衰减）
        if (sa == 0) return;
        byte sr = (byte)(((color >> 16) & 0xFF) * sa / 255);
        byte sg = (byte)(((color >> 8) & 0xFF) * sa / 255);
        byte sb = (byte)((color & 0xFF) * sa / 255);
        int inv = 255 - sa;
        row[i] = (byte)(sb + row[i] * inv / 255);
        row[i + 1] = (byte)(sg + row[i + 1] * inv / 255);
        row[i + 2] = (byte)(sr + row[i + 2] * inv / 255);
        row[i + 3] = (byte)(sa + row[i + 3] * inv / 255);
    }

    /// <summary>硬边圆点印记（d ≤ r 全覆盖）。</summary>
    public static void HardCircleStamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
    {
        double r = Math.Max(0.5, size / 2);
        for (int py = (int)Math.Floor(y - r); py <= (int)Math.Ceiling(y + r); py++)
            for (int px = (int)Math.Floor(x - r); px <= (int)Math.Ceiling(x + r); px++)
                if ((px - x) * (px - x) + (py - y) * (py - y) <= r * r)
                    PaintPixel(editor, px, py, 1, color);
    }

    /// <summary>软边圆点印记：d ≤ r 时 coverage=(1-d/r)^2（径向平滑衰减，毛笔效果）。</summary>
    public static void SoftCircleStamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
    {
        double r = Math.Max(0.5, size / 2);
        for (int py = (int)Math.Floor(y - r); py <= (int)Math.Ceiling(y + r); py++)
            for (int px = (int)Math.Floor(x - r); px <= (int)Math.Ceiling(x + r); px++)
            {
                double d = Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d <= r)
                {
                    double c = 1 - d / r;
                    PaintPixel(editor, px, py, c * c, color);
                }
            }
    }

    /// <summary>
    /// 多毛刷痕：沿笔画方向绘制 N 根平行毛线，毛的中心偏移与宽度从中间向两边逐步减小
    /// （中间粗浓、两边渐细渐淡，形如现实刷子刷过）。毛数随尺寸自适应 5~13 根。
    /// </summary>
    public static void BrushHairsStroke(PixelSurfaceEditor editor, Point2 from, Point2 to, double size, uint color)
    {
        int hairs = Math.Clamp((int)Math.Round(size / 3), 5, 13);
        double len = Math.Max(1e-6, Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y)));
        double nx = (to.Y - from.Y) / len; // 法线方向（垂直笔画）
        double ny = -(to.X - from.X) / len;
        double half = Math.Max(0.5, size / 2);

        for (int i = 0; i < hairs; i++)
        {
            double t = hairs == 1 ? 0 : i / (double)(hairs - 1) * 2 - 1; // -1..1，0=中间
            double offset = t * half;                                                                  // 沿法线偏移
            double hairR = Math.Max(0.5, size / 2 * (1 - Math.Abs(t)) * 0.85 + 0.3);                  // 毛宽：中间最大向两边递减
            var hFrom = new Point2((int)Math.Round(from.X + nx * offset), (int)Math.Round(from.Y + ny * offset));
            var hTo = new Point2((int)Math.Round(to.X + nx * offset), (int)Math.Round(to.Y + ny * offset));
            // 每根毛 = 沿线软边描线（毛半径 hairR）
            BresenhamLine(hFrom, hTo, (x, y) => SoftCircleStamp(editor, x, y, hairR * 2, color));
        }
    }
}
