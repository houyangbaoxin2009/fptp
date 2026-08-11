using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;

namespace Fptm.Tools;

/// <summary>
/// 绘制工具基类：笔画生命周期管理（一笔一命令，可撤销）。
/// MouseDown 快照首层图层 + 打开编辑会话；MouseMove 沿 Bresenham 线调 StrokeLine；
/// MouseUp Commit 出新图层并经 IDocumentService.ApplyLayerChange 入历史栈（COW，零拷贝撤销）。
/// 状态归 ToolState（各工具独立颜色/大小）。
/// </summary>
public abstract class StrokeToolBase : IEditorTool
{
    private Layer? _oldLayer;
    private PixelSurfaceEditor? _editor;
    private Point2 _last;

    /// <summary>宿主上下文（由 FptmModule.Initialize 统一注入）。</summary>
    protected IHostContext? Host;

    /// <summary>文档服务（经服务注册表获取；CLI/无文档宿主下为 null）。</summary>
    protected IDocumentService? Docs => Host?.Services.Get<IDocumentService>();

    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    /// 工具作为插件实例暴露时的插件名（与显示名一致）。
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

    /// <inheritdoc />
    public event Action? VisualChanged;

    /// <summary>由宿主模块统一注入（工具不独立走插件加载器）。</summary>
    public void Initialize(IHostContext host) => Host = host;

    /// <summary>本工具颜色（子类经 ToolState 读取，独立可选色）。</summary>
    protected abstract uint Color { get; }

    /// <summary>笔刷尺寸（像素）。</summary>
    protected abstract double StampSize { get; }

    /// <summary>单点印记（覆盖一个像素形状）；沿线逐点绘制由基类 Bresenham 驱动。</summary>
    protected abstract void Stamp(PixelSurfaceEditor editor, int x, int y, double size, uint color);

    /// <summary>
    /// 沿线段绘制：默认 Bresenham 逐点 Stamp；刷子（多毛刷痕）override 此方法。
    /// </summary>
    protected virtual void StrokeLine(PixelSurfaceEditor editor, Point2 from, Point2 to, double size, uint color)
        => Bresenham(from, to, (x, y) => Stamp(editor, x, y, size, color));

    /// <inheritdoc />
    public void MouseDown(ToolMouseEvent e)
    {
        if (e.Button != ToolMouseButton.Left) return;
        var doc = Docs?.Document;
        if (doc is null || doc.Layers.Count == 0) return;
        _oldLayer = doc.Layers[0];
        _editor = _oldLayer.Pixels.CreateEditor();
        _last = new Point2(e.X, e.Y);
        Stamp(_editor, e.X, e.Y, StampSize, Color);
    }

    /// <inheritdoc />
    public void MouseMove(ToolMouseEvent e)
    {
        if (_editor is null || _oldLayer is null) return;
        var to = new Point2(e.X, e.Y);
        StrokeLine(_editor, _last, to, StampSize, Color);
        _last = to;
        // 实时反馈：把当前笔画状态提交为预览表面（不入历史），画布即时显示笔迹
        Docs?.SetPreviewSurface(_oldLayer.Id, _editor.Commit());
        VisualChanged?.Invoke();
    }

    /// <inheritdoc />
    public void MouseUp(ToolMouseEvent e)
    {
        if (_editor is null || _oldLayer is null) return;
        Layer newLayer = _oldLayer.WithPixels(_editor.Commit());
        Docs?.ApplyLayerChange(_oldLayer.Id, _oldLayer, newLayer); // 最终提交（入历史，可撤销）
        _editor = null;
        _oldLayer = null;
    }

    /// <inheritdoc />
    public virtual void Activate() { }

    /// <inheritdoc />
    public virtual void Deactivate() { }

    /// <inheritdoc />
    public void DrawOverlay(IToolOverlay overlay) { } // 绘制工具无覆盖层

    /// <summary>在编辑器上以 coverage(0~1) 把颜色"源上合成"到目标像素（预乘 source-over；越界忽略）。</summary>
    protected static void PaintPixel(PixelSurfaceEditor editor, int x, int y, double coverage, uint color)
    {
        if (coverage <= 0 || (uint)x >= (uint)editor.Width || (uint)y >= (uint)editor.Height) return;
        Span<byte> row = editor.Row(y);
        int i = x * 4;
        byte sa = (byte)(((color >> 24) & 0xFF) * coverage); // 源 alpha（含笔刷衰减）
        if (sa == 0) return;
        byte sr = (byte)(((color >> 16) & 0xFF) * sa / 255);
        byte sg = (byte)(((color >> 8) & 0xFF) * sa / 255);
        byte sb = (byte)((color & 0xFF) * sa / 255);
        int inv = 255 - sa;
        row[i] = (byte)(sb + row[i] * inv / 255);
        row[i + 1] = (byte)(sg + row[i + 1] * inv / 255);
        row[i + 2] = (byte)(sr + row[i + 2] * inv / 255);
        row[i + 3] = (byte)(sa + row[i + 3] * inv / 255);
    }

