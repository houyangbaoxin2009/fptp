using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Fptm.Editing;

namespace Fptm.Tools;

/// <summary>选取：矩形选区工具（拖拽矩形，提交为文档选区，可撤销）。</summary>
public sealed class SelectRectTool : IEditorTool
{
    private IHostContext? _host;
    private Point2 _start;
    private Point2 _end;
    private bool _dragging;

    /// <inheritdoc />
    public string Id => "selectRect";

    /// <inheritdoc />
    public string DisplayName => "选取";

    /// <inheritdoc />
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

    /// <inheritdoc />
    public void Initialize(IHostContext host) => _host = host;

    /// <inheritdoc />
    public void MouseDown(ToolMouseEvent e)
    {
        if (e.Button != ToolMouseButton.Left) return;
        _start = new Point2(e.X, e.Y);
        _end = _start;
        _dragging = true;
    }

    /// <inheritdoc />
    public void MouseMove(ToolMouseEvent e)
    {
        if (_dragging)
        {
            _end = new Point2(e.X, e.Y);
            VisualChanged?.Invoke(); // 拖动中实时刷新覆盖层（矩形预览）
        }
    }

    /// <inheritdoc />
    public void MouseUp(ToolMouseEvent e)
    {
        if (!_dragging) return;
        _dragging = false;
        var doc = _host?.Services.Get<IDocumentService>()?.Document;
        if (doc is null) return;
        var sel = new Selection(doc.Width, doc.Height);
        int x0 = Math.Min(_start.X, _end.X);
        int y0 = Math.Min(_start.Y, _end.Y);
        sel.SetRect(x0, y0, Math.Abs(_end.X - _start.X) + 1, Math.Abs(_end.Y - _start.Y) + 1);
        _host?.Services.Get<IDocumentService>()?.SetSelection(sel);
    }

    /// <inheritdoc />
    public void DrawOverlay(IToolOverlay overlay)
    {
        if (!_dragging) return;
        overlay.DrawPolyline(
        [
            _start,
            new Point2(_end.X, _start.Y),
            _end,
            new Point2(_start.X, _end.Y),
        ], closed: true);
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public event Action? VisualChanged;
    
    public void Activate() { }

    /// <inheritdoc />
    public void Deactivate() { }
}

/// <summary>套索：手绘多边形选区（自由圈选，闭合后提交，可撤销）。</summary>
public sealed class LassoTool : IEditorTool
{
    private IHostContext? _host;
    private readonly List<Point2> _points = [];
    private bool _dragging;

    /// <inheritdoc />
    public string Id => "lasso";

    /// <inheritdoc />
    public string DisplayName => "套索";

    /// <inheritdoc />
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

    /// <inheritdoc />
    public void Initialize(IHostContext host) => _host = host;

    /// <inheritdoc />
    public void MouseDown(ToolMouseEvent e)
    {
        if (e.Button != ToolMouseButton.Left) return;
        _points.Clear();
        _points.Add(new Point2(e.X, e.Y));
        _dragging = true;
    }

    /// <inheritdoc />
    public void MouseMove(ToolMouseEvent e)
    {
        if (!_dragging) return;
        var p = new Point2(e.X, e.Y);
        if (_points.Count == 0 || _points[^1] != p)
        {
            _points.Add(p);
            VisualChanged?.Invoke(); // 收集点实时刷新覆盖层（套索轨迹）
        }
    }

    /// <inheritdoc />
    public void MouseUp(ToolMouseEvent e)
    {
        if (!_dragging) return;
        _dragging = false;
        var docs = _host?.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (docs is null || doc is null || _points.Count < 3) return;
        var sel = new Selection(doc.Width, doc.Height);
        sel.SetPolygon(_points);
        docs.SetSelection(sel);
        _points.Clear();
    }

    /// <inheritdoc />
    public void DrawOverlay(IToolOverlay overlay)
    {
        if (_dragging && _points.Count >= 2)
            overlay.DrawPolyline(_points, closed: true);
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public event Action? VisualChanged;
    
    public void Activate() { }

    /// <inheritdoc />
    public void Deactivate() { }
}

/// <summary>智能框选：魔棒（点击处颜色容差 BFS 选区），tolerance 固定 60。</summary>
public sealed class MagicWandTool : IEditorTool
{
    private IHostContext? _host;

    /// <inheritdoc />
    public string Id => "magicWand";

    /// <inheritdoc />
    public string DisplayName => "智能框选";

    /// <inheritdoc />
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

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
        Selection sel = FloodFill.SelectRegion(doc.Layers[0].Pixels, e.X, e.Y, 60);
        docs.SetSelection(sel);
    }

    /// <inheritdoc />
    public void MouseMove(ToolMouseEvent e) { }

    /// <inheritdoc />
    public void MouseUp(ToolMouseEvent e) { }

    /// <inheritdoc />
    public void DrawOverlay(IToolOverlay overlay) { }

    /// <inheritdoc />
    /// <inheritdoc />
    public event Action? VisualChanged;
    
    public void Activate() { }

    /// <inheritdoc />
    public void Deactivate() { }
}

