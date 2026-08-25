using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Itool.Commands;

/// <summary>
/// 裁切到选区命令：把当前文档首层按选区包围盒裁切为新尺寸画布（超出选区的像素丢弃），
/// 经 IDocumentService.ApplyLayerChange 走历史栈可撤销；裁切后旧选区失效，自动清空。
/// 无选区或选区为空时静默忽略。
/// </summary>
public sealed class CropToSelectionCommand(IHostContext host) : ICommand
{
    /// <inheritdoc />
    public string Id => "itool.cropToSelection";

    /// <inheritdoc />
    public string DisplayName => L10n.T("裁切到选区");

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        var docs = host.Services.Get<IDocumentService>();
        var doc = docs?.Document;
        if (docs is null || doc is null || doc.Layers.Count == 0 || doc.Selection is not { } sel)
            return;

        PixelSurface src = doc.Layers[0].Pixels;

        // 求选区包围盒（与 fpedit 复制命令同一套遍历）
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
        for (int y = 0; y < sel.Height; y++)
            for (int x = 0; x < sel.Width; x++)
                if (sel.Contains(x, y))
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
        if (maxX < minX)
            return; // 空选区

        int w = maxX - minX + 1, h = maxY - minY + 1;
        if (w == src.Width && h == src.Height)
            return; // 选区即整幅：无需裁切

        // 裁切包围盒为新像素面（像素原样拷贝）
        var editor = PixelSurface.Create(w, h).CreateEditor();
        for (int y = 0; y < h; y++)
            src.Row(minY + y).Slice(minX * 4, w * 4).CopyTo(editor.Row(y));

        Layer layer = doc.Layers[0];
        docs.ApplyLayerChange(layer.Id, layer, layer.WithPixels(editor.Commit()));
        docs.SetSelection(null);   // 画布尺寸已变，旧选区坐标失效：清空
    }
}