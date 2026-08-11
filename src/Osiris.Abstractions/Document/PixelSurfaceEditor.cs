namespace Osiris.Abstractions.Document;

/// <summary>
/// 像素面编辑会话：对 PixelSurface 的可变工作缓冲 + 脏矩形跟踪。
/// COW 语义：构造时深拷贝源数据，Commit() 前源 PixelSurface 保持不变；
/// Commit() 再深拷贝当前缓冲生成新 PixelSurface 返回。
/// 注意：本类型暴露原始 Span，无法自动感知写入区域——调用方写入后
/// 必须显式 MarkDirty 声明被修改区域（或对整幅操作 MarkAllDirty），
/// 供渲染层依据合并后的脏矩形做增量刷新。
/// </summary>
public sealed class PixelSurfaceEditor
{
    private readonly byte[] _buffer;   // 工作缓冲（源数据的独立副本）
    private readonly int _width;
    private readonly int _height;
    private readonly int _rowBytes;
    private DirtyRect? _dirty;         // 已合并的脏矩形；null = 尚无修改声明

    // 内部构造：由 PixelSurface.CreateEditor() 调用，深拷贝源像素。
    internal PixelSurfaceEditor(PixelSurface source)
    {
        _width = source.Width;
        _height = source.Height;
        _rowBytes = source.RowBytes;
        _buffer = source.Data.ToArray();
    }

    /// <summary>画布宽（像素）。</summary>
    public int Width => _width;

    /// <summary>画布高（像素）。</summary>
    public int Height => _height;

    /// <summary>单行字节数（== Width * 4）。</summary>
    public int RowBytes => _rowBytes;

    /// <summary>整幅可写缓冲（BGRA 预乘；写入后需 MarkDirty）。</summary>
    public Span<byte> Pixels => _buffer;

    /// <summary>按行可写缓冲（y 越界抛 ArgumentOutOfRangeException）。</summary>
    public Span<byte> Row(int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        return _buffer.AsSpan(y * _rowBytes, _rowBytes);
    }

    /// <summary>当前已合并的脏矩形；null = 尚无任何修改声明。</summary>
    public DirtyRect? DirtyRect => _dirty;

    /// <summary>
    /// 声明矩形区域被修改：与已有脏区自动合并为包围盒（超界部分裁剪到画布内）。
    /// </summary>
    public void MarkDirty(int x, int y, int width, int height)
    {
        // 裁剪到画布边界；空区域直接忽略
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(_width, x + width);
        int y1 = Math.Min(_height, y + height);
        if (x1 <= x0 || y1 <= y0)
            return;

        var rect = new DirtyRect(x0, y0, x1 - x0, y1 - y0);
        _dirty = _dirty is { } existing ? existing.Union(rect) : rect;
    }

    /// <summary>声明整幅画面被修改（脏矩形覆盖全画布）。</summary>
    public void MarkAllDirty() => _dirty = new DirtyRect(0, 0, _width, _height);

    /// <summary>
    /// 提交编辑：以当前缓冲复制出新的 PixelSurface 返回。
    /// 本编辑器可继续编辑（再次 Commit 将再次复制最新状态）；源 PixelSurface 始终不变。
    /// </summary>
    public PixelSurface Commit() => PixelSurface.Create(_width, _height, _buffer);
}
