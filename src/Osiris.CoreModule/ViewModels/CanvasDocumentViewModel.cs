using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Osiris.Core.Document;

namespace Osiris.CoreModule.ViewModels;

/// <summary>
/// 画布文档视图模型：画布状态（文档/视口/工具）的唯一数据源。
/// Dock 浮动/停靠移动画布时，模板每次生成新的 CanvasControl 并绑定同一 VM——
/// 状态（文档/缩放/偏移/工具）不丢，控件实例无双重父级（修复"画布浮动崩溃"：
/// 直接把 CanvasControl 实例设为 Dock 文档 Context 导致 Avalonia 拒绝双父级）。
/// </summary>
public sealed partial class CanvasDocumentViewModel : ObservableObject
{
    // 缩放限幅：与旧 CanvasControl 常量一致（0.05x ~ 64x）。
    private const double MinScale = 0.05;
    private const double MaxScale = 64.0;
    private const double FitMargin = 16;

    private readonly DocumentService _documents;

    /// <summary>控件最近一次可视尺寸（CanvasControl SizeChanged 回写；缩放适配用）。</summary>
    public Size LastViewSize { get; set; }

    public CanvasDocumentViewModel(DocumentService documents)
    {
        _documents = documents;
        documents.DocumentChanged += OnDocumentChanged;
    }

    /// <summary>文档变更（打开/撤销/重做后）→ 同步文档并自动缩放适配。</summary>
    private void OnDocumentChanged()
    {
        Document = _documents.Document;
        Revision++;
        ZoomFit();
    }

    /// <summary>当前渲染的文档（无文档时画布显示空白底色）。</summary>
    [ObservableProperty] private OsirisDocument? document;

    /// <summary>文档修订号：内容变化后递增，触发画布重绘。</summary>
    [ObservableProperty] private int revision;

    /// <summary>当前激活工具（宿主设置；渲染期调用其 DrawOverlay）。</summary>
    [ObservableProperty] private IEditorTool? activeTool;

    /// <summary>当前缩放比例（1.0 = 实际大小）。</summary>
    [ObservableProperty] private double scale = 1.0;

    /// <summary>视口偏移 X：文档原点在控件坐标中的位置。</summary>
    [ObservableProperty] private double offsetX;

    /// <summary>视口偏移 Y。</summary>
    [ObservableProperty] private double offsetY;

    /// <summary>以控件坐标 (controlX, controlY) 为锚点缩放到 newScale（锚定公式 Offset′ = p − (p − Offset) × (s′/s)）。</summary>
    public void ZoomAt(double controlX, double controlY, double newScale)
    {
        newScale = Math.Clamp(newScale, MinScale, MaxScale);
        if (Scale <= 0)
            return;

        double ratio = newScale / Scale;
        OffsetX = controlX - (controlX - OffsetX) * ratio;
        OffsetY = controlY - (controlY - OffsetY) * ratio;
        Scale = newScale;
    }

    /// <summary>缩放适配：整份文档居中填入可视区（等比缩放，留边距）。</summary>
    public void ZoomFit()
    {
        if (Document is not { } doc)
            return;
        Size size = LastViewSize;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        double availW = Math.Max(1.0, size.Width - FitMargin * 2);
        double availH = Math.Max(1.0, size.Height - FitMargin * 2);
        Scale = Math.Clamp(Math.Min(availW / doc.Width, availH / doc.Height), MinScale, MaxScale);
        OffsetX = (size.Width - doc.Width * Scale) / 2;
        OffsetY = (size.Height - doc.Height * Scale) / 2;
    }

    /// <summary>实际大小：Scale=1.0 并把文档居中。</summary>
    public void ZoomActual()
    {
        if (Document is not { } doc)
            return;
        Size size = LastViewSize;
        Scale = 1.0;
        OffsetX = (size.Width - doc.Width) / 2;
        OffsetY = (size.Height - doc.Height) / 2;
    }
}
