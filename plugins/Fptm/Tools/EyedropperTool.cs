using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Fptm.Editing;

namespace Fptm.Tools;

/// <summary>滴管：点击画布取色，设置到当前绘制工具（当前工具非绘制类时设置到刷子），供画笔窗口显示。</summary>
public sealed class EyedropperTool : IEditorTool
{
    private IHostContext? _host;

    /// <inheritdoc />
    public string Id => "eyedropper";

    /// <inheritdoc />
    public string DisplayName => "滴管";

    /// <inheritdoc />
    public string Name => DisplayName;

    /// <inheritdoc />
    public string Version => "1.0.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0.0";

    /// <inheritdoc />
    public void Initialize(IHostContext host) => _host = host;

    /// <inheritdoc />
    public void MouseDown(ToolMouseEvent e)
    {
        if (e.Button != ToolMouseButton.Left) return;
        var doc = _host?.Services.Get<IDocumentService>()?.Document;
        if (doc is null || doc.Layers.Count == 0) return;
        if ((uint)e.X >= (uint)doc.Width || (uint)e.Y >= (uint)doc.Height) return;

        ReadOnlySpan<byte> row = doc.Layers[0].Pixels.Row(e.Y);
        int i = e.X * 4;
        byte b = row[i], g = row[i + 1], r = row[i + 2], a = row[i + 3];
        uint color = a == 0
            ? 0u
            : (uint)(b | (g << 8) | ((r * 255 / a) << 16) | (a << 24)); // 非预乘 RGB + 原 alpha

        // 取色目标：当前工具为绘制类则设给该工具，否则默认刷子。
        string target = ToolState.Instance.IsStrokeTool(ToolState.Instance.CurrentToolId)
            ? ToolState.Instance.CurrentToolId
            : "brush";
        ToolState.Instance.SetColor(target, color);
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



