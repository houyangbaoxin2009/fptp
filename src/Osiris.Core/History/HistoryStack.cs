namespace Osiris.Core.History
{
    /// <summary>撤销/重做命令接口（命令模式）。</summary>
    public interface IUndoableCommand
    {
        string Name { get; }
        void Execute(Document.OsirisDocument doc);
        void Undo(Document.OsirisDocument doc);
        void Redo(Document.OsirisDocument doc);
    }

    /// <summary>线性撤销/重做栈。</summary>
    public sealed class HistoryStack
    {
        private readonly System.Collections.Generic.Stack<IUndoableCommand> _undo =
            new System.Collections.Generic.Stack<IUndoableCommand>();
        private readonly System.Collections.Generic.Stack<IUndoableCommand> _redo =
            new System.Collections.Generic.Stack<IUndoableCommand>();

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Push(IUndoableCommand cmd, Document.OsirisDocument doc)
        {
            cmd.Execute(doc);
            _undo.Push(cmd);
            _redo.Clear();
        }

        public void Undo(Document.OsirisDocument doc)
        {
            if (_undo.Count == 0) return;
            var cmd = _undo.Pop();
            cmd.Undo(doc);
            _redo.Push(cmd);
        }

        public void Redo(Document.OsirisDocument doc)
        {
            if (_redo.Count == 0) return;
            var cmd = _redo.Pop();
            cmd.Redo(doc);
            _undo.Push(cmd);
        }
    }
}
