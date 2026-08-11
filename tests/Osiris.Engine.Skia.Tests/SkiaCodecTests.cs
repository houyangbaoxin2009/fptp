using Osiris.Abstractions.Document;
using Osiris.Engine.Skia;
using Xunit;

namespace Osiris.Engine.Skia.Tests;

/// <summary>
/// SkiaCodec 编解码测试：PNG 无损往返、半透明 alpha 保持、无法解码路径。
/// </summary>
public class SkiaCodecTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-skia-tests", Guid.NewGuid().ToString("N"));

    public SkiaCodecTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

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
    public void EncodePng_Decode_RoundTripsOpaquePixels()
    {
        // 意图：不透明纯色 PNG 编码→解码后像素逐字节一致（无损格式）。
        PixelSurface committed = FillSurface(4, 4, b: 0, g: 0, r: 255, a: 255);
        string path = Path.Combine(_tempDir, "red.png");

        SkiaCodec.EncodePng(committed, path);
        PixelSurface? decoded = SkiaCodec.Decode(path);

        Assert.NotNull(decoded);
        Assert.Equal(committed.Width, decoded!.Width);
        Assert.Equal(committed.Height, decoded.Height);
        Assert.Equal(committed.Pixels.ToArray(), decoded.Pixels.ToArray());
    }

    [Fact]
    public void EncodePng_Decode_SemiTransparentPixel_KeepsAlpha()
    {
        // 意图：半透明像素（预乘绿 alpha=128）PNG 往返后 alpha 保持半透明（>0 且 <255），
        // 且预乘通道无变色（G 仍 ≈128）——验证 Unpremul→Premul 转换不偏色。
        PixelSurface committed = FillSurface(2, 2, b: 0, g: 128, r: 0, a: 128);
        string path = Path.Combine(_tempDir, "half.png");

        SkiaCodec.EncodePng(committed, path);
        PixelSurface? decoded = SkiaCodec.Decode(path);

        Assert.NotNull(decoded);
        byte[] pixel = decoded!.Row(0)[0..4].ToArray();
        Assert.True(pixel[3] > 0 && pixel[3] < 255, $"alpha 应保持半透明，实际 {pixel[3]}");
        Assert.InRange(pixel[3], 125, 131); // alpha ≈ 128
        Assert.InRange(pixel[1], 125, 131); // G ≈ 128（预乘后无变色）
        Assert.InRange(pixel[0], 0, 4);     // B ≈ 0
        Assert.InRange(pixel[2], 0, 4);     // R ≈ 0
    }

    [Fact]
    public void Decode_UndecodableBytes_ReturnsNull()
    {
        // 意图：无法识别的数据应返回 null（SKImage.FromEncodedData 失败路径）。
        PixelSurface? result = SkiaCodec.Decode(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        Assert.Null(result);
    }

    [Fact]
    public void Decode_MissingFile_ReturnsNull()
    {
        // 意图：文件不存在属"无法解码"，应按契约返回 null（失败路径统一走 null 语义）。
        PixelSurface? result = SkiaCodec.Decode(Path.Combine(_tempDir, "missing.png"));
        Assert.Null(result);
    }
}
