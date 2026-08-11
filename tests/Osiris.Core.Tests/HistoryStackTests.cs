using Osiris.Abstractions.Document;
using Osiris.Core.History;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// HistoryStack 撤销/重做栈测试：游标语义、MaxDepth 深度裁剪、redo 分支丢弃、清空。
/// </summary>
public class HistoryStackTests
{
    /// <summary>测试桩命令：记录 Execute/Undo/Redo 调用次数，供断言驱动行为。</summary>
    private sealed class CountingCommand(string name) : IUndoableCommand
    {
        public int ExecuteCount;
        public int UndoCount;
        public int RedoCount;

        public string Name { get; } = name;

        public void Execute(OsirisDocument document) => ExecuteCount++;

        public void Undo(OsirisDocument document) => UndoCount++;

        public void Redo(OsirisDocument document) => RedoCount++;
    }

    [Fact]
    public void Push_Undo_Redo_InvokesCommandsAndTracksState()
    {
        // 意图：Push 后 Undo 调用当前命令的 Undo 并回退游标，Redo 反向；CanUndo/CanRedo 随之变化。
        var stack = new HistoryStack();
        OsirisDocument doc = OsirisDocument.Create(2, 2);
        var a = new CountingCommand("A");
        var b = new CountingCommand("B");

        stack.Push(a);
        stack.Push(b);
        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);

        stack.Undo(doc);
        Assert.Equal(0, a.UndoCount);
        Assert.Equal(1, b.UndoCount); // 撤销的是最近压入的 B
        Assert.True(stack.CanUndo);
        Assert.True(stack.CanRedo);

        stack.Redo(doc);
        Assert.Equal(1, b.RedoCount);
        Assert.False(stack.CanRedo);

        stack.Undo(doc);
        stack.Undo(doc);
        Assert.Equal(1, a.UndoCount); // A 只被撤销一次（B 被撤销两次）
        Assert.False(stack.CanUndo);

        stack.Redo(doc);
        stack.Redo(doc);
        Assert.Equal(1, a.RedoCount);
        Assert.Equal(2, b.RedoCount); // 计数为累计值：B 的 Undo/Redo 各被调用两次
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Push_60Commands_TrimsOldest_KeepsMaxDepth()
    {
        // 意图：压入 60 个命令超出 MaxDepth(50) 时裁剪最旧 10 个，游标仍指向最新命令。
        var stack = new HistoryStack();
        OsirisDocument doc = OsirisDocument.Create(2, 2);
        var commands = Enumerable.Range(1, 60).Select(i => new CountingCommand($"C{i}")).ToArray();

        foreach (CountingCommand command in commands)
            stack.Push(command);

        Assert.Equal(HistoryStack.MaxDepth, stack.Commands.Count);
        Assert.DoesNotContain(commands[0], stack.Commands);   // C1 被裁剪
        Assert.DoesNotContain(commands[9], stack.Commands);   // C10 被裁剪
        Assert.Equal(commands[10], stack.Commands[0]);        // 剩下 C11..C60
        Assert.Equal(commands[59], stack.Commands[^1]);       // 最新命令仍在栈顶
        Assert.True(stack.CanUndo);                           // 游标指向最新命令
    }

    [Fact]
    public void Push_AfterUndo_DiscardsRedoBranch()
    {
        // 意图：撤销后新 Push 会丢弃 redo 分支（历史不允许回到被丢弃的未来）。
        var stack = new HistoryStack();
        OsirisDocument doc = OsirisDocument.Create(2, 2);
        var a = new CountingCommand("A");
        var b = new CountingCommand("B");
        var c = new CountingCommand("C");

        stack.Push(a);
        stack.Push(b);
        stack.Undo(doc);   // 游标回到 A，B 成为 redo 分支
        stack.Push(c);     // 新分支：B 被丢弃

        Assert.Equal(2, stack.Commands.Count);
        Assert.Equal(a, stack.Commands[0]);
        Assert.Equal(c, stack.Commands[1]);
        Assert.DoesNotContain(b, stack.Commands);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Clear_EmptiesStack()
    {
        // 意图：Clear 清空全部命令并复位游标（新文档/打开文档时调用）。
        var stack = new HistoryStack();
        OsirisDocument doc = OsirisDocument.Create(2, 2);
        stack.Push(new CountingCommand("A"));

        stack.Clear();

        Assert.Empty(stack.Commands);
        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }
}
