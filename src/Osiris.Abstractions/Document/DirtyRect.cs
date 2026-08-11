namespace Osiris.Abstractions.Document;

/// <summary>
/// 脏矩形：记录像素面编辑后被修改的区域（含合并逻辑）。
/// 语义：右/下边界为开区间（不包含 X+Width / Y+Height 处的像素），与 .NET Rectangle 一致。
/// </summary>
public readonly record struct DirtyRect(int X, int Y, int Width, int Height)
{
    /// <summary>右边界（不含，== X + Width）。</summary>
    public int Right => X + Width;

    /// <summary>下边界（不含，== Y + Height）。</summary>
    public int Bottom => Y + Height;

    /// <summary>是否为空矩形（宽度或高度非正，表示无有效区域）。</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>判断点是否落在矩形内（含左/上边界，不含右/下边界）。</summary>
    public bool Contains(int x, int y) => x >= X && x < Right && y >= Y && y < Bottom;

    /// <summary>
    /// 与另一脏区合并：返回恰好包围两者的最小矩形（渲染层增量刷新的最小范围）。
    /// 任一为空时直接返回另一方。
    /// </summary>
    public DirtyRect Union(DirtyRect other)
    {
        if (IsEmpty)
            return other;
        if (other.IsEmpty)
            return this;

        int x0 = Math.Min(X, other.X);
        int y0 = Math.Min(Y, other.Y);
        int x1 = Math.Max(Right, other.Right);
        int y1 = Math.Max(Bottom, other.Bottom);
        return new DirtyRect(x0, y0, x1 - x0, y1 - y0);
    }
}
