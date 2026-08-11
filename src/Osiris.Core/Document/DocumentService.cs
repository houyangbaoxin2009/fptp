using Osiris.Abstractions.Document;
using Osiris.Core.History;

namespace Osiris.Core.Document;

/// <summary>
/// 文档服务：文档模型（OsirisDocument 不可变 Layer 装配）的唯一宿主与命令入口。
/// 职责：
/// - 持有当前文档与历史栈（HistoryStack 为 Core 实现细节，不暴露给契约层）；
/// - NewDocument/OpenDocument 建文档并清历史；
/// - ApplyCommand 驱动命令执行 → 压栈 → 触发 DocumentChanged（壳据此刷新画布）；
/// - Undo/Redo 经历史栈回退/重做并触发 DocumentChanged；
/// - ReplaceLayer 为命令内部工具（COW 指针替换，零拷贝）；
/// - 实现契约 IDocumentService（经 Services 注册），扩展模块（fptm 等）经接口编辑文档，不直接引用本类。
/// </summary>
public sealed class DocumentService : IDocumentService
{
    /// <summary>当前活动文档（无文档时为 null）。</summary>
    public OsirisDocument? Document { get; private set; }

    /// <summary>文档历史栈（命令簿记与撤销/重做）。</summary>
    public HistoryStack History { get; } = new();

    /// <summary>文档内容变化通知（新建/打开/命令执行/撤销/重做后触发）。</summary>
    public event Action? DocumentChanged;

    /// <summary>活动图层变化通知（携带新活动图层 Id）。</summary>
    public event Action<string>? ActiveLayerChanged;

    /// <summary>当前活动图层 Id（null = 无活动图层）。</summary>
    public string? ActiveLayerId { get; private set; }

    /// <summary>新建空文档（无图层）并清空历史。</summary>
    public void NewDocument(int width, int height)
    {
        Document = OsirisDocument.Create(width, height);
        History.Clear();
        ActiveLayerId = null;
        DocumentChanged?.Invoke();
    }

    /// <summary>打开文档：以背景像素面建单层文档（图层"背景"），清空历史。</summary>
    public void OpenDocument(PixelSurface background)
    {
        ArgumentNullException.ThrowIfNull(background);

        var document = OsirisDocument.Create(background.Width, background.Height);
        document.Layers.Add(new Layer(background) { Name = "背景" });

        Document = document;
        History.Clear();
        SetActiveLayer(document.Layers[0].Id);
        DocumentChanged?.Invoke();
    }

    /// <summary>执行命令：命令操作文档 → 压入历史栈 → 触发变更通知。</summary>
    public void ApplyCommand(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Document is not { } document)
            throw new InvalidOperationException("当前无文档，无法执行命令。");

        command.Execute(document);
        History.Push(command);
        DocumentChanged?.Invoke();
    }

    /// <summary>撤销最近一次命令（无文档/无可撤销时静默忽略）。</summary>
    public void Undo()
    {
        if (Document is { } document && History.CanUndo)
        {
            History.Undo(document);
            DocumentChanged?.Invoke();
        }
    }

    /// <summary>重做最近一次撤销（无文档/无可重做时静默忽略）。</summary>
    public void Redo()
    {
        if (Document is { } document && History.CanRedo)
        {
            History.Redo(document);
            DocumentChanged?.Invoke();
        }
    }

    /// <summary>
    /// 应用图层像素变更（IDocumentService 契约入口，供扩展模块调用）：
    /// 把图层从 oldLayer 替换为 newLayer，经历史栈（ApplyFilterCommand）可撤销/重做。
    /// </summary>
    public void ApplyLayerChange(string layerId, Layer oldLayer, Layer newLayer)
        => ApplyCommand(new ApplyFilterCommand(this, layerId, oldLayer, newLayer));

    /// <summary>
    /// 设置文档选区（IDocumentService 契约入口，供扩展模块调用）：
    /// null 表示清除选区；经历史栈（SelectionEditCommand，before=当前选区）可撤销/重做。
    /// </summary>
    public void SetSelection(Selection? selection)
    {
        if (Document is not { } document)
            throw new InvalidOperationException("当前无文档，无法设置选区。");
        ApplyCommand(new SelectionEditCommand(document.Selection, selection));
    }

    /// <summary>设置活动图层并触发通知。</summary>
    public void SetActiveLayer(string layerId)
    {
        ActiveLayerId = layerId;
        ActiveLayerChanged?.Invoke(layerId);
    }

    /// <summary>
    /// 按 Id 替换文档中的图层（命令内部工具，COW 语义：newLayer 为 with 派生的不可变引用，
    /// 撤销 = 指针回退到 oldLayer，零拷贝）。本方法不触发 DocumentChanged——
    /// 由调用方（ApplyCommand/Undo/Redo）在整条操作完成后统一通知。
    /// </summary>
    public void ReplaceLayer(string layerId, Layer newLayer)
    {
        ArgumentNullException.ThrowIfNull(newLayer);
        if (Document is not { } document)
            return;

        int index = document.Layers.FindIndex(layer => layer.Id == layerId);
        if (index >= 0)
            document.Layers[index] = newLayer;
    }
}
