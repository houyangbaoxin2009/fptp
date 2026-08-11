using System.Reflection;
using System.Runtime.InteropServices;
using Osiris.Abstractions.Document;
using SkiaSharp;

namespace Osiris.Engine.Skia;

/// <summary>
/// 零拷贝视图：把契约层 PixelSurface（BGRA 预乘 byte[]）包装为 SKImage，
/// 不复制像素数据，Skia 直接采样底层数组（App 画布逐帧合成与导出共用）。
/// </summary>
public static class ZeroCopyImage
{
    /// <summary>
    /// 为 PixelSurface 创建零拷贝 SKImage 视图。
    /// BGRA 预乘 = Skia 原生 Bgra8888 + Premul，字节布局完全一致，零拷贝成立。
    /// 返回的 SKImage 生命周期由调用方管理（using 释放）；释放时自动解固定。
    /// </summary>
    public static SKImage CreateView(PixelSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        // 1) 固定底层数组：契约层只暴露 ReadOnlySpan，底层 byte[] 本体经反射取出
        //    （契约已冻结、_data 字段名稳定；GCHandle 固定需要数组对象）。
        //    GCHandle.Alloc(pinned) 阻止 GC 压缩移动，保证指针在 SKData 生命周期内稳定。
        byte[] pixels = PixelSurfaceMemory.GetBackingArray(surface);
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);

        // 2) SKData 无拷贝包装：长度 = 行字节数 × 高（行尾无填充，RowBytes == Width*4）。
        //    释放回调里 FreeHandle——句柄与数据同生命周期，SKData 最后一次释放时解固定，
        //    避免 fixed 作用域逃逸导致的悬空指针。
        int length = checked(surface.RowBytes * surface.Height);
        using var data = SKData.Create(handle.AddrOfPinnedObject(), length, (_, _) => handle.Free());

        // 3) 零拷贝 SKImage：SkiaSharp 3.x 无 FromPixelData，等价 API 为 FromPixels；
        //    内部走 sk_image_new_raster_data，SKImage 持有 SKData 的原生引用——
        //    data 用 after 释放不影响图像，图像释放时才触发 SKData 释放回调并解固定。
        var info = new SKImageInfo(surface.Width, surface.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        return SKImage.FromPixels(info, data, surface.RowBytes);
    }
}

/// <summary>
/// 像素缓冲访问器：反射读取 PixelSurface 的私有 byte[] _data。
/// 契约层只暴露只读 Span，但 GCHandle 固定 / 回读写入需要数组对象本体；
/// FieldInfo 经 static 缓存，仅首次反射开销，运行期与普通字段访问同量级。
/// </summary>
internal static class PixelSurfaceMemory
{
    // 契约已冻结：PixelSurface 内部缓冲字段名 _data 稳定（1.0.0.0）。
    private static readonly FieldInfo DataField = typeof(PixelSurface)
        .GetField("_data", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("PixelSurface._data 字段不存在，契约版本不匹配。");

    /// <summary>取回 PixelSurface 底层像素数组（零拷贝，不复制）。</summary>
    public static byte[] GetBackingArray(PixelSurface surface) => (byte[])DataField.GetValue(surface)!;
}

