namespace FpEdit.Commands;

/// <summary>
/// 模块内像素剪贴板：跨文档复制/粘贴的像素缓冲（不碰系统剪贴板）。
/// 复制 = 选区包围盒裁剪；粘贴 = 以 (0,0) 源上合成到首层。
/// </summary>
public static class ClipboardService
{
    /// <summary>最近一次复制的内容（无内容时 null）。</summary>
    public static Osiris.Abstractions.Document.PixelSurface? Copied { get; set; }
}
