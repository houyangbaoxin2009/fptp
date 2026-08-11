using Osiris.Abstractions.Document;

namespace Osiris.Core.History;

/// <summary>
/// 线性撤销/重做栈（List + 游标，架构 6 节）：
/// 只做命令簿记（Push 不执行命令——执行由 DocumentService.ApplyCommand 负责）；
/// Undo/Redo 调用命令的 Undo/Redo 方法并把文档推进/回退，同时移动游标。
/// 新 Push 会丢弃当前游标之后的 redo 分支；超 MaxDepth 裁剪最旧命令控内存。
/// </summary>
public sealed class HistoryStack
{
    /// <summary>历史深度上限（超出裁剪最旧命令，控内存；架构约定 30~50）。</summary>
    public const int MaxDepth = 50;

    // 命令列表：游标之前的为已执行命令（可 Undo），游标之后为 redo 分支
    private readonly List<IUndoableCommand> _commands = [];

    // 游标：-1 = 无已执行命令；指向最近一次已执行命令
    private int _cursor = -1;

    /// <summary>历史变化通知（入栈/撤销/重做/清空后触发）。壳据此刷新画布与历史面板。</summary>
    public event Action? Changed;

    /// <summary>是否存在可撤销的命令。</summary>
    public bool CanUndo => _cursor >= 0;

    /// <summary>是否存在可重做的命令。</summary>
    public bool CanRedo => _cursor < _commands.Count - 1;

    /// <summary>当前游标位置（-1 = 无已执行命令）。</summary>
    public int Cursor => _cursor;

    /// <summary>全部命令（含已撤销的 redo 分支，历史面板展示用）。</summary>
    public IReadOnlyList<IUndoableCommand> Commands => _commands;

    /// <summary>压入已执行完成的命令：丢弃 redo 分支、裁剪深度、触发通知。</summary>
    public void Push(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 新分支：丢弃当前游标之后的 redo 分支
        if (_cursor < _commands.Count - 1)
            _commands.RemoveRange(_cursor + 1, _commands.Count - _cursor - 1);

        _commands.Add(command);
        _cursor++;
        TrimToDepth();
        Changed?.Invoke();
    }

    /// <summary>撤销：调用当前命令的 Undo 并回退游标。</summary>
    public void Undo(OsirisDocument document)
    {
        if (!CanUndo)
            return;
        _commands[_cursor].Undo(document);
        _cursor--;
        Changed?.Invoke();
    }

    /// <summary>重做：前移游标并调用命令的 Redo。</summary>
    public void Redo(OsirisDocument document)
    {
        if (!CanRedo)
            return;
        _cursor++;
        _commands[_cursor].Redo(document);
        Changed?.Invoke();
    }

    /// <summary>清空历史（新文档/打开文档时调用）。</summary>
    public void Clear()
    {
        _commands.Clear();
        _cursor = -1;
        Changed?.Invoke();
    }

    /// <summary>超限裁剪最旧命令（游标同步前移，保持指向同一命令）。</summary>
    private void TrimToDepth()
    {
        while (_commands.Count > MaxDepth)
        {
            _commands.RemoveAt(0);
            _cursor--;
        }
    }
}
