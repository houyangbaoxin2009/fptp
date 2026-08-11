using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Osiris.Engine.Skia;
using SkiaSharp;

namespace Osiris.CoreModule.Controls;

/// <summary>
/// 画布控件（架构文档第 7 节"渲染协议"实现）：
/// - 渲染：Control.Render(DrawingContext) → context.Custom(CanvasDrawOperation)，
///   op 的 Render(ImmediateDrawingContext) 里 TryGetFeature&lt;ISkiaSharpApiLeaseFeature&gt;()
///   取 SKCanvas 直绘（零拷贝、自动 DPI）；文档经 DocumentRenderer 逐层合成。
/// - 视口状态：(Offset, Scale)，渲染时 canvas.Translate(Offset) + canvas.Scale(Scale)。
/// - 命中测试：inverse(M) × e.GetPosition(this) → 文档像素坐标 → 组 ToolMouseEvent 交给 ActiveTool。
/// - 缩放锚定公式（滚轮/捏合在控件坐标 p，目标缩放 s′）：Offset′ = p − (p − Offset) × (s′/s)。
/// - Revision 计数器变化触发 InvalidateVisual（性能：仅计数变化时重绘）。
/// - 平移：中键或空格+左键拖拽。
/// </summary>
public sealed class CanvasControl : Control
{
    // 缩放限幅：避免缩到无穷大/无穷小（0.05x ~ 64x）。
    private const double MinScale = 0.05;
    private const double MaxScale = 64.0;

    // 文档模型与渲染状态
    private OsirisDocument? _document;
    private int _revision;
    private IEditorTool? _activeTool;

    // 视口状态：Offset = 文档原点在控件坐标中的位置，Scale = 放大倍数（1.0 = 100%）。
    private double _scale = 1.0;
    private Vector _offset;

    // 平移交互状态
    private bool _panning;          // 正在拖拽平移
    private bool _spacePressed;     // 空格键按住（空格+左键平移）
    private Point _lastPointer;     // 上一次指针位置（拖拽增量计算用）

    // 工具覆盖层代理：收集工具绘制的折线，渲染期统一以蚂蚁线画到 SKCanvas。
    private readonly CanvasOverlayProxy _overlayProxy = new();

    public CanvasControl()
    {
        // 键盘事件（空格键平移修饰）需要控件可聚焦；超界内容裁剪，避免画到控件外。
        Focusable = true;
        ClipToBounds = true;

        KeyDown += (_, e) => { if (e.Key == Key.Space) _spacePressed = true; };
        KeyUp += (_, e) => { if (e.Key == Key.Space) _spacePressed = false; };
    }

    /// <summary>当前渲染的文档（无文档时画布只显示空白底色）。</summary>
    public OsirisDocument? Document
    {
        get => _document;
        set
        {
            if (ReferenceEquals(_document, value))
                return;
            _document = value;
            InvalidateVisual();
        }
    }

    /// <summary>文档修订号：文档内容变化后宿主递增，触发画布重绘（架构第 7 节性能约定）。</summary>
    public int Revision
    {
        get => _revision;
        set
        {
            if (_revision == value)
                return;
            _revision = value;
            InvalidateVisual();
        }
    }

    /// <summary>当前激活工具：鼠标事件经 ToolEvent 转发给宿主路由到该工具；渲染期调用其 DrawOverlay。</summary>
    public IEditorTool? ActiveTool
    {
        get => _activeTool;
        set
        {
            if (ReferenceEquals(_activeTool, value))
                return;
            _activeTool = value;
            InvalidateVisual();
        }
    }

    /// <summary>工具覆盖层回调：工具经 DrawOverlay(overlay) 写入的折线由此代理收集并在渲染期绘制。</summary>
    public IToolOverlay Overlay => _overlayProxy;

    /// <summary>当前缩放比例（1.0 = 实际大小）。</summary>
    public double Scale => _scale;

    /// <summary>视口偏移：文档原点在控件坐标中的位置。</summary>
    public Vector Offset => _offset;

