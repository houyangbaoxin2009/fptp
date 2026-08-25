using Osiris.Core.Security;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// 模块信任名单测试：哈希白名单命中/未命中、用户 Trust 持久化、无内置名单降级、损坏名单容错。
/// </summary>
public class ModuleTrustStoreTests
{
    private static readonly string TempRoot =
        Path.Combine(Path.GetTempPath(), "osiris-trust-tests", Guid.NewGuid().ToString("N"));

    private static string WriteTempFile(string dir, string name, byte[] content)
    {
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    [Fact]
    public void IsTrusted_内置名单命中()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string builtin = Path.Combine(root, "trusted-modules.json");
            File.WriteAllText(builtin, """{"modules":{"mymod":["abc123"]}}""");
            var store = new ModuleTrustStore(builtin, Path.Combine(root, "user.json"));

            Assert.True(store.IsTrusted("mymod", "abc123"));
            Assert.False(store.IsTrusted("mymod", "deadbeef"));
            Assert.False(store.IsTrusted("other", "abc123"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Trust_写入用户名单即时生效且持久化()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string userPath = Path.Combine(root, "user.json");
            var store = new ModuleTrustStore(Path.Combine(root, "builtin.json"), userPath);
            Assert.False(store.IsTrusted("extmod", "h1"));

            store.Trust("extmod", "h1");
            Assert.True(store.IsTrusted("extmod", "h1")); // 内存即时生效

            // 新实例读取同一用户文件 → 持久化生效
            var reloaded = new ModuleTrustStore(Path.Combine(root, "builtin.json"), userPath);
            Assert.True(reloaded.IsTrusted("extmod", "h1"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void 无内置名单文件_HasBuiltinTrustFile为false()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            var store = new ModuleTrustStore(Path.Combine(root, "不存在的builtin.json"), Path.Combine(root, "user.json"));
            Assert.False(store.HasBuiltinTrustFile);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void 损坏的用户名单_回退空名单不崩溃()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string userPath = Path.Combine(root, "user.json");
            File.WriteAllText(userPath, "{ 不是合法JSON ");
            var store = new ModuleTrustStore(Path.Combine(root, "builtin.json"), userPath);

            Assert.False(store.IsTrusted("x", "y")); // 不崩溃，按空名单处理
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void HashUtil_Sha256File_文件哈希稳定且缺失返回null()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string file = WriteTempFile(root, "a.bin", [1, 2, 3, 4, 5]);
            string hash1 = HashUtil.Sha256File(file)!;
            string hash2 = HashUtil.Sha256File(file)!;
            Assert.Equal(hash1, hash2);          // 稳定
            Assert.Equal(64, hash1.Length);      // SHA-256 hex 长度
            Assert.Null(HashUtil.Sha256File(Path.Combine(root, "缺失.bin"))); // 缺失返回 null
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void IsTrusted_内置TieData名单命中()
    {
        // 意图：内置名单为 tie:data 格式（trusted-modules.data.tie，DoNetTD 写出风格）时哈希命中/未命中判定正确。
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string builtin = Path.Combine(root, "trusted-modules.data.tie");
            File.WriteAllText(builtin, """
                type tie<data>
                [
                    "modules": [
                        "mymod": ["abc123"],
                    ],
                ]
                """);
            var store = new ModuleTrustStore(builtin, Path.Combine(root, "user.json"));

            Assert.True(store.IsTrusted("mymod", "abc123"));
            Assert.False(store.IsTrusted("mymod", "deadbeef"));   // 哈希不匹配
            Assert.False(store.IsTrusted("other", "abc123"));     // 模块不在名单
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Trust_用户名单落盘为TieData_重载命中()
    {
        // 意图：用户 Trust 后名单文件内容为 tie:data（可被 DoNetTD 解析），新实例重载同文件仍命中。
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            string userPath = Path.Combine(root, "trusted-modules.data.tie");
            var store = new ModuleTrustStore(Path.Combine(root, "builtin.json"), userPath);

            store.Trust("extmod", "h1");
            Assert.True(store.IsTrusted("extmod", "h1"));
            Assert.Contains("type tie<data>", File.ReadAllText(userPath));   // 落盘 tie:data 带角色头

            var reloaded = new ModuleTrustStore(Path.Combine(root, "builtin.json"), userPath);
            Assert.True(reloaded.IsTrusted("extmod", "h1"));
        }
        finally { Directory.Delete(root, true); }
    }
}
