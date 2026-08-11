using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Osiris.CoreModule.Services;

/// <summary>
/// 文件对话框服务契约：模块（命令）经此接口打开/保存图片文件，
/// 与具体 UI 平台解耦（Avalonia 的 StorageProvider 实现；Linux 走 XDG portal）。
/// 注意：必须使用 StorageProvider（TopLevel.StorageProvider），禁用已废弃的 Avalonia.Controls.FileDialog。
/// </summary>
public interface IFileDialogService
{
    /// <summary>打开文件选择对话框，返回用户选中的本地路径（取消返回 null）。</summary>
    Task<string?> OpenFileAsync(
        Window owner,
        string title,
        IReadOnlyList<FilePickerFileType>? filters = null);

    /// <summary>另存为对话框，返回用户输入的保存路径（取消返回 null）。</summary>
    Task<string?> SaveFileAsync(
        Window owner,
        string title,
        string suggestedName,
        IReadOnlyList<FilePickerFileType>? filters = null);
}

/// <summary>
/// 基于 Avalonia StorageProvider 的文件对话框实现（跨平台，Linux 走 XDG portal）。
/// 内部用 TryGetLocalPath() 把 StorageFile 转回本地文件系统路径，供 SkiaCodec 直接读写。
/// </summary>
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    /// <summary>通用图片文件类型过滤（PNG/JPEG/BMP/WebP 等 Skia 支持格式）。</summary>
    public static readonly FilePickerFileType ImageFilter = new("图像文件")
    {
        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"],
    };

    /// <inheritdoc />
    public async Task<string?> OpenFileAsync(Window owner, string title, IReadOnlyList<FilePickerFileType>? filters = null)
    {
        ArgumentNullException.ThrowIfNull(owner);

        // TopLevel.GetTopLevel(owner) 等价 owner 本体（Window 是 TopLevel 子类），
        // 用 GetTopLevel 保持与控件场景一致；平台不支持打开选择器时返回 null。
        if (TopLevel.GetTopLevel(owner)?.StorageProvider is not { CanOpen: true } provider)
            return null;

        IReadOnlyList<IStorageFile> files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,                       // 本服务单文件语义
            FileTypeFilter = filters ?? [ImageFilter],
        });

        // 未选择任何文件视为取消
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> SaveFileAsync(Window owner, string title, string suggestedName, IReadOnlyList<FilePickerFileType>? filters = null)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (TopLevel.GetTopLevel(owner)?.StorageProvider is not { CanSave: true } provider)
            return null;

        IStorageFile? file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = filters ?? [ImageFilter],
        });

        return file?.TryGetLocalPath();
    }
}
