using System.Security.Cryptography;

namespace Osiris.Core.Security;

/// <summary>
/// 哈希工具：模块文件 SHA-256 计算（防篡改校验的数据源）。
/// 用法：发布时计算模块 DLL 哈希写入信任名单；加载时重算比对，不一致视为被篡改。
/// </summary>
public static class HashUtil
{
    /// <summary>
    /// 计算文件 SHA-256（十六进制小写）。文件不存在/读取失败返回 null（调用方按不可信处理）。
    /// </summary>
    public static string? Sha256File(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // 文件不可读 → 无法校验 → 视为不可信
        }
    }
}
