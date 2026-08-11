using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Fptm.Editing;

namespace Fptm.Tools;

/// <summary>颜料桶：点击区域填充（颜色容差 BFS 选区 → 目标色填充 → 历史命令，可撤销）。</summary>
public sealed class PaintBucketTool : IEditorTool
{
    private IHostContext? _host;

    /// <inheritdoc />
    public string Id => "bucket";

    /// <inheritdoc />
    public string DisplayName => "颜料桶";

    /// <inheritdoc />
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public void Initialize(IHostContext host) => _host = host;

    /// <inheritdoc />
    public void MouseDown(ToolMouseEvent e)
    {
        if (e.Button != ToolMouseButton.Left) return;
        var docs = _host?.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (docs is null || doc is null || doc.Layers.Count == 0) return;
        if ((uint)e.X >= (uint)doc.Width || (uint)e.Y >= (uint)doc.Height) return;

        Layer layer = doc.Layers[0];
        Selection region = FloodFill.SelectRegion(layer.Pixels, e.X, e.Y, 32);
        if (region is null) return;
        PixelSurface filled = FloodFill.FillRegion(layer.Pixels, region, Editing.ToolState.Instance.GetColor("bucket"));
        Layer newLayer = layer.WithPixels(filled);
        docs.ApplyLayerChange(layer.Id, layer, newLayer);
    }

    /// <inheritdoc />
    public void MouseMove(ToolMouseEvent e) { }

    /// <inheritdoc />
    public void MouseUp(ToolMouseEvent e) { }

    /// <inheritdoc />
    public void DrawOverlay(IToolOverlay overlay) { }

    /// <inheritdoc />
    /// <inheritdoc />
    public event Action? VisualChanged { add { } remove { } } // 一次性操作无中间视觉状态
    
    public void Activate() { }

    /// <inheritdoc />
    public void Deactivate() { }
}




