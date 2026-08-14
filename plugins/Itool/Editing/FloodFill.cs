using Osiris.Abstractions.Document;

namespace Itool.Editing;

/// <summary>
/// 泛洪填充 / 区域选择算法（魔棒选区与颜料桶共用）。
/// 像素操作在预乘 BGRA 上，颜色比较用非预乘值；alpha&lt;128 视为透明（比较色 0）。
/// </summary>
public static class FloodFill
{
    /// <summary>
    /// 从 (startX, startY) 做颜色相似 BFS（曼哈顿距离 |dr|+|dg|+|db| ≤ tolerance）生成选区掩码。
    /// 起点越界返回空选区。
    /// </summary>
    public static Selection SelectRegion(PixelSurface surface, int startX, int startY, int tolerance)
    {
        var selection = new Selection(surface.Width, surface.Height);
        if ((uint)startX >= (uint)surface.Width || (uint)startY >= (uint)surface.Height)
            return selection; // 越界：空选区

        (int sr, int sg, int sb) = CompareColor(surface, startX, startY);

        // BFS 四邻域；Selection.Contains 作 visited（O(1) 位测试）
        var queue = new Queue<(int X, int Y)>();
        selection.SetRect(startX, startY, 1, 1);
        queue.Enqueue((startX, startY));
        while (queue.Count > 0)
        {
            (int x, int y) = queue.Dequeue();
            Visit(x + 1, y);
            Visit(x - 1, y);
            Visit(x, y + 1);
            Visit(x, y - 1);
        }
        return selection;

        void Visit(int nx, int ny)
        {
            if ((uint)nx >= (uint)surface.Width || (uint)ny >= (uint)surface.Height) return;
            if (selection.Contains(nx, ny)) return;
            (int cr, int cg, int cb) = CompareColor(surface, nx, ny);
            if (Math.Abs(cr - sr) + Math.Abs(cg - sg) + Math.Abs(cb - sb) <= tolerance)
            {
                selection.SetRect(nx, ny, 1, 1);
                queue.Enqueue((nx, ny));
            }
        }
    }

    /// <summary>
    /// 区域内像素替换为目标色：保持各像素自身 Alpha，目标色按该 Alpha 预乘。
    /// 返回新 PixelSurface（COW，原实例不变）。
    /// </summary>
    public static PixelSurface FillRegion(PixelSurface surface, Selection region, uint targetBgra)
    {
        var editor = surface.CreateEditor();
        byte tr = (byte)(targetBgra >> 16), tg = (byte)(targetBgra >> 8), tb = (byte)targetBgra;
        for (int y = 0; y < surface.Height; y++)
        {
            Span<byte> row = editor.Row(y);
            for (int x = 0; x < surface.Width; x++)
            {
                if (!region.Contains(x, y)) continue;
                int i = x * 4;
                byte a = row[i + 3];
                if (a == 0) continue; // 全透明像素保持透明
                row[i] = (byte)(tb * a / 255);
                row[i + 1] = (byte)(tg * a / 255);
                row[i + 2] = (byte)(tr * a / 255);
                // alpha 不变
            }
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }

    /// <summary>取像素的非预乘比较色（alpha&lt;128 视为透明 0）。</summary>
    private static (int R, int G, int B) CompareColor(PixelSurface s, int x, int y)
    {
        ReadOnlySpan<byte> row = s.Row(y);
        int i = x * 4;
        byte b = row[i], g = row[i + 1], r = row[i + 2], a = row[i + 3];
        if (a < 128) return (0, 0, 0);
        return (r * 255 / a, g * 255 / a, b * 255 / a);
    }
}
