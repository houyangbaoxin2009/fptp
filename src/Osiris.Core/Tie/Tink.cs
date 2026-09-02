using System.Buffers.Binary;
using System.Text;

namespace Osiris.Core.Tie;

/// <summary>
/// tink 节点帧协议（宿主侧实现，与 FpSDK.Tink / std/tink.tie 及 tink 跨语言库同构）。
/// <para>帧 = [len:u32 BE][payload:len 字节][crc:u32 BE]；crc = CRC32-IEEE(payload)（多项式 0xEDB88320）。</para>
/// <para>校验向量：<c>crc32("123456789") == 0xCBF43926</c>。</para>
/// 行帧桥（fptp.tie-bridge.v2）：stdin/stdout 文本流上每行一条 <c>base64(帧)</c>，
/// 输出帧 payload 首字节为 tag（<see cref="OkTag"/>=OK / <see cref="ErrTag"/>=ERR），
/// 其余字节为正文 UTF-8。纯函数，不碰 IO。
/// </summary>
public static class Tink
{
    /// <summary>帧固定开销：len(4) + crc(4)。</summary>
    public const int FrameOverhead = 8;

    /// <summary>输出帧 tag：成功。</summary>
    public const byte OkTag = 0x00;

    /// <summary>输出帧 tag：错误。</summary>
    public const byte ErrTag = 0x01;

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    /// <summary>CRC32-IEEE（查表，与 zlib.crc32 / std/tink.tie 的 rdu_crc 一致）。</summary>
    public static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint c = 0xFFFFFFFFu;
        foreach (byte b in data)
            c = CrcTable[(c ^ b) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    /// <summary>payload → 完整帧字节：len(u32 BE) + payload + crc(u32 BE)。</summary>
    public static byte[] Encode(ReadOnlySpan<byte> payload)
    {
        var frame = new byte[payload.Length + FrameOverhead];
        BinaryPrimitives.WriteUInt32BigEndian(frame, (uint)payload.Length);
        payload.CopyTo(frame.AsSpan(4));
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(4 + payload.Length), Crc32(payload));
        return frame;
    }

    /// <summary>
    /// 从 bytes[pos..] 解析一帧（校验 CRC）。
    /// 成功返回 payload 并置 <paramref name="nextPos"/>（下一帧起点）；不完整或 CRC 不符返回 null、nextPos=-1。
    /// </summary>
    public static byte[]? TryDecode(ReadOnlySpan<byte> bytes, int pos, out int nextPos)
    {
        nextPos = -1;
        if (pos < 0 || pos + FrameOverhead - 1 >= bytes.Length)
            return null;
        int n = BinaryPrimitives.ReadInt32BigEndian(bytes[pos..]);
        if (n < 0 || pos + FrameOverhead + (long)n > bytes.Length)
            return null;
        var payload = bytes.Slice(pos + 4, n).ToArray();
        uint got = BinaryPrimitives.ReadUInt32BigEndian(bytes[(pos + 4 + n)..]);
        if (Crc32(payload) != got)
            return null;
        nextPos = pos + FrameOverhead + n;
        return payload;
    }

    /// <summary>构造应答载荷：[tag][正文 UTF-8]。帧桥输出阶段由脚本/host 用该格式组 payload。</summary>
    public static byte[] TagPayload(byte tag, string utf8Text)
    {
        byte[] text = Encoding.UTF8.GetBytes(utf8Text);
        var payload = new byte[text.Length + 1];
        payload[0] = tag;
        text.CopyTo(payload, 1);
        return payload;
    }
}