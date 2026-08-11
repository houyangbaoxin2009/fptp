using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;

namespace Fptm.Commands;

/// <summary>复制：把当前文档选区包围盒内的像素裁剪入模块剪贴板。</summary>
public sealed class CopyCommand : ICommand
{
    private readonly IHostContext _host;

    public CopyCommand(IHostContext host) => _host = host;

    /// <inheritdoc />
    public string Id => "fptm.copy";

    /// <inheritdoc />
    public string DisplayName => "复制";

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        var doc = _host.Services.Get<IDocumentService>()?.Document;
        if (doc is null || doc.Layers.Count == 0 || doc.Selection is not { } sel) return;
        PixelSurface src = doc.Layers[0].Pixels;

        // 求选区包围盒
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < sel.Height; y++)
            for (int x = 0; x < sel.Width; x++)
                if (sel.Contains(x, y))
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
        if (maxX < minX) return; // 空选区

        // 裁剪包围盒为新像素面（保持像素原样）
        int w = maxX - minX + 1, h = maxY - minY + 1;
        var editor = PixelSurface.Create(w, h).CreateEditor();
        for (int y = 0; y < h; y++)
            src.Row(minY + y).Slice(minX * 4, w * 4).CopyTo(editor.Row(y));
        ClipboardService.Copied = editor.Commit();
    }
}

/// <summary>粘贴：把剪贴板像素以 (0,0) 源上合成到当前文档首层（可撤销）。</summary>
public sealed class PasteCommand : ICommand
{
    private readonly IHostContext _host;

    public PasteCommand(IHostContext host) => _host = host;

    /// <inheritdoc />
    public string Id => "fptm.paste";

    /// <inheritdoc />
    public string DisplayName => "粘贴";

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        var docs = _host.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (docs is null || doc is null || doc.Layers.Count == 0 || ClipboardService.Copied is not { } clip) return;

        Layer layer = doc.Layers[0];
        var editor = layer.Pixels.CreateEditor();
        for (int y = 0; y < clip.Height && y < editor.Height; y++)
        {
            ReadOnlySpan<byte> srcRow = clip.Row(y);
            Span<byte> dstRow = editor.Row(y);
            for (int x = 0; x < clip.Width && x < editor.Width; x++)
            {
                int si = x * 4;
                byte a = srcRow[si + 3];
                if (a == 0) continue; // 透明像素不覆盖
                int di = x * 4;
                int inv = 255 - a;
                dstRow[di] = (byte)(srcRow[si] + dstRow[di] * inv / 255);
                dstRow[di + 1] = (byte)(srcRow[si + 1] + dstRow[di + 1] * inv / 255);
                dstRow[di + 2] = (byte)(srcRow[si + 2] + dstRow[di + 2] * inv / 255);
                dstRow[di + 3] = (byte)(a + dstRow[di + 3] * inv / 255);
            }
        }
        editor.MarkAllDirty();
        Layer newLayer = layer.WithPixels(editor.Commit());
        docs.ApplyLayerChange(layer.Id, layer, newLayer);
    }
}

/// <summary>撤销（向前一步的逆向）：操作窗口按钮复用。</summary>
public sealed class UndoCommand : ICommand
{
    private readonly IHostContext _host;

    public UndoCommand(IHostContext host) => _host = host;

    /// <inheritdoc />
    public string Id => "fptm.undo";

    /// <inheritdoc />
    public string DisplayName => "撤销";

    /// <inheritdoc />
    public void Execute(object? parameter) => _host.Services.Get<IDocumentService>()?.Undo();
}

/// <summary>重做（向前一步）：操作窗口按钮复用。</summary>
public sealed class RedoCommand : ICommand
{
    private readonly IHostContext _host;

    public RedoCommand(IHostContext host) => _host = host;

    /// <inheritdoc />
    public string Id => "fptm.redo";

    /// <inheritdoc />
    public string DisplayName => "重做";

    /// <inheritdoc />
    public void Execute(object? parameter) => _host.Services.Get<IDocumentService>()?.Redo();
}

/// <summary>
/// 颜料盘槽位命令（fptm.palette1..9）：把对应槽位颜色应用到当前画笔工具。
/// 壳快捷键路由（默认 Ctrl+A+1..9）执行本命令；操作窗口/画笔窗口也可经命令表触发。
/// </summary>
public sealed class PaletteSlotCommand : ICommand
{
    private readonly int _index; // 0-based 槽位索引

    public PaletteSlotCommand(int index) => _index = index;

    /// <inheritdoc />
    public string Id => $"fptm.palette{_index + 1}";

    /// <inheritdoc />
    public string DisplayName => $"颜料槽 {_index + 1}";

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (_index < 0 || _index >= Editing.ToolState.Instance.Slots.Length) return;
        string toolId = Editing.ToolState.Instance.IsStrokeTool(Editing.ToolState.Instance.CurrentToolId)
            ? Editing.ToolState.Instance.CurrentToolId
            : "brush";
        Editing.ToolState.Instance.SetColor(toolId, Editing.ToolState.Instance.GetSlot(_index));
    }
}
