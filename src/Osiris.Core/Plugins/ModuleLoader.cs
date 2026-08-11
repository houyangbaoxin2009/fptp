using System.Reflection;
using System.Text.Json;
using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;

namespace Osiris.Core.Plugins;

/// <summary>
/// 扩展模块加载器（架构 8.1 节）：扫描目录下的 module.json 清单（无清单时 *.dll 反射兜底），
/// 为每个 Native 模块建独立可回收 ALC（ModuleLoadContext），反射扫描 [PluginExport] 且实现
/// IModule 的类型，实例化并 Initialize。已禁用/已卸载模块跳过加载（重启生效）。
/// 卸载纪律：不缓存 Type/Assembly；2.1 无 UnloadModule（禁用 = 下次启动不加载）。
/// </summary>
public static class ModuleLoader
{
    /// <summary>宿主版本（MinHostVersion 校验基准，与 Directory.Build.props 的 Version 对齐）。</summary>
    public const string HostVersion = "1.0.0";

    /// <summary>模块清单文件名（语言中立数据源，见架构 4.7 节）。</summary>
    private const string ManifestFileName = "module.json";

    /// <summary>
    /// 加载指定目录下的全部模块（清单驱动；无清单则反射兜底）。
    /// 返回成功加载并初始化的模块数；单个模块失败经 onError 上报不中断整体。
    /// </summary>
    /// <param name="directory">模块根目录（含 module.json 或模块子目录）。</param>
    /// <param name="registry">模块注册表（登记记录、恢复持久化状态）。</param>
    /// <param name="context">宿主上下文（传给模块 Initialize）。</param>
    /// <param name="onError">逐模块错误回调（(上下文信息, 异常)）。</param>
    public static int LoadFromDirectory(string directory, ModuleRegistry registry, IHostContext context, Action<string, Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(context);
        if (!Directory.Exists(directory))
            return 0;

        // 1) 清单驱动：目录内 + 一级子目录的 module.json
        List<(string ModuleDir, string ManifestPath)> manifests = FindManifests(directory);
        if (manifests.Count == 0)
            return LoadByReflectionFallback(directory, registry, context, onError);

        int loaded = 0;
        foreach ((string moduleDir, string manifestPath) in manifests)
        {
            try
            {
                if (LoadManifest(moduleDir, manifestPath, registry, context, onError))
                    loaded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onError?.Invoke(manifestPath, ex);
            }
        }
        return loaded;
    }

    // ---- 清单驱动加载 ----

    /// <summary>解析清单并加载单个模块；返回是否成功加载（含初始化）。</summary>
    private static bool LoadManifest(string moduleDir, string manifestPath, ModuleRegistry registry, IHostContext context, Action<string, Exception>? onError)
    {
        ModuleManifest manifest = ParseManifest(manifestPath);

        // 构造注册表记录：状态以持久化 modules.json 为准（Register 内部以持久化状态覆盖 Status）
        var record = new ModuleRecord(
            manifest.Id, manifest.Name, manifest.Version, manifest.Kind,
            ModuleStatus.Enabled, manifest.Type, manifest.Language, manifest.EntryPoint, moduleDir);
        registry.Register(record);

        // 已禁用/已卸载 → 跳过加载（记录已登记供管理面板展示；禁用 = 下次启动不加载）
        if (!registry.IsEnabled(manifest.Id))
            return false;

        // MinHostVersion 校验：不满足记错误并跳过
        if (!IsHostVersionSatisfied(manifest.MinHostVersion, out string? versionError))
        {
            onError?.Invoke(manifest.Id, new InvalidOperationException(versionError));
            return false;
        }

        // tie 脚本模块：2.1 由未来 TieHost 标准模块解释执行，此处跳过
        if (manifest.Type == ModuleType.Script)
        {
            onError?.Invoke(manifest.Id, new NotSupportedException($"tie 脚本模块 {manifest.Id} 需 TieHost 支持，2.1 跳过加载。"));
            return false;
        }

        string dllPath = Path.Combine(moduleDir, manifest.EntryPoint ?? "");
        if (!File.Exists(dllPath))
        {
            onError?.Invoke(manifest.Id, new FileNotFoundException($"入口程序集不存在: {dllPath}"));
            return false;
        }

        return LoadNative(dllPath, registry, context, onError) > 0;
    }

    /// <summary>
    /// Native 模块加载：独立可回收 ALC → 反射扫描 [PluginExport]+IModule → 实例化 → Initialize。
    /// 返回该程序集成功初始化的导出类型数。
    /// </summary>
    private static int LoadNative(string dllPath, ModuleRegistry registry, IHostContext context, Action<string, Exception>? onError)
    {
        // 卸载纪律：ALC 仅在本方法作用域内持有，方法结束即放弃强引用（不缓存 Assembly/Type）
        var alc = new ModuleLoadContext(dllPath);
        Assembly assembly;
        try
        {
            assembly = alc.LoadFromAssemblyPath(dllPath);
        }
        catch (Exception ex)
        {
            onError?.Invoke(dllPath, ex);
            return 0;
        }

        int loaded = 0;
        foreach (Type type in SafeGetTypes(assembly))
        {
            // 契约：须同时满足 [PluginExport] 标记 + 实现 IModule
            if (type.GetCustomAttribute<PluginExportAttribute>() is null)
                continue;
            if (!typeof(IModule).IsAssignableFrom(type))
                continue;

            try
            {
                var module = (IModule)Activator.CreateInstance(type)!;
                module.Initialize(context);                    // 注册命令/滤镜/设置组等服务
                registry.RegisterInstance(module);             // 登记实例（宿主收集 IToolPlugin/IFilterPlugin 能力）
                if (module is ISettingProvider provider)
                    registry.RegisterSettingProvider(provider); // GetConfig 回退默认值 / 级别校验数据源
                loaded++;
            }
            catch (Exception ex)
            {
                onError?.Invoke(type.FullName ?? type.Name, ex);
            }
        }
        return loaded;
    }