    /// <summary>整数 Bresenham 线段：逐点回调。</summary>
    protected static void Bresenham(Point2 from, Point2 to, Action<int, int> plot)
    {
        int x0 = from.X, y0 = from.Y, x1 = to.X, y1 = to.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            plot(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}

/// <summary>铅笔：1px 硬边。</summary>
public sealed class PencilTool : StrokeToolBase
{
    /// <inheritdoc />
    public override string Id => "pencil";

    /// <inheritdoc />
    public override string DisplayName => "铅笔";

    /// <inheritdoc />
    protected override uint Color => Editing.ToolState.Instance.GetColor(Id);

    /// <inheritdoc />
    protected override double StampSize => 1;

    /// <inheritdoc />
    protected override void Stamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
        => PaintPixel(editor, x, y, 1, color);
}

/// <summary>钢笔：硬边圆点（尺寸 1~10，默认 3，经 ToolState 独立设置）。</summary>
public sealed class PenTool : StrokeToolBase
{
    /// <inheritdoc />
    public override string Id => "pen";

    /// <inheritdoc />
    public override string DisplayName => "钢笔";

    /// <inheritdoc />
    protected override uint Color => Editing.ToolState.Instance.GetColor(Id);

    /// <inheritdoc />
    protected override double StampSize => Editing.ToolState.Instance.GetSize(Id);

    /// <inheritdoc />
    protected override void Stamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
    {
        double r = Math.Max(0.5, size / 2);
        for (int py = (int)Math.Floor(y - r); py <= (int)Math.Ceiling(y + r); py++)
            for (int px = (int)Math.Floor(x - r); px <= (int)Math.Ceiling(x + r); px++)
                if ((px - x) * (px - x) + (py - y) * (py - y) <= r * r)
                    PaintPixel(editor, px, py, 1, color);
    }
}

/// <summary>毛笔：软边径向衰减笔刷（固定 8px；coverage=(1-d/r)^2，浓淡自然过渡）。</summary>
public sealed class InkBrushTool : StrokeToolBase
{
    /// <inheritdoc />
    public override string Id => "inkBrush";

    /// <inheritdoc />
    public override string DisplayName => "毛笔";

    /// <inheritdoc />
    protected override uint Color => Editing.ToolState.Instance.GetColor(Id);

    /// <inheritdoc />
    protected override double StampSize => 8;

    /// <inheritdoc />
    protected override void Stamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
        => SoftStamp(editor, x, y, size, color);

    /// <summary>软边圆点：d≤r 时 coverage=(1-d/r)^2（径向平滑衰减）。供本类与刷子共用。</summary>
    internal static void SoftStamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
    {
        double r = Math.Max(0.5, size / 2);
        for (int py = (int)Math.Floor(y - r); py <= (int)Math.Ceiling(y + r); py++)
            for (int px = (int)Math.Floor(x - r); px <= (int)Math.Ceiling(x + r); px++)
            {
                double d = Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d <= r)
                {
                    double c = 1 - d / r;
                    PaintPixel(editor, px, py, c * c, color);
                }
            }
    }
}

/// <summary>
/// 刷子：多毛刷痕（现实刷子效果）。沿笔画方向绘制 N 根平行毛线，
/// 毛的中心偏移与宽度从中间向两边逐步减小——中间粗浓、两边渐细渐淡；
/// 每根毛用软边描线（复用毛笔 SoftStamp），毛数随尺寸自适应 5~13 根。
/// </summary>
public sealed class BrushTool : StrokeToolBase
{
    /// <inheritdoc />
    public override string Id => "brush";

    /// <inheritdoc />
    public override string DisplayName => "刷子";

    /// <inheritdoc />
    protected override uint Color => Editing.ToolState.Instance.GetColor(Id);

    /// <inheritdoc />
    protected override double StampSize => Editing.ToolState.Instance.GetSize(Id);

    /// <inheritdoc />
    /// 覆写沿线绘制：多根平行毛线（每根沿线软边描线），毛宽/偏移中间大两边小。
    protected override void StrokeLine(PixelSurfaceEditor editor, Point2 from, Point2 to, double size, uint color)
    {
        // 毛数随尺寸自适应（5~13 根）：小刷毛少、大刷毛多。
        int hairs = Math.Clamp((int)Math.Round(size / 3), 5, 13);
        double len = Math.Max(1e-6, Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y)));
        double nx = (to.Y - from.Y) / len; // 法线方向（垂直笔画）
        double ny = -(to.X - from.X) / len;
        double half = Math.Max(0.5, size / 2);

        for (int i = 0; i < hairs; i++)
        {
            double t = hairs == 1 ? 0 : i / (double)(hairs - 1) * 2 - 1; // -1..1，0=中间
            double offset = t * half;                                                                      // 沿法线偏移
            double hairR = Math.Max(0.5, size / 2 * (1 - Math.Abs(t)) * 0.85 + 0.3);                       // 毛宽：中间最大向两边递减
            var hFrom = new Point2((int)Math.Round(from.X + nx * offset), (int)Math.Round(from.Y + ny * offset));
            var hTo = new Point2((int)Math.Round(to.X + nx * offset), (int)Math.Round(to.Y + ny * offset));
            // 每根毛 = 沿线软边描线（毛半径 hairR）
            Bresenham(hFrom, hTo, (x, y) => InkBrushTool.SoftStamp(editor, x, y, hairR * 2, color));
        }
    }

    /// <inheritdoc />
    /// 单点（未移动时）也画软边点。
    protected override void Stamp(PixelSurfaceEditor editor, int x, int y, double size, uint color)
        => InkBrushTool.SoftStamp(editor, x, y, size, color);
}
