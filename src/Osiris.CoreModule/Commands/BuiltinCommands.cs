using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Ui;
using Osiris.Core.Document;
using Osiris.CoreModule.Controls;
using Osiris.CoreModule.Services;
using Osiris.Engine.Skia;

namespace Osiris.CoreModule.Commands;

/// <summary>
/// 核心模块命令 Id 常量：GUI 菜单（AddMenu）与快捷键绑定引用；命令 Id 全局唯一。
/// </summary>
public static class KnownCommands
{
    /// <summary>文件/打开：选择图片解码为文档。</summary>
    public const string Open = "osiris.core.open";

    /// <summary>文件/保存：以 PNG 覆盖保存到原路径（未保存过则走另存为）。</summary>
    public const string Save = "osiris.core.save";

    /// <summary>文件/导出：弹另存为，以 JPEG 导出。</summary>
    public const string Export = "osiris.core.export";

    /// <summary>编辑/撤销：回退最近一次文档命令。</summary>
    public const string Undo = "osiris.core.undo";

    /// <summary>编辑/重做：重做最近一次撤销。</summary>
    public const string Redo = "osiris.core.redo";

    /// <summary>视图/缩放适应：文档适配画布尺寸。</summary>
    public const string ZoomFit = "osiris.core.zoomFit";

    /// <summary>视图/实际大小：1:1 显示。</summary>
    public const string ZoomActual = "osiris.core.zoomActual";

    /// <summary>CLI 批处理子命令 Id（仅 ICliCommandProvider 声明，无 GUI 菜单）。</summary>
    public const string Batch = "osiris.core.batch";
}

/// <summary>
/// 命令共享上下文：核心命令依赖的服务与当前文档路径状态。
/// 由 CoreModule 装配后注入各命令（ICommand.Execute 无参，依赖只能经构造注入）。
/// </summary>
internal sealed class CommandContext
{
    /// <summary>画布控件（Document 读写、ZoomFit/ZoomActual、Revision 刷新）。</summary>
    public required CanvasControl Canvas { get; init; }

    /// <summary>文档服务（Open/Undo/Redo 走 DocumentService）。</summary>
    public required DocumentService Documents { get; init; }

    /// <summary>文件对话框服务（打开/另存为）。</summary>
    public required IFileDialogService FileDialog { get; init; }

    /// <summary>当前文档来源路径（打开时记录；保存用原名）。null = 尚未保存过。</summary>
    public string? CurrentPath { get; set; }

    /// <summary>从画布控件解析所属窗口（headless/无窗口环境返回 null，命令静默跳过）。</summary>
    public Window? OwnerWindow => TopLevel.GetTopLevel(Canvas) as Window;
}

/// <summary>
/// 打开命令：文件对话框选图片 → SkiaCodec 解码 → DocumentService.OpenDocument 建文档 →
/// 设置画布 Document 并缩放适配。异步执行（ICommand 契约为 void，内部 async void + 异常防护）。
/// </summary>
internal sealed class OpenCommand : ICommand
{
    private readonly CommandContext _ctx;

    public OpenCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.Open;
    public string DisplayName => "打开…";

    public async void Execute(object? parameter)
    {
        try
        {
            Window? owner = _ctx.OwnerWindow;
            if (owner is null)
                return;

            // 1) 选择图片文件
            string? path = await _ctx.FileDialog.OpenFileAsync(owner, "打开图片");
            if (path is null)
                return;

            // 2) 解码为像素面（失败提示并返回）
            PixelSurface? surface = SkiaCodec.Decode(path);
            if (surface is null)
                return;

            // 3) 打开文档（背景层）并绑定画布 + 适配视图
            _ctx.Documents.OpenDocument(surface);
            _ctx.Canvas.Document = _ctx.Documents.Document;
            _ctx.Canvas.ZoomFit();
            _ctx.CurrentPath = path;
        }
        catch (Exception ex)
        {
            // 打开失败不应让应用崩溃（async void 无调用方捕获，就地防护）。
            Console.Error.WriteLine($"打开失败：{ex.Message}");
        }
    }
}

