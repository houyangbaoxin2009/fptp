using System.Reflection;
using System.Runtime.Loader;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 模块加载器真实路径测试：经 ModuleLoader + ModuleRegistry 加载 plugins/bin 的
/// Fpter，验证注册表记录、滤镜表、以及 ALC 卸载（WeakReference 断言）。
/// </summary>
[Collection("Plugins")]
public class ModuleLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-plugin-tests", Guid.NewGuid().ToString("N"));

    public ModuleLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    /// <summary>创建独立 ModuleRegistry（三文件临时路径，JSON 存储）。</summary>
    private ModuleRegistry CreateRegistry() => new(
        Path.Combine(_tempDir, "modules.json"),
        Path.Combine(_tempDir, "settings.json"),
        Path.Combine(_tempDir, "secure.json"),
        new JsonConfigStore());

    /// <summary>执行加载并收集错误：返回 (成功数, 错误列表)。</summary>
    private static (int Loaded, List<string> Errors) LoadPlugins(ModuleRegistry registry, TestHostContext context)
    {
        var errors = new List<string>();
        int loaded = ModuleLoader.LoadFromDirectory(
            PluginsBinLocator.Path, registry, context,
            (ctx, ex) => errors.Add($"{ctx}: {ex.Message}"));
        return (loaded, errors);
    }

    /// <summary>在 AssemblyLoadContext.All 中查找加载了 Fpter 的模块 ALC。</summary>
    private static Assembly? FindPluginAssembly()
        => FindModuleAssembly("Fpter");

    /// <summary>在模块 ALC 中按程序集名查找已加载的程序集。</summary>
    private static Assembly? FindModuleAssembly(string assemblyName)
    {
        foreach (AssemblyLoadContext alc in AssemblyLoadContext.All)
        {
            if (alc.Name is null || !alc.Name.StartsWith("osiris-module:", StringComparison.Ordinal))
                continue;
            foreach (Assembly assembly in alc.Assemblies)
                if (assembly.GetName().Name == assemblyName)
                    return assembly;
        }
        return null;
    }

    [Fact]
    public void LoadFromDirectory_LoadsItoolModule_NineEditorTools()
    {
        // 意图：itool 模块（子目录 plugins/bin/Itool/module.json）经 FindManifests
        // 一级子目录扫描被加载；ITool.Tools 暴露 9 个编辑工具。
        ModuleRegistry registry = CreateRegistry();
        var context = new TestHostContext();
        (int loaded, List<string> errors) = LoadPlugins(registry, context);

        Assert.True(loaded >= 2, $"加载数应 ≥2（fpter + itool），实际 {loaded}");
        Assert.Empty(errors);

        ModuleRecord? record = registry.Get("itool");
        Assert.NotNull(record);
        Assert.Equal(ModuleKind.Extension, record!.Kind);

        Assembly? assembly = FindModuleAssembly("Itool");
        Assert.NotNull(assembly);
        Type? pluginType = assembly!.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<PluginExportAttribute>() is not null);
        Assert.NotNull(pluginType);

        var plugin = Activator.CreateInstance(pluginType!);
        var tools = (IReadOnlyList<Osiris.Abstractions.Ui.IEditorTool>?)pluginType!.GetProperty("Tools")?.GetValue(plugin);
        Assert.NotNull(tools);
        Assert.Equal(9, tools!.Count);
        // 覆盖用户要求的全部工具
        string[] ids = tools.Select(t => t.Id).OrderBy(s => s).ToArray();
        Assert.Contains("selectRect", ids);   // 选取
        Assert.Contains("lasso", ids);        // 套索
        Assert.Contains("magicWand", ids);    // 智能框选
        Assert.Contains("eyedropper", ids);   // 滴管
        Assert.Contains("pencil", ids);       // 铅笔
        Assert.Contains("pen", ids);          // 钢笔
        Assert.Contains("inkBrush", ids);     // 毛笔
        Assert.Contains("brush", ids);        // 刷子
        Assert.Contains("bucket", ids);       // 颜料桶
    }

    [Fact]
    public void LoadFromDirectory_RegistersFpterModule_Record()
    {
        // 意图：真实 ALC 路径加载插件——注册表记录 fpter 且 Kind=Extension。
        ModuleRegistry registry = CreateRegistry();
        var context = new TestHostContext();
        (int loaded, List<string> errors) = LoadPlugins(registry, context);

        Assert.True(loaded >= 1, $"加载数应 ≥1，实际 {loaded}");
        Assert.Empty(errors);

        ModuleRecord? record = registry.Get("fpter");
        Assert.NotNull(record);
        Assert.Equal("fpter", record!.Id);
        Assert.Equal(ModuleKind.Extension, record.Kind);
    }

    [Fact]
    public void LoadedPlugin_ExposesTwoFptpFilters()
    {
        // 意图：插件滤镜表非空，且包含 2 个 fpter.* 滤镜（灰度/动漫）。
        ModuleRegistry registry = CreateRegistry();
        var context = new TestHostContext();
        (int loaded, List<string> errors) = LoadPlugins(registry, context);
        Assert.True(loaded >= 1, $"加载数应 ≥1，实际 {loaded}");
        Assert.Empty(errors);

        Assembly? assembly = FindPluginAssembly();
        Assert.NotNull(assembly);
        Type? pluginType = assembly!.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<PluginExportAttribute>() is not null);
        Assert.NotNull(pluginType);

        var plugin = Activator.CreateInstance(pluginType!);
        var filters = (IReadOnlyList<IFilterProcessor>?)pluginType!.GetProperty("Filters")?.GetValue(plugin);

        Assert.NotNull(filters);
        Assert.True(filters!.Count >= 2, $"滤镜数应 ≥2，实际 {filters.Count}");
        Assert.Contains(filters, f => f.Id == "fpter.grayscale");
        Assert.Contains(filters, f => f.Id == "fpter.anime");
    }

    [Fact]
    public void PluginAssembly_IsCollectible_AfterRegistryReleased()
    {
        // 卸载断言（ALC 可回收承诺的唯一证明）：
        // ModuleLoadContext 为 isCollectible 可回收 ALC，ModuleLoader 本身不缓存
        // Type/Assembly；但 ModuleRegistry.RegisterSettingProvider 会把插件实例
        // (ISettingProvider) 存入强引用表，因此必须连同 registry 一起释放引用后，
        // GC 才能回收模块 ALC——本测试验证"registry 释放后"模块类型可回收。
        ModuleRegistry? registry = CreateRegistry();
        TestHostContext? context = new();
        (int loaded, List<string> errors) = LoadPlugins(registry, context);
        Assert.True(loaded >= 1, $"加载数应 ≥1，实际 {loaded}");
        Assert.Empty(errors);

        // 类型引用只在独立方法内取得（方法返回即释放局部引用，避免 Debug 局部变量存活干扰回收）
        WeakReference weakRef = AcquirePluginTypeWeakRef();

        // 释放全部强引用：注册表（持有插件实例）、上下文。
        registry = null;
        context = null;

        // 多轮强制 GC（含 blocking 完整回收）确保可回收 ALC 被回收
        //（本集合已串行化，无其它测试并行持有插件引用）。
        for (int i = 0; i < 3; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }

        Assert.False(weakRef.IsAlive);
    }

    /// <summary>
    /// 从模块 ALC 获取插件类型的弱引用；Assembly/Type 强引用仅存活于本方法内，
    /// 返回后即释放——保证 GC 时无测试侧强根残留。
    /// </summary>
    private static WeakReference AcquirePluginTypeWeakRef()
    {
        Assembly? assembly = FindPluginAssembly();
        Assert.NotNull(assembly);
        Type? pluginType = assembly!.GetTypes()
            .FirstOrDefault(t => t.GetCustomAttribute<PluginExportAttribute>() is not null);
        Assert.NotNull(pluginType);
        return new WeakReference(pluginType);
    }
}
