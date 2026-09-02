using System.Text;
using FpSDK;
using Xunit;

namespace FpSdk.Tests;

/// <summary>
/// tink 帧协议单测（.NET 侧，与 std/tink.tie / tink 跨语言库同构）：
/// CRC 标准向量、帧编解码往返（含空 payload）、CRC 篡改拒绝、越界解析失败、应答 tag 载荷。
/// </summary>
public class TinkTests
{
    /// <summary>CRC 标准向量：crc32("123456789") == 0xCBF43926。</summary>
    [Fact]
    public void Crc32_StandardVector()
    {
        Assert.Equal(0xCBF43926u, Tink.Crc32("123456789"u8));
    }

    [Fact]
    public void Frame_EncodeDecode_Roundtrip()
    {
        byte[] payload = { 1, 2, 3, 0x00, 0xFF, 128 };
        byte[] frame = Tink.Encode(payload);

        Assert.Equal(payload.Length + Tink.FrameOverhead, frame.Length);

        byte[]? decoded = Tink.TryDecode(frame, 0, out int nextPos);
        Assert.NotNull(decoded);
        Assert.Equal(payload, decoded);
        Assert.Equal(frame.Length, nextPos);
    }

    [Fact]
    public void Frame_EmptyPayload_Roundtrip()
    {
        byte[] frame = Tink.Encode([]);
        byte[]? decoded = Tink.TryDecode(frame, 0, out int nextPos);
        Assert.NotNull(decoded);
        Assert.Empty(decoded!);
        Assert.Equal(frame.Length, nextPos);
    }

    [Fact]
    public void Frame_CrcTamper_Rejected()
    {
        byte[] frame = Tink.Encode("payload"u8);
        frame[^1] ^= 0x01;   // 翻转 crc 末字节
        Assert.Null(Tink.TryDecode(frame, 0, out int nextPos));
        Assert.Equal(-1, nextPos);
    }

    [Fact]
    public void Frame_OutOfBounds_Rejected()
    {
        byte[] frame = Tink.Encode("abcdef"u8);
        // 从尾端解析：长度字段越界
        Assert.Null(Tink.TryDecode(frame, frame.Length, out _));
        Assert.Null(Tink.TryDecode(frame, frame.Length - 3, out _));
    }

    [Fact]
    public void Frame_Truncated_Rejected()
    {
        byte[] frame = Tink.Encode("abcdef"u8);
        byte[] cut = frame[..^2];   // 缺 crc 尾部
        Assert.Null(Tink.TryDecode(cut, 0, out _));
    }

    [Fact]
    public void TagPayload_Roundtrip()
    {
        byte[] payload = Tink.TagPayload(Tink.OkTag, "你好 ✓");
        Assert.Equal(Tink.OkTag, payload[0]);
        Assert.Equal("你好 ✓", Encoding.UTF8.GetString(payload, 1, payload.Length - 1));
    }
}