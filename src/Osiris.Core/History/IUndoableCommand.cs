using Osiris.Abstractions.Document;

namespace Osiris.Core.History;

/// <summary>
/// 撤销/重做命令接口（命令模式）：一次用户操作 = 一个命令。
/// 命令直接操作 OsirisDocument（不可变 Layer 经 with 派生替换，COW 零拷贝）；
/// 执行由 DocumentService.ApplyCommand 驱动，Undo/Redo 由 HistoryStack 驱动。
/// </summary>
public interface IUndoableCommand
{
    /// <summary>命令显示名（历史面板展示，如 "滤镜: 灰度"）。</summary>
    string Name { get; }

    /// <summary>执行命令（把文档从"变换前"推进到"变换后"）。</summary>
    void Execute(OsirisDocument document);

    /// <summary>撤销命令（回退到变换前状态）。</summary>
    void Undo(OsirisDocument document);

    /// <summary>重做命令（再次应用变换）。</summary>
    void Redo(OsirisDocument document);
}
