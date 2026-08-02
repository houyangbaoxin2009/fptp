using System;
using System.Collections.Generic;
using Osiris.Core.Document;

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

    /// <summary>
    /// 线性撤销/重做栈（List + 游标）。
    /// 支持历史面板：命令列表、点击跳转、变更通知、内存上限。
    /// </summary>
    public sealed class HistoryStack
    {
        /// <summary>历史深度上限（超出裁剪最旧命令，控内存）。</summary>
        public const int MaxDepth = 100;

        private readonly List<IUndoableCommand> _commands = new List<IUndoableCommand>();
        private int _cursor = -1;

        /// <summary>历史变化通知（入栈/撤销/重做/跳转/清空后触发）。壳据此刷新画布与面板。</summary>
        public event EventHandler Changed;

        public bool CanUndo => _cursor >= 0;
        public bool CanRedo => _cursor < _commands.Count - 1;
        /// <summary>当前游标位置（-1 = 无已执行命令）。</summary>
        public int Cursor => _cursor;
        /// <summary>全部命令（含已撤销的 redo 分支，面板展示用）。</summary>
        public IReadOnlyList<IUndoableCommand> Commands => _commands;

        public void Push(IUndoableCommand cmd, OsirisDocument doc)
        {
            cmd.Execute(doc);
            // 新分支：丢弃当前游标之后的 redo 分支
            if (_cursor < _commands.Count - 1)
                _commands.RemoveRange(_cursor + 1, _commands.Count - _cursor - 1);
            _commands.Add(cmd);
            _cursor++;
            TrimToDepth();
            OnChanged();
        }

        public void Undo(OsirisDocument doc)
        {
            if (!CanUndo) return;
            _commands[_cursor].Undo(doc);
            _cursor--;
            OnChanged();
        }

        public void Redo(OsirisDocument doc)
        {
            if (!CanRedo) return;
            _cursor++;
            _commands[_cursor].Redo(doc);
            OnChanged();
        }

        /// <summary>历史面板点击跳转：撤销/重做到指定命令索引。</summary>
        public void JumpTo(int index, OsirisDocument doc)
        {
            if (index < -1 || index >= _commands.Count) return;
            while (_cursor > index) { _commands[_cursor].Undo(doc); _cursor--; }
            while (_cursor < index) { _cursor++; _commands[_cursor].Redo(doc); }
            OnChanged();
        }

        public void Clear()
        {
            _commands.Clear();
            _cursor = -1;
            OnChanged();
        }

        private void TrimToDepth()
        {
            while (_commands.Count > MaxDepth)
            {
                _commands.RemoveAt(0);
                _cursor--;
            }
        }

        private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
