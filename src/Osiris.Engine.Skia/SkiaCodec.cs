using System.Runtime.InteropServices;
using Osiris.Abstractions.Document;
using SkiaSharp;

namespace Osiris.Engine.Skia;

/// <summary>
/// 基于 SkiaSharp 的图片编解码器：解码任意 Skia 支持格式（PNG/JPEG/BMP/WebP 等）
/// 为 BGRA 预乘 PixelSurface，编码 PixelSurface 为 PNG/JPEG 文件。
/// 仅依赖 Abstractions 像素交换类型，导入/导出与渲染共用同一数据面。
/// </summary>
public static class SkiaCodec
{
    /// <summary>
    /// 从文件解码图片为 BGRA 预乘 PixelSurface。
    /// 契约：任何失败（文件缺失/不可读/损坏/无权限）均返回 null，由调用方处理；
    /// 不向外抛文件系统异常（PixelSurface? 语义 = 失败即 null）。
    /// </summary>
    public static PixelSurface? Decode(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        byte[] bytes;
        try
        {
            // 读取文件可能因文件不存在/被占用/无权限/路径非法而失败，
            // 统一按"无法解码"处理返回 null，与调用方契约一致。
            bytes = File.ReadAllBytes(filePath);
        }
        catch (Exception ex) when (ex is FileNotFoundException
                                   or DirectoryNotFoundException
                                   or IOException
                                   or ArgumentException
                                   or UnauthorizedAccessException)
        {
            return null;
        }

        return Decode(bytes);
    }

    /// <summary>从字节数据解码图片为 BGRA 预乘 PixelSurface；无法解码返回 null。</summary>
    public static PixelSurface? Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // FromEncodedData 支持 PNG/JPEG/BMP/WebP 等全部 Skia 内置格式。
        using SKImage? image = SKImage.FromEncodedData(data);
        return image is null ? null : ReadToPixelSurface(image);
    }

    /// <summary>把 PixelSurface 编码为 PNG 写入文件（无损）。</summary>
    public static void EncodePng(PixelSurface surface, string filePath)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        // 零拷贝视图直接编码：编码期间 SKImage 持有底层数组引用（保持固定），
        // 数据不复制；SKData 用 using 释放。
        using SKImage image = ZeroCopyImage.CreateView(surface);
        using SKData? data = image.Encode(SKEncodedImageFormat.Png, 100);
        if (data is null)
            throw new InvalidOperationException("PNG 编码失败。");
        File.WriteAllBytes(filePath, data.ToArray());
    }

    /// <summary>把 PixelSurface 编码为 JPEG 写入文件（有损，quality 0~100）。</summary>
    public static void EncodeJpeg(PixelSurface surface, string filePath, int quality = 90)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        using SKImage image = ZeroCopyImage.CreateView(surface);
        using SKData? data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        if (data is null)
            throw new InvalidOperationException("JPEG 编码失败。");
        File.WriteAllBytes(filePath, data.ToArray());
    }

    /// <summary>
    /// 内部协作：SKImage → PixelSurface 回读（BGRA 预乘）。
    /// 经 SKPixmap 一次性读取，Skia 自动把源格式（含半透明 PNG 的 Unpremul）转换到
    /// 预乘目标——2.0 中不指定 Premul 时按预乘合成会得到错误的半透明颜色（历史坑，
    /// 见旧 ImageCodecSkia：Copy(SKColorType) 只换颜色类型、保留 Unpremul），此处规避。
    /// </summary>
    internal static PixelSurface ReadToPixelSurface(SKImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        // 一次性分配目标缓冲：宽*高*4（BGRA，行尾无填充）。
        int length = checked(image.Width * image.Height * 4);
        byte[] buffer = new byte[length];

        // 固定缓冲交给 Skia 直接写入，避免按行 Marshal.Copy 的多次往返。
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            if (!image.ReadPixels(info, handle.AddrOfPinnedObject(), image.Width * 4))
                throw new InvalidDataException($"像素读取失败（{image.Width}x{image.Height}）。");
        }
        finally
        {
            handle.Free();
        }

        // 经契约工厂创建 PixelSurface（内部复制一次；解码属低频操作，开销可接受）。
        return PixelSurface.Create(image.Width, image.Height, buffer);
    }
}