/// <summary>
/// 保存命令：把当前文档（首层像素）以 PNG 覆盖保存到原路径；
/// 从未保存过时退化为另存为对话框。
/// </summary>
internal sealed class SaveCommand : ICommand
{
    private readonly CommandContext _ctx;

    public SaveCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.Save;
    public string DisplayName => "保存";

    public async void Execute(object? parameter)
    {
        try
        {
            // 无文档/无图层时无可保存内容
            OsirisDocument? document = _ctx.Canvas.Document;
            if (document is null || document.Layers.Count == 0)
                return;

            // 首层像素面（骨架阶段：文档单一背景层，保存首层即整图）
            PixelSurface surface = document.Layers[0].Pixels;

            string? path = _ctx.CurrentPath;
            if (path is null)
            {
                // 从未保存过 → 弹另存为（PNG）
                Window? owner = _ctx.OwnerWindow;
                if (owner is null)
                    return;
                path = await _ctx.FileDialog.SaveFileAsync(owner, "保存图片", "未命名.png");
                if (path is null)
                    return;
            }

            SkiaCodec.EncodePng(surface, path);
            _ctx.CurrentPath = path; // 记录保存路径，下次保存用原名
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"保存失败：{ex.Message}");
        }
    }
}

/// <summary>
/// 导出命令：弹另存为，把当前文档（首层像素）以 JPEG 编码导出（有损，适合分享）。
/// </summary>
internal sealed class ExportCommand : ICommand
{
    private readonly CommandContext _ctx;

    public ExportCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.Export;
    public string DisplayName => "导出…";

    public async void Execute(object? parameter)
    {
        try
        {
            OsirisDocument? document = _ctx.Canvas.Document;
            if (document is null || document.Layers.Count == 0)
                return;

            Window? owner = _ctx.OwnerWindow;
            if (owner is null)
                return;

            string? path = await _ctx.FileDialog.SaveFileAsync(
                owner, "导出为 JPEG", "导出.jpg", [new FilePickerFileType("JPEG 图像") { Patterns = ["*.jpg"] }]);
            if (path is null)
                return;

            SkiaCodec.EncodeJpeg(document.Layers[0].Pixels, path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"导出失败：{ex.Message}");
        }
    }
}

/// <summary>撤销命令：DocumentService.Undo → 画布 Revision++ 触发重绘。</summary>
internal sealed class UndoCommand : ICommand
{
    private readonly CommandContext _ctx;

    public UndoCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.Undo;
    public string DisplayName => "撤销";

    public void Execute(object? parameter)
    {
        _ctx.Documents.Undo();
        _ctx.Canvas.Revision++;
    }
}

/// <summary>重做命令：DocumentService.Redo → 画布 Revision++ 触发重绘。</summary>
internal sealed class RedoCommand : ICommand
{
    private readonly CommandContext _ctx;

    public RedoCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.Redo;
    public string DisplayName => "重做";

    public void Execute(object? parameter)
    {
        _ctx.Documents.Redo();
        _ctx.Canvas.Revision++;
    }
}

/// <summary>缩放适应命令：画布按文档尺寸适配窗口。</summary>
internal sealed class ZoomFitCommand : ICommand
{
    private readonly CommandContext _ctx;

    public ZoomFitCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.ZoomFit;
    public string DisplayName => "缩放适应";

    public void Execute(object? parameter) => _ctx.Canvas.ZoomFit();
}

/// <summary>实际大小命令：画布 1:1 显示（Scale=1.0 居中）。</summary>
internal sealed class ZoomActualCommand : ICommand
{
    private readonly CommandContext _ctx;

    public ZoomActualCommand(CommandContext ctx) => _ctx = ctx;

    public string Id => KnownCommands.ZoomActual;
    public string DisplayName => "实际大小";

    public void Execute(object? parameter) => _ctx.Canvas.ZoomActual();
}
