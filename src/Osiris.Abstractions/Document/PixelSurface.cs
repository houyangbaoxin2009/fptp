namespace Osiris.Abstractions.Document;

/// <summary>
/// 像素表面：BGRA 预乘 8bit 位图的不可变容器（契约层唯一像素交换类型）。
/// 宿主渲染层（Skia）与插件滤镜之间只通过本类型交换像素，规避 ABI 红线；
/// 内部为 byte[]，天然支持 Span 切片与零拷贝 SKData 包装。
/// 本类型只读（Pixels/Row 均返回 ReadOnlySpan），修改必须经 CreateEditor() 的编辑会话（COW）。
/// </summary>
public sealed class PixelSurface
{
    // 像素缓冲：按行连续存储，行内 BGRA 预乘（B,G,R,A），行尾无填充（RowBytes == Width * 4）。
    private readonly byte[] _data;

    /// <summary>画布宽（像素）。</summary>
    public int Width { get; }

    /// <summary>画布高（像素）。</summary>
    public int Height { get; }

    /// <summary>单行字节数（== Width * 4）。</summary>
    public int RowBytes { get; }

    // 私有构造：只允许静态工厂创建，保证 _data 长度与尺寸严格一致。
    private PixelSurface(int width, int height, byte[] data)
    {
        Width = width;
        Height = height;
        RowBytes = width * 4;
        _data = data;
    }

    /// <summary>整幅像素只读访问（BGRA 预乘，总长 == RowBytes * Height）。</summary>
    public ReadOnlySpan<byte> Pixels => _data;

    /// <summary>按行只读访问（y 越界抛 ArgumentOutOfRangeException）。</summary>
    public ReadOnlySpan<byte> Row(int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
        return _data.AsSpan(y * RowBytes, RowBytes);
    }

    /// <summary>创建全透明黑画布（BGRA 全零即预乘透明黑）。</summary>
    public static PixelSurface Create(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        return new PixelSurface(width, height, new byte[checked(width * height * 4)]);
    }

    /// <summary>
    /// 从外部数据创建像素面：data 须为 BGRA 预乘且长度 == width * height * 4（数据被复制）。
    /// </summary>
    public static PixelSurface Create(int width, int height, ReadOnlySpan<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        int expected = checked(width * height * 4);
        if (data.Length != expected)
            throw new ArgumentException($"数据长度 {data.Length} 与 {width}x{height} 的 BGRA 缓冲（{expected} 字节）不匹配。", nameof(data));

        var copy = new byte[data.Length];
        data.CopyTo(copy);
        return new PixelSurface(width, height, copy);
    }

    /// <summary>
    /// 打开编辑会话（COW 语义）：返回持有独立缓冲的 PixelSurfaceEditor。
    /// 编辑器的未提交修改不影响本实例——本实例像素在 Commit() 前保持不变。
    /// </summary>
    public PixelSurfaceEditor CreateEditor() => new(this);

    // 内部协作：供 PixelSurfaceEditor 构造时读取源数据做深拷贝。
    internal ReadOnlySpan<byte> Data => _data;
}
