namespace Osiris.Abstractions.Document;

/// <summary>
/// 文档服务契约：文档编辑/历史操作的宿主服务抽象。
/// Core 的 DocumentService 实现本接口并注册进 Services（host.Services.Get&lt;IDocumentService&gt;()），
/// 扩展模块（fptm 等）经此编辑文档——避免引用 Core 实现（ABI 红线）。
/// 历史语义：文档变更全部经历史栈（命令模式，COW 不可变 Layer，可撤销/重做）。
/// </summary>
public interface IDocumentService
{
    /// <summary>当前活动文档（无文档时 null）。</summary>
    OsirisDocument? Document { get; }

    /// <summary>文档变更事件（打开/撤销/重做/图层变更后触发）。</summary>
    event Action? DocumentChanged;

    /// <summary>以背景图层打开新文档（清空历史）。</summary>
    void OpenDocument(PixelSurface background);

    /// <summary>应用图层像素变更（oldLayer → newLayer，经历史栈，可撤销）。</summary>
    void ApplyLayerChange(string layerId, Layer oldLayer, Layer newLayer);

    /// <summary>设置文档选区（null=清除；经历史栈，可撤销）。</summary>
    void SetSelection(Selection? selection);

    /// <summary>撤销上一步。</summary>
    void Undo();

    /// <summary>重做（向前一步）。</summary>
    void Redo();
}
