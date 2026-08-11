using System.Reflection;
using System.Runtime.Loader;

namespace Osiris.Cli;

/// <summary>
/// CLI 命令收集专用的可回收 ALC（架构 8.2 卸载纪律）：
/// Core 内部的 ModuleLoadContext 为 internal，CLI 无法复用，此处镜像其共享程序集跳过逻辑——
/// 每个扩展模块入口程序集一个独立 ALC，加载后不缓存 Type/Assembly（随作用域结束可回收）。
/// 共享程序集（Osiris.Abstractions/Osiris.Core/SkiaSharp/System.*/Avalonia）回退默认 ALC，
/// 避免同一程序集两处加载导致类型分裂（ABI 红线，见架构 5 节）。
/// </summary>
internal sealed class CliModuleLoadContext : AssemblyLoadContext
{
    /// <summary>共享程序集名前缀：宿主（CLI/GUI）已加载，不放入模块 ALC。</summary>
    private static readonly string[] SharedAssemblyPrefixes =
        ["Osiris.Abstractions", "Osiris.Core", "SkiaSharp", "System.", "Avalonia"];

    // 模块私有依赖解析器：按入口 dll 所在目录解析其私有依赖
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>构造：以模块入口 dll 建立可回收 ALC 与依赖解析器。</summary>
    public CliModuleLoadContext(string pluginPath)
        : base($"osiris-cli:{Path.GetFileName(pluginPath)}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>
    /// 加载重写：共享程序集 → 返回 null 回退默认 ALC；默认 ALC 已加载同名 → 复用；
    /// 其余依赖经 AssemblyDependencyResolver 在模块目录内解析。
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);

        // 共享程序集（Abstractions/Core/System.* 等）：交由默认 ALC 解析
        if (IsShared(assemblyName.Name))
            return null;

        // 默认 ALC 已加载的同名程序集：直接复用
        foreach (Assembly loaded in AssemblyLoadContext.Default.Assemblies)
            if (string.Equals(loaded.GetName().Name, assemblyName.Name, StringComparison.Ordinal))
                return loaded;

        // 模块私有依赖：从模块目录解析
        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }

    /// <summary>判断程序集名是否命中共享前缀（默认 ALC 负责解析）。</summary>
    private static bool IsShared(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        foreach (string prefix in SharedAssemblyPrefixes)
            if (name.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        return false;
    }
}
