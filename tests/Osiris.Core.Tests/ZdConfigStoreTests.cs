using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// ZdConfigStore 测试：tie:zd 压缩配置存储（数据传输/临时文件/备份场景）——
/// 与 Json/TieData 同数据面（扁平键值），往返、互转（zd ↔ tie:data 同数据）、损坏容错、非标量忽略。
/// </summary>
public class ZdConfigStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "osiris-zd-tests", Guid.NewGuid().ToString("N"));

    public ZdConfigStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    private string TempFile(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void SaveLoad_RoundTrip_Scalars()
    {
        // 意图：四类标量经 zd 编码 → 解码完整往返（数字统一读为 double），与 Json/TieData 存储语义一致。
        string path = TempFile("tmp.zd");
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["fast.enabled"] = true,
            ["fast.tolerance"] = 60.0,
            ["fast.count"] = 4,
            ["fast.name"] = "交换数据",
        };

        new ZdConfigStore().Save(path, data);
        var loaded = new ZdConfigStore().Load(path);

        Assert.Equal(true, loaded["fast.enabled"]);
        Assert.Equal(60.0, loaded["fast.tolerance"]);
        Assert.Equal(4.0, loaded["fast.count"]);           // int → 读回 double
        Assert.Equal("交换数据", loaded["fast.name"]);
        Assert.Equal(4, loaded.Count);
    }

    [Fact]
    public void MissingFile_ReturnsEmpty()
    {
        // 意图：文件不存在 → 空字典。
        Assert.Empty(new ZdConfigStore().Load(TempFile("nope.zd")));
    }

    [Fact]
    public void CorruptZd_ReturnsEmpty_NoThrow()
    {
        // 意图：损坏 zd 字节 → 空字典（调用方安全重置），不抛异常。
        string path = TempFile("bad.zd");
        File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0xFF, 0xFE]);   // 非法 zd 流

        Assert.Empty(new ZdConfigStore().Load(path));
    }

    [Fact]
    public void SameData_TieDataAndZd_Interchangeable()
    {
        // 意图：同一数据面在 tie:data 文本与 zd 二进制间无损互通（TieDataConfigStore ↔ ZdConfigStore 等价履约）。
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["fptm.tolerance"] = 80.0,
            ["fpter.enabled"] = true,
            ["itool.color"] = "ABC",
        };

        string tieDataPath = TempFile("settings.data.tie");
        string zdPath = TempFile("settings.zd");
        new TieDataConfigStore().Save(tieDataPath, data);
        new ZdConfigStore().Save(zdPath, data);

        Assert.Equal(new TieDataConfigStore().Load(tieDataPath), new ZdConfigStore().Load(zdPath));
    }

    [Fact]
    public void NonScalar_IgnoredOnLoad()
    {
        // 意图：zd map 中嵌入数组值 → 读取时忽略（合约只承诺标量），其余标量正常。
        string path = TempFile("tmp.zd");
        var entries = new Dictionary<string, DoNetZD.ZdValue>
        {
            ["ok"] = new DoNetZD.ZdValue.Bool(true),
            ["list"] = new DoNetZD.ZdValue.Array([new DoNetZD.ZdValue.String("x")]),
        };
        File.WriteAllBytes(path, DoNetZD.ZdCodec.Encode(new DoNetZD.ZdValue.Map(entries)));

        var loaded = new ZdConfigStore().Load(path);

        Assert.Equal(true, loaded["ok"]);
        Assert.False(loaded.ContainsKey("list"));   // 数组被忽略
    }
}