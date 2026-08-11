namespace Osiris.Abstractions.Document;

/// <summary>
/// 文档模型：画布尺寸 + 图层集合 + 活动选区（纯数据容器）。
/// 历史栈是 Core 的实现细节——DocumentService 维护独立 HistoryStack，
/// 经命令委托引用本模型，契约层不暴露历史；
/// 插件只通过本模型读写当前文档状态。
/// </summary>
public sealed class OsirisDocument
{
    /// <summary>画布宽（像素，创建后固定）。</summary>
    public int Width { get; init; }

    /// <summary>画布高（像素，创建后固定）。</summary>
    public int Height { get; init; }

    /// <summary>图层集合（索引 0 为底层；替换图层用不可变 Layer.With* 派生）。</summary>
    public List<Layer> Layers { get; } = [];

    /// <summary>活动选区（null 表示当前无选区）。</summary>
    public Selection? Selection { get; set; }

    /// <summary>新建空文档（无图层）。</summary>
    public static OsirisDocument Create(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        return new OsirisDocument { Width = width, Height = height };
    }
}