    /// <summary>
    /// 工具鼠标事件（文档像素坐标，已做逆视口变换）。
    /// 宿主订阅后转发给当前激活工具（ActiveTool.MouseDown/MouseMove/MouseUp）。
    /// </summary>
    public event Action<ToolMouseEvent>? ToolEvent;

    /// <summary>
    /// 以控件坐标 p 为锚点缩放到 newScale（架构第 7 节锚定公式）：
    /// Offset′ = p − (p − Offset) × (s′/s)，缩放后锚点下的文档点保持不动。
    /// </summary>
    public void ZoomAt(Point controlPoint, double newScale)
    {
        newScale = Math.Clamp(newScale, MinScale, MaxScale);
        if (_scale <= 0)
            return;

        double ratio = newScale / _scale;
        _offset = new Vector(
            controlPoint.X - (controlPoint.X - _offset.X) * ratio,
            controlPoint.Y - (controlPoint.Y - _offset.Y) * ratio);
        _scale = newScale;
        InvalidateVisual();
    }

    /// <summary>缩放适配：整份文档居中填入控件可视区（等比缩放，留边距）。</summary>
    public void ZoomFit()
    {
        if (_document is null)
            return;

        Size size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        const double margin = 16; // 画布四周留白（设备无关像素）
        double availW = Math.Max(1.0, size.Width - margin * 2);
        double availH = Math.Max(1.0, size.Height - margin * 2);

        _scale = Math.Clamp(Math.Min(availW / _document.Width, availH / _document.Height), MinScale, MaxScale);
        _offset = new Vector(
            (size.Width - _document.Width * _scale) / 2,
            (size.Height - _document.Height * _scale) / 2);
        InvalidateVisual();
    }

    /// <summary>实际大小：Scale=1.0 并把文档居中。</summary>
    public void ZoomActual()
    {
        if (_document is null)
            return;

        _scale = 1.0;
        Size size = Bounds.Size;
        _offset = new Vector(
            (size.Width - _document.Width) / 2,
            (size.Height - _document.Height) / 2);
        InvalidateVisual();
    }

    /// <inheritdoc />
    /// 渲染入口：把视口状态与文档打包进自定义绘制操作，交给渲染线程执行
    /// （架构第 7 节：context.Custom(new CanvasDrawOperation(...))）。
    public override void Render(DrawingContext context)
        => context.Custom(new CanvasDrawOperation(
            new Rect(Bounds.Size), _document, _offset, _scale, _activeTool, _overlayProxy));

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus(); // 保证后续空格键/快捷键事件能到达本控件

        Point pos = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;

        // 中键 或 空格+左键 → 平移拖拽（架构第 7 节；与 2.0 一致）。
        if (props.IsMiddleButtonPressed || (_spacePressed && props.IsLeftButtonPressed))
        {
            _panning = true;
            _lastPointer = pos;
            e.Pointer.Capture(this); // 捕获指针，拖出控件外仍持续收到移动事件
            e.Handled = true;
            return;
        }

        // 其余左/右键 → 工具事件：控件坐标经逆视口变换得文档像素坐标。
        ToolEvent?.Invoke(new ToolMouseEvent(
            ToDocX(pos.X), ToDocY(pos.Y), ToButton(props.PointerUpdateKind), ToModifiers(e.KeyModifiers)));
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        Point pos = e.GetPosition(this);

        // 平移拖拽：视口偏移随指针增量移动。
        if (_panning)
        {
            Point delta = pos - _lastPointer;
            _offset = new Vector(_offset.X + delta.X, _offset.Y + delta.Y);
            _lastPointer = pos;
            InvalidateVisual();
            return;
        }

