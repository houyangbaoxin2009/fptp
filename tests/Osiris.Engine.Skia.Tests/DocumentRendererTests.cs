using Osiris.Abstractions.Document;
using Osiris.Abstractions.Imaging;
using Osiris.Engine.Skia;
using SkiaSharp;
using Xunit;

namespace Osiris.Engine.Skia.Tests;

/// <summary>
/// DocumentRenderer 离屏合成测试：单层不透明红、双层 Multiply 混合已知值、RenderToImage 快照。
/// </summary>
public class DocumentRendererTests
{
    /// <summary>构造全图填色的像素面。</summary>
    private static PixelSurface FillSurface(int width, int height, byte b, byte g, byte r, byte a)
    {
        PixelSurfaceEditor editor = PixelSurface.Create(width, height).CreateEditor();
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i + 3 < pixels.Length; i += 4)
        {
            pixels[i] = b;
            pixels[i + 1] = g;
            pixels[i + 2] = r;
            pixels[i + 3] = a;
        }
        return editor.Commit();
    }

    [Fact]
    public void RenderToPixelSurface_SingleRedLayer_CenterPixelRed()
    {
        // 意图：单层不透明红 2x2 → 离屏渲染后中心像素(1,1)应为 BGRA [0,0,255,255]。
        var document = OsirisDocument.Create(2, 2);
        document.Layers.Add(new Layer(FillSurface(2, 2, b: 0, g: 0, r: 255, a: 255)));

        PixelSurface rendered = DocumentRenderer.RenderToPixelSurface(document);

        Assert.Equal(2, rendered.Width);
        Assert.Equal(2, rendered.Height);
        Assert.Equal(new byte[] { 0, 0, 255, 255 }, rendered.Row(1)[0..4].ToArray()); // 中心像素
    }

    [Fact]
    public void RenderToPixelSurface_TwoLayers_SemiTransparentGreenMultiplyOverWhite()
    {
        // 意图：白底 + 半透明绿(alpha=128) Multiply 叠白底。
        // Multiply 混合：co = Cs*Cd（白是乘性中性元 → 绿色保持），再按 alpha 与白底
        // src-over 混合 → 中心像素 ≈ R=127, G=255, B=127, A=255（容差 ±3 吸收 Skia 取整差异）。
        var document = OsirisDocument.Create(2, 2);
        document.Layers.Add(new Layer(FillSurface(2, 2, b: 255, g: 255, r: 255, a: 255)));          // 底层白
        document.Layers.Add(new Layer(FillSurface(2, 2, b: 0, g: 128, r: 0, a: 128))                // 顶层半透明绿（预乘）
        {
            BlendMode = BlendMode.Multiply,
        });

        PixelSurface rendered = DocumentRenderer.RenderToPixelSurface(document);

        byte[] center = rendered.Row(1)[0..4].ToArray();
        Assert.InRange(center[0], 124, 130); // B ≈ 127
        Assert.InRange(center[1], 250, 255); // G ≈ 255
        Assert.InRange(center[2], 124, 130); // R ≈ 127
        Assert.Equal(255, center[3]);        // 不透明
    }

    [Fact]
    public void RenderToImage_ReturnsNonNullSnapshot_WithDocumentSize()
    {
        // 意图：离屏快照非空且尺寸与文档一致。
        var document = OsirisDocument.Create(3, 2);
        document.Layers.Add(new Layer(FillSurface(3, 2, b: 0, g: 0, r: 255, a: 255)));

        using SKImage image = DocumentRenderer.RenderToImage(document);

        Assert.NotNull(image);
        Assert.Equal(3, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(SKColorType.Bgra8888, image.ColorType);
        Assert.Equal(SKAlphaType.Premul, image.AlphaType);
    }
}
