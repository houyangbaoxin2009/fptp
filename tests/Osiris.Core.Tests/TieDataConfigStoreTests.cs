using DoNetTD;
using Osiris.Abstractions.Modules;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// TieDataConfigStore 测试：tie:data 扁平配置来回烤、头部声明、数字归一化 double、
/// 旧 JSON 兼容迁移（*.data.tie 缺失回退 *.json / 内容为 JSON 回退解析）、损坏容错、非标量忽略，
/// 以及 ModuleRegistry 经 TieDataConfigStore 的完整落盘/重载集成。
/// </summary>
public class TieDataConfigStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "osiris-tiedata-tests", Guid.NewGuid().ToString("N"));

    public TieDataConfigStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    /// <summary>临时文件路径（*.data.tie 后缀）。</summary>
    private string TempFile(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public void SaveLoad_RoundTrip_FlattenScalars()
    {
        // 意图：bool/double/int/string 四类标量 Save → Load 完整往返（数字统一读为 double，与 JsonConfigStore 一致）。
        string path = TempFile("settings.data.tie");
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["fptm.tolerance"] = 60.0,
            ["fptm.feather"] = 3,
            ["fpter.enabled"] = true,
            ["itool.color"] = "FF0000FF",
        };

        new TieDataConfigStore().Save(path, data);
        var loaded = new TieDataConfigStore().Load(path);

        Assert.Equal(60.0, loaded["fptm.tolerance"]);
        Assert.Equal(3.0, loaded["fptm.feather"]);           // int 写入 → 读回 double
        Assert.Equal(true, loaded["fpter.enabled"]);
        Assert.Equal("FF0000FF", loaded["itool.color"]);
        Assert.Equal(4, loaded.Count);
    }

    [Fact]
    public void Save_EmitsTieDataHeader_AndCanonicialShape()
    {
        // 意图：写入文件带 type tie<data> 头部声明，且文件内容可被 DoNetTD 直接解析（与 tiec 生态互通）。
        string path = TempFile("modules.data.tie");
        var data = new Dictionary<string, object> { ["fpter"] = "enabled", ["fptm"] = "disabled" };

        new TieDataConfigStore().Save(path, data);

        string text = File.ReadAllText(path);
        Assert.StartsWith("type tie<data>", text);
        var doc = TieDocument.ParseFile(path);              // DoNetTD 再解析（官方生态互通验证）
        Assert.IsType<TieTable>(doc.Root);
        Assert.Equal("enabled", ((TieTable)doc.Root)["fpter"]!.ToString()!.Trim('"'));
    }

    [Fact]
    public void MissingFile_ReturnsEmpty_NoLegacyJson()
    {
        // 意图：新路径与旧路径都不存在 → 空字典。
        var result = new TieDataConfigStore().Load(TempFile("nope.data.tie"));
        Assert.Empty(result);
    }

    [Fact]
    public void JsonLegacyFile_Migrated_WhenDataTieMissing()
    {
        // 意图：*.data.tie 不存在但同名旧 *.json 存在（旧版配置）→ 读取旧 JSON 数据（迁移，下次 Save 落盘 tie:data）。
        string legacy = TempFile("settings.json");
        File.WriteAllText(legacy, """{"fptm.tolerance":60,"fpter.enabled":true}""");

        var loaded = new TieDataConfigStore().Load(TempFile("settings.data.tie"));

        Assert.Equal(60.0, loaded["fptm.tolerance"]);
        Assert.Equal(true, loaded["fpter.enabled"]);
    }

    [Fact]
    public void DataTieFileContainingJson_FallsBackToJsonParse()
    {
        // 意图：*.data.tie 文件内容实为 JSON（异常场景）→ tie:data 解析失败自动回退 JSON 读取，数据不丢。
        string path = TempFile("settings.data.tie");
        File.WriteAllText(path, """{"fptm.tolerance":80}""");

        var loaded = new TieDataConfigStore().Load(path);

        Assert.Equal(80.0, loaded["fptm.tolerance"]);
    }

    [Fact]
    public void CorruptTieData_ReturnsEmpty()
    {
        // 意图：*.data.tie 内容非法（既非 tie:data 也非 JSON）→ 空字典（调用方安全重置）。
        string path = TempFile("settings.data.tie");
        File.WriteAllText(path, "{ 完全不是合法内容 ");

        Assert.Empty(new TieDataConfigStore().Load(path));
    }

    [Fact]
    public void NonScalarValues_IgnoredOnLoad()
    {
        // 意图：表/数组值不映射（IConfigStore 只承诺标量），其余标量正常读取。
        string path = TempFile("settings.data.tie");
        var data = new Dictionary<string, object> { ["a"] = true, ["b"] = "x" };
        new TieDataConfigStore().Save(path, data);

        // 手工追加一个表值（模拟未来写入嵌套）
        var doc = TieDocument.ParseFile(path);
        ((TieTable)doc.Root).SetItem("nested", new TieTable().SetItem("k", new TieString("v")));
        doc.WriteToFile(path);

        var loaded = new TieDataConfigStore().Load(path);

        Assert.Equal(true, loaded["a"]);
        Assert.Equal("x", loaded["b"]);
        Assert.False(loaded.ContainsKey("nested"));   // 表值被忽略
    }

    [Fact]
    public void ToLegacyJsonPath_替换后缀()
    {
        Assert.Equal("a/b/settings.json", TieDataConfigStore.ToLegacyJsonPath("a/b/settings.data.tie"));
        Assert.Equal("x.json", TieDataConfigStore.ToLegacyJsonPath("x.json"));   // 非 data.tie 路径原样
    }

    [Fact]
    public void ModuleRegistry_TieDataStore_落盘重载一致()
    {
        // 意图：ModuleRegistry 经 TieDataConfigStore 持久化的三文件（modules.data.tie/settings.data.tie/secure.data.tie）
        // 在重建注册表后状态与配置完整恢复（与 JsonConfigStore 路径等价，tie:data 履约无差别）。
        string modules = TempFile("modules.data.tie");
        string settings = TempFile("settings.data.tie");
        string secure = TempFile("secure.data.tie");
        var store = new TieDataConfigStore();

        var first = new ModuleRegistry(modules, settings, secure, store);
        first.Register(new ModuleRecord("fptm", "FPTM", "1.0.0", ModuleKind.Extension,
            ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, "Fptm.dll", _tempDir));
        first.SetConfig("fptm", "tolerance", 120.0);
        first.SetConfig("fptm", "enabled", true);

        var reloaded = new ModuleRegistry(modules, settings, secure, store);

        // 持久化状态/配置恢复：Initialize 期间 Register 时以持久化状态覆盖（IsEnabled 从 modules.data.tie），
        // 配置从 settings.data.tie 读回；记录本身按设计由模块加载器重建（Get 为 null 正常）。
        Assert.True(reloaded.IsEnabled("fptm"));
        Assert.Equal(120.0, reloaded.GetConfig<double>("fptm", "tolerance", 0));
        Assert.True(reloaded.GetConfig<bool>("fptm", "enabled", false));
    }
}