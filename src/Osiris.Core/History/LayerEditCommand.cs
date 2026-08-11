using Osiris.Abstractions.Document;

namespace Osiris.Core.History;

/// <summary>
/// 图层增删命令：新建/删除图层的可撤销操作（记录操作类型与目标图层）。
/// 新增图层在指定索引插入（缺省追加到顶层）；删除图层按 Id 定位并记录实际索引，
/// 撤销/重做保持对称（加 ⇄ 删）。
/// </summary>
public sealed class LayerEditCommand : IUndoableCommand
{
    /// <summary>图层编辑操作类型。</summary>
    public enum LayerEditKind
    {
        /// <summary>新建图层。</summary>
        Add,

        /// <summary>删除图层。</summary>
        Remove,
    }

    private readonly LayerEditKind _kind;
    private readonly Layer _layer;

    // 目标索引：Add 由构造指定（-1 = 追加到顶层）；Remove 在 Execute 时捕获实际位置
    private int _index;

    /// <summary>构造图层编辑命令。</summary>
    /// <param name="index">Add 时的插入位置（-1 = 追加到图层栈顶）；Remove 忽略。</param>
    public LayerEditCommand(LayerEditKind kind, Layer layer, int index = -1)
    {
        _kind = kind;
        _layer = layer ?? throw new ArgumentNullException(nameof(layer));
        _index = index;
        Name = kind == LayerEditKind.Add ? $"新建图层: {layer.Name}" : $"删除图层: {layer.Name}";
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public void Execute(OsirisDocument document)
    {
        switch (_kind)
        {
            case LayerEditKind.Add:
                Insert(document);
                break;
            case LayerEditKind.Remove:
                _index = document.Layers.FindIndex(layer => layer.Id == _layer.Id);
                if (_index >= 0)
                    document.Layers.RemoveAt(_index);
                break;
        }
    }

    /// <inheritdoc />
    public void Undo(OsirisDocument document)
    {
        // 对称回退：Add 撤销 = 删除该图层；Remove 撤销 = 在捕获位置恢复
        switch (_kind)
        {
            case LayerEditKind.Add:
                int removeAt = document.Layers.FindIndex(layer => layer.Id == _layer.Id);
                if (removeAt >= 0)
                    document.Layers.RemoveAt(removeAt);
                break;
            case LayerEditKind.Remove:
                if (_index >= 0)
                    document.Layers.Insert(Math.Min(_index, document.Layers.Count), _layer);
                break;
        }
    }

    /// <inheritdoc />
    public void Redo(OsirisDocument document) => Execute(document);

    /// <summary>插入图层到目标位置（越界钳制，缺省追加顶层）。</summary>
    private void Insert(OsirisDocument document)
    {
        int position = _index < 0 ? document.Layers.Count : Math.Min(_index, document.Layers.Count);
        document.Layers.Insert(position, _layer);
    }
}
