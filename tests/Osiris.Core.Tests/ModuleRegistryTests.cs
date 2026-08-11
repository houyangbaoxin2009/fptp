using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Settings;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// ModuleRegistry（设置注册表）测试：JSON 即时持久化、描述符默认值回退、
/// 损坏文件恢复为空、Standard 禁用拦截、Security 键写权限拦截。
/// </summary>
public class ModuleRegistryTests : IDisposable
{
    // 每测试独立临时目录（三个配置文件隔离，互不污染）
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-registry-tests", Guid.NewGuid().ToString("N"));

    private string ModulesPath => Path.Combine(_tempDir, "modules.json");
    private string ConfigPath => Path.Combine(_tempDir, "settings.json");
    private string SecurePath => Path.Combine(_tempDir, "secure.json");

    public ModuleRegistryTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    /// <summary>构造一个模块记录（默认 Extension，可按需指定分级）。</summary>
    private static ModuleRecord NewRecord(string id, ModuleKind kind = ModuleKind.Extension) =>
        new(id, $"模块{id}", "1.0.0", kind, ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet, $"{id}.dll", null);

    [Fact]
    public void Register_SetConfig_NewInstanceLoads_Restores()
    {
        // 意图：SetConfig 即时落盘 JSON——用同一路径构造新实例 Load 后能恢复之前写入的值。
        var first = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        first.Register(NewRecord("testmod"));
        first.SetConfig("testmod", "threshold", 42.0);

        var second = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        double? restored = second.GetConfig<double>("testmod", "threshold");
        Assert.Equal(42.0, restored);
    }

    [Fact]
    public void GetConfig_NoValue_FallsBackToSettingProviderDefault()
    {
        // 意图：未写配置时 GetConfig 回退 ISettingProvider 描述符当前值（即默认值 7.0）。
        var registry = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        registry.Register(NewRecord("testmod"));
        registry.RegisterSettingProvider(new FakeSettingProvider("testmod", defaultValue: 7.0));

        Assert.Equal(7.0, registry.GetConfig<double>("testmod", "threshold"));
    }

    [Fact]
    public void CorruptConfigFile_LoadsEmpty_AndFallsBack()
    {
        // 意图：settings.json 损坏（非法 JSON）时按空配置加载不抛异常，GetConfig 回退描述符默认值。
        File.WriteAllText(ConfigPath, "{ 这不是合法 JSON !!");
        var registry = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        registry.Register(NewRecord("testmod"));
        registry.RegisterSettingProvider(new FakeSettingProvider("testmod", defaultValue: 7.0));

        Assert.Equal(7.0, registry.GetConfig<double>("testmod", "threshold"));
    }

    [Fact]
    public void SetEnabled_StandardModule_ThrowsInvalidOperationException()
    {
        // 意图：权限规则——内置(Standard)模块不可被用户禁用/卸载。
        var registry = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        registry.Register(NewRecord("stdmod", ModuleKind.Standard));

        Assert.Throws<InvalidOperationException>(() => registry.SetEnabled("stdmod", false));
        Assert.Throws<InvalidOperationException>(() => registry.MarkRemoved("stdmod"));
    }

    [Fact]
    public void SetConfig_SecurityKey_ThrowsInvalidOperationException()
    {
        // 意图：声明 Security 级别的设置键对普通写入一律拒绝（仅更新模块可写）；可读不可写。
        var registry = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        registry.Register(NewRecord("testmod"));
        registry.RegisterSettingProvider(new FakeSettingProvider("testmod", defaultValue: 7.0, securityKey: "apiKey"));

        Assert.Throws<InvalidOperationException>(() => registry.SetConfig("testmod", "apiKey", "secret"));
        // 安全键可读：回退描述符当前值
        Assert.Equal("secret-default", registry.GetConfig<string>("testmod", "apiKey", "fallback"));
    }

    [Fact]
    public void SetEnabled_ExtensionModule_WorksAndPersists()
    {
        // 意图：扩展模块可禁用/重新启用，状态即时持久化（新实例可恢复）。
        var registry = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        registry.Register(NewRecord("extmod"));
        registry.SetEnabled("extmod", false);
        Assert.False(registry.IsEnabled("extmod"));

        var reloaded = new ModuleRegistry(ModulesPath, ConfigPath, SecurePath, new JsonConfigStore());
        Assert.False(reloaded.IsEnabled("extmod"));
    }

    /// <summary>测试用设置提供者：贡献一个 Number 设置项（阈值），可按需追加 Security 级文本键。</summary>
    private sealed class FakeSettingProvider(string moduleId, double defaultValue, string? securityKey = null)
        : ISettingProvider
    {
        public string Id => moduleId;
        public string Name => "测试设置提供者";
        public string Version => "1.0.0";
        public string MinHostVersion => "1.0.0";

        public void Initialize(IHostContext host)
        {
            // 测试不依赖初始化逻辑
        }

        public IReadOnlyList<SettingGroup> Groups
        {
            get
            {
                var items = new List<SettingItem>
                {
                    new NumberSettingItem(defaultValue, 0, 100, 1)
                    {
                        GroupId = moduleId,
                        Key = "threshold",
                        Label = "阈值",
                        Scope = SettingScope.User,
                    },
                };
                if (securityKey is not null)
                {
                    items.Add(new TextSettingItem("secret-default")
                    {
                        GroupId = moduleId,
                        Key = securityKey,
                        Label = "安全键",
                        Scope = SettingScope.Security,
                    });
                }
                return [new SettingGroup { Id = moduleId, DisplayName = "测试组", Items = items }];
            }
        }
    }
}