        // 普通移动 → 工具 Move 事件（按钮取当前按住键）。
        var props = e.GetCurrentPoint(this).Properties;
        ToolMouseButton button = props.IsMiddleButtonPressed ? ToolMouseButton.Middle
            : props.IsRightButtonPressed ? ToolMouseButton.Right
            : ToolMouseButton.Left;
        ToolEvent?.Invoke(new ToolMouseEvent(ToDocX(pos.X), ToDocY(pos.Y), button, ToModifiers(e.KeyModifiers)));
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        Point pos = e.GetPosition(this);

        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null); // 释放指针捕获
            e.Handled = true;
            return;
        }

        // 工具 Up 事件。
        ToolEvent?.Invoke(new ToolMouseEvent(
            ToDocX(pos.X), ToDocY(pos.Y),
            ToButton(e.GetCurrentPoint(this).Properties.PointerUpdateKind),
            ToModifiers(e.KeyModifiers)));
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        // Ctrl+滚轮缩放（与 2.0 一致）：以光标位置为锚点，每格 ±10%。
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            double factor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
            ZoomAt(e.GetPosition(this), _scale * factor);
            e.Handled = true;
        }
        // 无 Ctrl 的滚轮不做处理（保持 2.0 行为：仅 Ctrl+滚轮缩放）。
    }

    /// <summary>控件坐标 X → 文档像素坐标（逆视口变换：(px − Offset.X) / Scale）。</summary>
    private int ToDocX(double px) => (int)Math.Round((px - _offset.X) / _scale);

    /// <summary>控件坐标 Y → 文档像素坐标（逆视口变换）。</summary>
    private int ToDocY(double py) => (int)Math.Round((py - _offset.Y) / _scale);

    /// <summary>Avalonia PointerUpdateKind → 契约 ToolMouseButton。</summary>
    private static ToolMouseButton ToButton(PointerUpdateKind kind) => kind switch
    {
        PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => ToolMouseButton.Middle,
        PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => ToolMouseButton.Right,
        _ => ToolMouseButton.Left,
    };

    /// <summary>Avalonia KeyModifiers → 契约 ToolModifiers（[Flags]）。</summary>
    private static ToolModifiers ToModifiers(KeyModifiers km)
    {
        ToolModifiers modifiers = ToolModifiers.None;
        if (km.HasFlag(KeyModifiers.Control)) modifiers |= ToolModifiers.Control;
        if (km.HasFlag(KeyModifiers.Shift)) modifiers |= ToolModifiers.Shift;
        if (km.HasFlag(KeyModifiers.Alt)) modifiers |= ToolModifiers.Alt;
        return modifiers;
    }

    /// <summary>
    /// 工具覆盖层代理（IToolOverlay 实现）：工具在 DrawOverlay 期间调用 DrawPolyline
    /// 收集折线（文档像素坐标），渲染期由 RenderTo 统一以蚂蚁线（虚线）绘制到 SKCanvas。
    /// 收集而非直绘：DrawOverlay 可能在渲染线程外被宿主调用，延迟绘制保证线程安全。
    /// </summary>
    private sealed class CanvasOverlayProxy : IToolOverlay
    {
        // 已收集的折线：顶点数组 + 是否闭合。
        private readonly List<(Point2[] Points, bool Closed)> _polylines = [];

        /// <summary>当前收集的折线条数（渲染期判断是否有内容可画）。</summary>
        public int Count => _polylines.Count;

        /// <summary>清空上一帧收集内容（每帧渲染前调用）。</summary>
        public void Clear() => _polylines.Clear();

        /// <inheritdoc />
        /// 按顺序连线；closed 为 true 时末点自动闭合回起点。
        public void DrawPolyline(IReadOnlyList<Point2> points, bool closed)
        {
            ArgumentNullException.ThrowIfNull(points);
            if (points.Count < 2)
                return;
            _polylines.Add((points.ToArray(), closed));
        }

        /// <summary>
        /// 把收集的折线以虚线样式画到 SKCanvas（调用方已施加视口变换，坐标处于文档像素空间）。
        /// 蚂蚁线 = 黑色描边 + SKPathEffect.CreateDash 虚线；线宽除以 Scale，
        /// 使视觉线宽恒定 1 屏幕像素（不随缩放变粗）。
        /// </summary>
        public void RenderTo(SKCanvas canvas, float scale)
        {
            if (_polylines.Count == 0)
                return;

            using var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f / scale,
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, 220),
            };
            using SKPathEffect dash = SKPathEffect.CreateDash([4f, 4f], 0f);
            paint.PathEffect = dash;

            foreach ((Point2[] points, bool closed) in _polylines)
            {
                using var path = new SKPath();
                path.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Length; i++)
                    path.LineTo(points[i].X, points[i].Y);
                if (closed)
                    path.Close();
                canvas.DrawPath(path, paint);
            }
        }
    }

    /// <summary>
    /// 自定义绘制操作（Avalonia.Rendering.SceneGraph.ICustomDrawOperation）：
    /// 在渲染线程经 Skia 租约取 SKCanvas 直绘文档与覆盖层，零拷贝、自动 DPI。
    /// 注：Avalonia 12 若未来以 RenderOperation 替代 ICustomDrawOperation，
    /// 本类保持"自定义绘制"语义，换用等价 API 即可。
    /// </summary>
    private sealed class CanvasDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly OsirisDocument? _document;
        private readonly Vector _offset;
        private readonly double _scale;
        private readonly IEditorTool? _activeTool;
        private readonly CanvasOverlayProxy _overlay;

        public CanvasDrawOperation(
            Rect bounds,
            OsirisDocument? document,
            Vector offset,
            double scale,
            IEditorTool? activeTool,
            CanvasOverlayProxy overlay)
        {
            _bounds = bounds;
            _document = document;
            _offset = offset;
            _scale = scale;
            _activeTool = activeTool;
            _overlay = overlay;
        }

        /// <inheritdoc />
        public Rect Bounds => _bounds;

        /// <inheritdoc />
        /// 渲染：清底色 → 视口变换（Translate+Scale）→ DocumentRenderer 逐层合成 → 工具覆盖层。
        public void Render(ImmediateDrawingContext context)
        {
            // 取 Skia 直绘租约（架构第 7 节）；非 Skia 后端（如软件渲染未装 Skia）则跳过。
            ISkiaSharpApiLeaseFeature? feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (feature is null)
                return;
            using ISkiaSharpApiLease lease = feature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            // 画布底色：浅灰，用于区分文档（白色）之外的空白区域。
            canvas.Clear(new SKColor(240, 240, 240));

            if (_document is null)
                return;

            // 文档区：视口变换后调用 DocumentRenderer 合成（其内部白底 Clear 作用于整画布，
            // 文档矩形外仍为上方浅灰底色——文档比视口小时视觉上"浮"在画布中央）。
            canvas.Save();
            try
            {
                canvas.Translate((float)_offset.X, (float)_offset.Y);
                canvas.Scale((float)_scale, (float)_scale);
                DocumentRenderer.Render(canvas, _document);
            }
            finally
            {
                canvas.Restore();
            }

            // 工具覆盖层：先让工具刷新绘制内容，再把收集的折线画到画布（同样经视口变换）。
            if (_activeTool is not null)
            {
                _overlay.Clear();
                _activeTool.DrawOverlay(_overlay);
                if (_overlay.Count > 0)
                {
                    canvas.Save();
                    try
                    {
                        canvas.Translate((float)_offset.X, (float)_offset.Y);
                        canvas.Scale((float)_scale, (float)_scale);
                        _overlay.RenderTo(canvas, (float)_scale);
                    }
                    finally
                    {
                        canvas.Restore();
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool HitTest(Point point) => _bounds.Contains(point);

        /// <inheritdoc />
        /// 场景图据此判断是否可复用上一帧的绘制操作：文档/视口任一变化即视为不同。
        public bool Equals(ICustomDrawOperation? other)
            => other is CanvasDrawOperation op
                && ReferenceEquals(op._document, _document)
                && op._scale == _scale
                && op._offset == _offset;

        /// <inheritdoc />
        /// 无自有托管资源（SKPaint 等在 Render 内 using 释放），无需清理。
        public void Dispose() { }
    }
}
