namespace Osiris.Abstractions.Document;

/// <summary>
/// 像素级选区蒙版：1 位/像素（byte[] 每字节存 8 个像素）。
/// 位序约定：位索引 = y * Width + x，位于 byte[index &gt;&gt; 3] 的第 (index &amp; 7) 位。
/// 支持多边形/矩形栅格化、判点、与另一选区求交（就地）、克隆。
/// </summary>
public sealed class Selection
{
    // 位掩码缓冲：长度 == (Width * Height + 7) / 8。
    private readonly byte[] _mask;

    /// <summary>选区宽（像素）。</summary>
    public int Width { get; }

    /// <summary>选区高（像素）。</summary>
    public int Height { get; }

    /// <summary>创建全空选区（初始无任何像素被选中）。</summary>
    public Selection(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        _mask = new byte[checked((width * height + 7) / 8)];
    }

    // 私有构造：直接采用给定缓冲（Clone 用，掩码长度已由调用方保证）。
    private Selection(int width, int height, byte[] mask)
    {
        Width = width;
        Height = height;
        _mask = mask;
    }

    /// <summary>清除选区（全部像素取消选中）。</summary>
    public void Clear() => Array.Clear(_mask);

    /// <summary>判断 (x,y) 是否被选中；越界返回 false。</summary>
    public bool Contains(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            return false;
        int index = y * Width + x;
        return (_mask[index >> 3] & (1 << (index & 7))) != 0;
    }

    /// <summary>矩形栅格化：选中 [x, x+width) × [y, y+height) 区域（自动裁剪到画布）。</summary>
    public void SetRect(int x, int y, int width, int height)
    {
        // 裁剪到画布边界；越界或负尺寸时循环自然不执行
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = (int)Math.Min((long)Width, (long)x + width);
        int y1 = (int)Math.Min((long)Height, (long)y + height);
        for (int yy = y0; yy < y1; yy++)
            for (int xx = x0; xx < x1; xx++)
                SetBit(xx, yy);
    }

    /// <summary>
    /// 多边形栅格化（扫描线算法）：选中多边形内部像素。
    /// 顶点为文档像素坐标，多边形自动首尾相连闭合（无需重复首点）；
    /// 水平边不参与扫描线求交，超画布部分自动裁剪。
    /// </summary>
    public void SetPolygon(IReadOnlyList<Point2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3)
            return;

        // ---- 1) 构建边表：每条边记录 [YMin, YMax) 与 X 随 Y 的增量 dxInv ----
        var edges = new List<(int YMin, int YMax, double X, double DxInv)>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Point2 a = points[i];
            Point2 b = points[(i + 1) % points.Count];
            if (a.Y == b.Y)
                continue; // 水平边与扫描线无单点交点，跳过
            if (a.Y < b.Y)
                edges.Add((a.Y, b.Y, a.X, (double)(b.X - a.X) / (b.Y - a.Y)));
            else
                edges.Add((b.Y, a.Y, b.X, (double)(a.X - b.X) / (a.Y - b.Y)));
        }
        if (edges.Count == 0)
            return;

        // 扫描线范围：与画布高度求交
        edges.Sort(static (e1, e2) => e1.YMin.CompareTo(e2.YMin));
        int yStart = Math.Max(0, edges[0].YMin);
        int yEnd = Math.Min(Height - 1, edges.Max(static e => e.YMax) - 1);

        // ---- 2) 逐扫描线维护活动边表（AET）：交点排序后成对填充 ----
        var active = new List<(int YMax, double X, double DxInv)>();
        int edgeIndex = 0;
        for (int y = yStart; y <= yEnd; y++)
        {
            // 本行新进入的边（YMin <= y 且未加入），X 按斜率外推到当前行
            while (edgeIndex < edges.Count && edges[edgeIndex].YMin <= y)
            {
                (int yMin, int yMax, double x, double dxInv) = edges[edgeIndex];
                active.Add((yMax, x + (y - yMin) * dxInv, dxInv));
                edgeIndex++;
            }

            // 移除已结束的边（YMax <= y 的行不参与）
            active.RemoveAll(e => e.YMax <= y);

            // 交点按 X 排序，相邻两两配对填充
            active.Sort(static (e1, e2) => e1.X.CompareTo(e2.X));
            for (int i = 0; i + 1 < active.Count; i += 2)
            {
                int x0 = (int)Math.Ceiling(active[i].X);
                int x1 = (int)Math.Floor(active[i + 1].X);
                if (x0 < 0)
                    x0 = 0;
                if (x1 >= Width)
                    x1 = Width - 1;
                for (int x = x0; x <= x1; x++)
                    SetBit(x, y);
            }

            // 推进到下一扫描线
            for (int i = 0; i < active.Count; i++)
                active[i] = (active[i].YMax, active[i].X + active[i].DxInv, active[i].DxInv);
        }
    }

    /// <summary>
    /// 与另一选区求交（就地，仅保留两边都选中的像素）。
    /// 两选区尺寸必须一致，否则抛 ArgumentException。
    /// </summary>
    public void Intersect(Selection other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Width != Width || other.Height != Height)
            throw new ArgumentException($"选区尺寸不一致：本 {Width}x{Height}，目标 {other.Width}x{other.Height}。", nameof(other));

        for (int i = 0; i < _mask.Length; i++)
            _mask[i] &= other._mask[i];
    }

    /// <summary>深拷贝选区（掩码与尺寸均独立）。</summary>
    public Selection Clone()
    {
        var copy = new byte[_mask.Length];
        Array.Copy(_mask, copy, _mask.Length);
        return new Selection(Width, Height, copy);
    }

    // 内部置位：仅接受已校验在画布内的坐标。
    private void SetBit(int x, int y)
    {
        int index = y * Width + x;
        _mask[index >> 3] |= (byte)(1 << (index & 7));
    }
}
