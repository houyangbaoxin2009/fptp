using Osiris.Abstractions.Document;

namespace Osiris.Core.History;

/// <summary>
/// 选区编辑命令：套索/矩形选框一笔 = 一个命令（不是每鼠标事件一个，符合架构 6 节约定）。
/// before/after 为选区蒙版深拷贝（Selection.Clone），撤销/重做直接替换 document.Selection。
/// </summary>
public sealed class SelectionEditCommand : IUndoableCommand
{
    private readonly Selection? _before;
    private readonly Selection? _after;

    /// <summary>构造：记录变换前后选区（null = 无选区）。</summary>
    public SelectionEditCommand(Selection? before, Selection? after)
    {
        _before = before;
        _after = after;
    }

    /// <inheritdoc />
    public string Name { get; } = "修改选区";

    /// <inheritdoc />
    public void Execute(OsirisDocument document) => document.Selection = _after;

    /// <inheritdoc />
    public void Undo(OsirisDocument document) => document.Selection = _before;

    /// <inheritdoc />
    public void Redo(OsirisDocument document) => document.Selection = _after;
}
