using System.Reflection;
using Osiris.Abstractions.Cli;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Core.Plugins;

namespace Osiris.Cli;

/// <summary>
/// CLI 命令收集器（架构 4.9"收集全部 ICliCommandProvider.Commands"）：
/// 从注册表枚举"已启用 + Extension + Native"的模块，在独立可回收 ALC 中加载其入口程序集，
/// 扫描 [PluginExport] 且实现 ICliCommandProvider 的类型，收集全部 Commands 供根命令动态挂载。
/// 背景：Core 的 ModuleLoader 遵循卸载纪律（不缓存 Type/Assembly、不对外暴露模块实例），
/// 且 ModuleLoadContext 为 internal——CLI 侧自行维护一个同构的"命令收集通道"，
/// 只读取 Commands 属性（纯数据描述，不重复调用 Initialize，加载副作用已由 ModuleLoader 完成）。
/// 过滤条件与 GUI 完全一致（Disabled/Removed 跳过、MinHostVersion 已由 ModuleLoader 校验）——CLI 无特权。
/// </summary>
internal sealed class CliCommandCatalog
{
    private readonly ModuleRegistry _registry;

    /// <summary>构造：以共享注册表为数据源（记录含持久化状态与入口程序集元数据）。</summary>
    public CliCommandCatalog(ModuleRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>收集全部已启用扩展模块贡献的 CLI 子命令；单个模块失败经 onError 上报不中断整体。</summary>
    public IReadOnlyList<CliCommandDescriptor> Collect(Action<string, Exception>? onError = null)
    {
        var commands = new List<CliCommandDescriptor>();
        foreach (ModuleRecord record in _registry.Modules)
        {
            // 与 GUI 相同的过滤：禁用/已卸载不加载；CLI 只收集 Extension（Standard 由 GUI 静态加载，CLI 不引用）
            if (record.Status != ModuleStatus.Enabled)
                continue;
            if (record.Kind != ModuleKind.Extension)
                continue;
            if (record.Type != ModuleType.Native || string.IsNullOrEmpty(record.EntryPoint) || record.LoadPath is null)
                continue;

            string dll = Path.Combine(record.LoadPath, record.EntryPoint);
            if (!File.Exists(dll))
                continue;

            try
            {
                commands.AddRange(CollectFromAssembly(dll, onError));
            }
            catch (Exception ex)
            {
                onError?.Invoke(dll, ex);
            }
        }
        return commands;
    }

    /// <summary>在独立 ALC 中扫描单个入口程序集，收集其中 ICliCommandProvider 贡献的命令。</summary>
    private static IReadOnlyList<CliCommandDescriptor> CollectFromAssembly(string dllPath, Action<string, Exception>? onError)
    {
        var found = new List<CliCommandDescriptor>();

        // 独立可回收 ALC：方法结束即放弃强引用（卸载纪律，架构 8.2）
        var alc = new CliModuleLoadContext(dllPath);
        Assembly assembly = alc.LoadFromAssemblyPath(dllPath);

        foreach (Type type in SafeGetTypes(assembly))
        {
            // 契约：须同时满足 [PluginExport] 标记 + 实现 ICliCommandProvider
            if (type.GetCustomAttribute<PluginExportAttribute>() is null)
                continue;
            if (!typeof(ICliCommandProvider).IsAssignableFrom(type))
                continue;

            try
            {
                // 只读 Commands（纯数据描述）；不调用 Initialize——避免与 ModuleLoader 已执行的初始化重复副作用
                var provider = (ICliCommandProvider)Activator.CreateInstance(type)!;
                found.AddRange(provider.Commands);
            }
            catch (Exception ex)
            {
                onError?.Invoke(type.FullName ?? type.Name, ex);
            }
        }
        return found;
    }

    /// <summary>容错取类型：程序集部分类型加载失败时仅返回成功加载的类型（同 ModuleLoader.SafeGetTypes）。</summary>
    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(static t => t is not null).Cast<Type>();
        }
    }
}
