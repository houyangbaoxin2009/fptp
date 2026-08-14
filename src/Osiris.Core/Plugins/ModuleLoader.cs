using System.Reflection;
using System.Text.Json;
using Osiris.Abstractions;
using Osiris.Abstractions.Localization;
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

        // 签名校验（安全骨架）：宿主经服务注册表注入 IModuleSignatureValidator；
        // 拒绝返回 false 的模块不加载（防恶意/不可信来源模块执行任意代码）。
        var validator = context.Services.Get<IModuleSignatureValidator>();
        if (validator is not null && !validator.IsTrusted(manifest.Id, moduleDir, manifest.Signature))
        {
            onError?.Invoke(manifest.Id, new UnauthorizedAccessException(
                $"模块 {manifest.Id} 未通过签名校验（来源不可信），已拒绝加载。"));
            return false;
        }

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

        bool loadedOk = LoadNative(dllPath, registry, context, onError) > 0;
        if (loadedOk)
            RegisterModuleLanguagePack(moduleDir); // 模块加载成功：注册其自带语言包（模块目录/langs）
        return loadedOk;
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
                registry.RegisterInstance(module);             // 登记实例（宿主收集 ITool/IFilterPlugin 能力）
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
                    registry.RegisterInstance(module);         // 登记实例（宿主收集 ITool/IFilterPlugin 能力）
                    if (module is ISettingProvider provider)
                        registry.RegisterSettingProvider(provider);
                    RegisterModuleLanguagePack(directory);     // 模块加载成功：注册其自带语言包（模块目录/langs）
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

    /// <summary>
    /// 读取模块目录 module.json 的 entryPoint（主 DLL 文件名）；无清单/缺字段返回 null。
    /// 供签名校验器定位模块主 DLL（与 LoadManifest 同源解析）。
    /// </summary>
    public static string? ReadEntryPoint(string moduleDir)
    {
        string manifestPath = Path.Combine(moduleDir, ManifestFileName);
        if (!File.Exists(manifestPath))
            return null;
        try
        {
            return ParseManifest(manifestPath).EntryPoint;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// 枚举目录下模块清单（id/name，不含加载）：供外部模块确认框展示来源。
    /// 无清单时尝试反射识别 DLL 的 IModule 元数据（与 LoadFromDirectory 同源发现逻辑）。
    /// </summary>
    public static IReadOnlyList<(string Id, string Name)> EnumerateManifests(string directory)
    {
        var result = new List<(string, string)>();
        if (!Directory.Exists(directory))
            return result;

        foreach ((string moduleDir, string manifestPath) in FindManifests(directory))
        {
            try
            {
                ModuleManifest manifest = ParseManifest(manifestPath);
                result.Add((manifest.Id, manifest.Name));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.Add((Path.GetFileName(moduleDir), $"<清单解析失败: {ex.Message}>"));
            }
        }

        // 无清单：反射识别 DLL 的 IModule 元数据（不实例化，仅读 Id/Name）
        if (result.Count == 0)
        {
            foreach (string dll in Directory.EnumerateFiles(directory, "*.dll"))
            {
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
                        // 不经 Activator 实例化（无副作用），反射读取属性值
                        var module = (IModule)Activator.CreateInstance(type)!;
                        result.Add((module.Id, module.Name));
                        break; // 每个 DLL 只取第一个模块
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result.Add((Path.GetFileNameWithoutExtension(dll), $"<反射失败: {ex.Message}>"));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// 注册模块自带语言包（模块目录下 langs/）：条目合并进全局语言表，
    /// 随模块分发——卸载/移除模块后其翻译条目一并消失。经 L10n 静态门面转发，
    /// 未注入语言服务（CLI 早期/测试）时静默忽略，模块文本保持原文。
    /// </summary>
    private static void RegisterModuleLanguagePack(string moduleDir)
        => L10n.RegisterLanguagePack(Path.Combine(moduleDir, "langs"));

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
            string.IsNullOrWhiteSpace(minHost) ? null : minHost,
            GetString("signature")); // 可选签名声明（安全骨架：IModuleSignatureValidator 校验数据源）
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
        ScriptLanguage Language, string? EntryPoint, string? MinHostVersion, string? Signature);
}


