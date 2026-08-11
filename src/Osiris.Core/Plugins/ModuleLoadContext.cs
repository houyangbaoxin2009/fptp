using System.Reflection;
using System.Runtime.Loader;

namespace Osiris.Core.Plugins;

/// <summary>
/// 扩展模块专用 AssemblyLoadContext：每个 Native 模块一个独立 ALC（isCollectible: true，可回收）。
/// 卸载纪律（架构 8.2 节）：
/// - 加载后不缓存 Type/Assembly 引用（加载器只在本上下文中临时持有实例，随作用域结束释放）；
/// - 2.1 禁用/卸载 = 下次启动不加载（重启生效），暂不实现 UnloadModule；
/// - 未来 Unload 流程：摘除事件订阅 → 释放实例引用 → alc.Unload() → GC.Collect() → WeakReference 断言可回收。
/// </summary>
internal sealed class ModuleLoadContext : AssemblyLoadContext
{
    // 插件目录依赖解析器：按 dll 所在目录解析其私有依赖
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>构造：以插件入口 dll 建立可回收 ALC 与依赖解析器。</summary>
    public ModuleLoadContext(string pluginPath)
        : base($"osiris-module:{Path.GetFileName(pluginPath)}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>
    /// 共享程序集名前缀跳过列表：宿主已加载（Abstractions/Core/Skia/运行时/Avalonia），
    /// 插件目录不再重复解析，避免同一程序集两处加载导致类型分裂（ABI 红线）。
    /// </summary>
    public static readonly string[] SharedAssemblyPrefixes =
        ["Osiris.Abstractions", "Osiris.Algorithms", "Osiris.Core", "SkiaSharp", "System.", "Avalonia"];

    /// <summary>判断程序集名是否属于共享前缀（宿主 ALC 负责解析）。</summary>
    public static bool IsSharedAssemblyName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        foreach (string prefix in SharedAssemblyPrefixes)
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        return false;
    }

    /// <summary>
    /// 加载重写：共享程序集 → 返回 null 回退默认 ALC（宿主已加载同一副本）；
    /// 默认 ALC 已加载同名程序集 → 复用（避免重复加载）；
    /// 其余依赖经 AssemblyDependencyResolver 在插件目录内解析。
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);

        // 共享程序集（Osiris.Abstractions / Osiris.Core / System.* 等）：交由默认 ALC 解析
        if (IsSharedAssemblyName(assemblyName.Name))
            return null;

        // 宿主默认 ALC 已加载的同名程序集：直接复用
        foreach (Assembly loaded in AssemblyLoadContext.Default.Assemblies)
            if (string.Equals(loaded.GetName().Name, assemblyName.Name, StringComparison.Ordinal))
                return loaded;

        // 插件私有依赖：从插件目录解析
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }
}
