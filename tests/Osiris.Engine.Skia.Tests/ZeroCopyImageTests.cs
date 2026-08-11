using Osiris.Abstractions.Document;
using Osiris.Engine.Skia;
using SkiaSharp;
using Xunit;

namespace Osiris.Engine.Skia.Tests;

/// <summary>
/// ZeroCopyImage 零拷贝视图测试：尺寸/格式对齐、写入后再建视图的像素可见性。
/// </summary>
public class ZeroCopyImageTests
{
    [Fact]
    public void CreateView_ReportsSurfaceSizeAndBgraPremulFormat()
    {
        // 意图：零拷贝视图应报告与源面一致的尺寸，格式为 Bgra8888 预乘（与契约布局对齐）。
        PixelSurface surface = PixelSurface.Create(4, 3);
        using SKImage image = ZeroCopyImage.CreateView(surface);

        Assert.Equal(4, image.Width);
        Assert.Equal(3, image.Height);
        Assert.Equal(SKColorType.Bgra8888, image.ColorType);
        Assert.Equal(SKAlphaType.Premul, image.AlphaType);
    }

    [Fact]
    public void CreateView_AfterSurfaceWrite_ReadPixelsReturnsPixel()
    {
        // 意图：先写 surface 再建视图（SKImage 不可变），ReadPixels 应读到写入的像素。
        PixelSurface surface = PixelSurface.Create(2, 2);
        PixelSurfaceEditor editor = surface.CreateEditor();
        // 首像素写为不透明红（BGRA 预乘：B=0,G=0,R=255,A=255）
        editor.Row(0)[0] = 0;
        editor.Row(0)[1] = 0;
        editor.Row(0)[2] = 255;
        editor.Row(0)[3] = 255;
        PixelSurface committed = editor.Commit();

        using SKImage image = ZeroCopyImage.CreateView(committed);
        var pixels = new byte[2 * 2 * 4];

        // SkiaSharp 3.x 的 ReadPixels 只接受固定指针（IntPtr）目标，用 GCHandle 固定托管缓冲
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            Assert.True(image.ReadPixels(
                new SKImageInfo(2, 2, SKColorType.Bgra8888, SKAlphaType.Premul),
                handle.AddrOfPinnedObject(),
                2 * 4));
        }
        finally
        {
            handle.Free();
        }

        // 首像素 BGRA = [0,0,255,255]（不透明红）
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, pixels[..4].ToArray());
    }
}