    // ---- 无清单兜底：反射扫描 ----

    /// <summary>无 module.json 时扫描目录内 *.dll，反射识别 [PluginExport]+IModule 类型。</summary>
    private static int LoadByReflectionFallback(string directory, ModuleRegistry registry, IHostContext context, Action<string, Exception>? onError)
    {
        int loaded = 0;
        foreach (string dll in Directory.EnumerateFiles(directory, "*.dll"))
        {
            // 共享程序集（Abstractions/Core/System 等）不是模块，跳过
            if (ModuleLoadContext.IsSharedAssemblyName(Path.GetFileNameWithoutExtension(dll)))
                continue;

            try
            {
                var alc = new ModuleLoadContext(dll);
                Assembly assembly = alc.LoadFromAssemblyPath(dll);
                foreach (Type type in SafeGetTypes(assembly))
                {
                    if (type.GetCustomAttribute<PluginExportAttribute>() is null || !typeof(IModule).IsAssignableFrom(type))
                        continue;

                    var module = (IModule)Activator.CreateInstance(type)!;
                    if (!IsHostVersionSatisfied(module.MinHostVersion, out string? versionError))
                    {
                        onError?.Invoke(module.Id, new InvalidOperationException(versionError));
                        continue;
                    }

                    // 兜底记录：元数据取自 IModule 实例自身
                    var record = new ModuleRecord(
                        module.Id, module.Name, module.Version, module.Kind,
                        ModuleStatus.Enabled, ModuleType.Native, ScriptLanguage.DotNet,
                        Path.GetFileName(dll), directory);
                    registry.Register(record);

                    // 持久化禁用/卸载 → 不初始化（重启生效）
                    if (!registry.IsEnabled(module.Id))
                        continue;

                    module.Initialize(context);
                    registry.RegisterInstance(module);         // 登记实例（宿主收集 IToolPlugin/IFilterPlugin 能力）
                    if (module is ISettingProvider provider)
                        registry.RegisterSettingProvider(provider);
                    loaded++;
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(dll, ex);
            }
        }
        return loaded;
    }

    // ---- 辅助 ----

    /// <summary>收集清单文件：目录根 + 一级子目录。</summary>
    private static List<(string ModuleDir, string ManifestPath)> FindManifests(string directory)
    {
        var list = new List<(string, string)>();
        string root = Path.Combine(directory, ManifestFileName);
        if (File.Exists(root))
            list.Add((directory, root));
        foreach (string sub in Directory.EnumerateDirectories(directory))
        {
            string path = Path.Combine(sub, ManifestFileName);
            if (File.Exists(path))
                list.Add((sub, path));
        }
        return list;
    }

    /// <summary>解析 module.json 清单为强类型数据（字段缺省时取保守默认）。</summary>
    private static ModuleManifest ParseManifest(string manifestPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement root = doc.RootElement;

        string GetString(string property, string fallback = "")
            => root.TryGetProperty(property, out JsonElement element) ? element.GetString() ?? fallback : fallback;

        string id = GetString("id");
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidDataException($"模块清单缺少 id 字段: {manifestPath}");

        string entry = GetString("entryPoint");
        string minHost = GetString("minHostVersion");
        return new ModuleManifest(
            id,
            GetString("name", id),
            GetString("version", "0.0.0.0"),
            ParseKind(GetString("kind")),
            ParseType(GetString("type")),
            ParseLanguage(GetString("language")),
            string.IsNullOrWhiteSpace(entry) ? null : entry,
            string.IsNullOrWhiteSpace(minHost) ? null : minHost);
    }

    /// <summary>kind 解析：standard → Standard；未知按 Extension（保守）。</summary>
    private static ModuleKind ParseKind(string kind) => kind switch
    {
        "standard" => ModuleKind.Standard,
        _ => ModuleKind.Extension,
    };

    /// <summary>type 解析：script → Script；其余按 Native。</summary>
    private static ModuleType ParseType(string type) => type switch
    {
        "script" => ModuleType.Script,
        _ => ModuleType.Native,
    };

    /// <summary>language 解析：tie → Tie；其余按 DotNet。</summary>
    private static ScriptLanguage ParseLanguage(string language) => language switch
    {
        "tie" => ScriptLanguage.Tie,
        _ => ScriptLanguage.DotNet,
    };

    /// <summary>MinHostVersion 校验：未声明即不限制；声明且满足 ≤ 宿主版本才放行。</summary>
    private static bool IsHostVersionSatisfied(string? minHostVersion, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(minHostVersion))
            return true;
        if (Version.TryParse(minHostVersion, out Version? min) && Version.TryParse(HostVersion, out Version? host) && min <= host)
            return true;
        error = $"模块要求宿主版本 ≥ {minHostVersion}，当前 {HostVersion}，已跳过加载。";
        return false;
    }

    /// <summary>容错取类型：程序集部分类型加载失败时仅返回成功加载的类型。</summary>
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

    /// <summary>module.json 清单强类型（内部私有数据行）。</summary>
    private sealed record ModuleManifest(
        string Id, string Name, string Version, ModuleKind Kind, ModuleType Type,
        ScriptLanguage Language, string? EntryPoint, string? MinHostVersion);
}


