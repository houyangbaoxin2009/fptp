using Osiris.Abstractions.Modules;

namespace Osiris.Core.Plugins;

/// <summary>
/// 模块更新交互服务实现（IModuleUpdater，双面接口）：
/// - 通用面（所有模块可调）：GetUpdateStatus / RequestUpdate / UpdateStatusChanged / GetSecurityConfig（可读不可写）；
/// - Update 专用面（校验调用方角色）：ReplaceStandardModule / SetSecurityConfig（写 secure.json）。
/// 2.1 实现本地状态与元数据骨架：下载/签名校验/文件原子替换由后续版本补充。
/// </summary>
public sealed class ModuleUpdater : IModuleUpdater
{
    private readonly ModuleRegistry _registry;

    // 模块目录列表（替换内置模块时的目标目录；2.1 骨架暂未用于文件操作）
    private readonly IReadOnlyList<string> _moduleDirectories;

    // 调用方身份回调：宿主在模块代码执行期间提供"当前正在执行的模块 Kind"
    private readonly Func<ModuleKind?> _currentCallerKind;

    /// <summary>构造：注入模块注册表（secure.json 读写经其内部实现）。</summary>
    /// <param name="moduleDirectories">内置模块所在目录列表（ReplaceStandardModule 文件替换目标，2.1 预留）。</param>
    /// <param name="currentCallerKind">当前调用方模块 Kind 回调（宿主设置）；缺省时返回 null（任何写操作均被拒绝）。</param>
    public ModuleUpdater(
        ModuleRegistry registry,
        IReadOnlyList<string>? moduleDirectories = null,
        Func<ModuleKind?>? currentCallerKind = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _moduleDirectories = moduleDirectories ?? [];
        _currentCallerKind = currentCallerKind ?? (() => null);
    }

    /// <inheritdoc />
    public event EventHandler<UpdateStatus>? UpdateStatusChanged;

    /// <inheritdoc />
    public UpdateStatus GetUpdateStatus(string moduleId)
    {
        // 安装版本：优先注册表记录，其次安全设置中的 "version" 元数据，最后占位版本
        string installed = _registry.Get(moduleId)?.Version
            ?? _registry.GetSecureValue(moduleId, "version")?.ToString()
            ?? "0.0.0.0";
        // 2.1 本地骨架：无远端更新源 → LatestVersion=null、UpdateAvailable=false
        return new UpdateStatus(moduleId, installed, LatestVersion: null, UpdateAvailable: false, Message: "2.1 本地骨架：尚无更新源");
    }

    /// <inheritdoc />
    public void RequestUpdate(string moduleId)
    {
        // 2.1 骨架：无下载/校验流程，仅推送一次状态事件占位（后续版本接更新源后在此触发异步任务）
        UpdateStatusChanged?.Invoke(this, GetUpdateStatus(moduleId));
    }

    /// <inheritdoc />
    public T? GetSecurityConfig<T>(string moduleId, string key, T? fallback = default)
    {
        object? value = _registry.GetSecureValue(moduleId, key);
        return value is T typed ? typed : fallback;
    }

    /// <inheritdoc />
    public void SetSecurityConfig(string moduleId, string key, object? value)
    {
        RequireUpdateCaller(nameof(SetSecurityConfig));
        _registry.SetSecureValue(moduleId, key, value);
    }

    /// <inheritdoc />
    public bool ReplaceStandardModule(string moduleId, string newVersion, string packagePath)
    {
        RequireUpdateCaller(nameof(ReplaceStandardModule));

        // 包文件必须存在（下载/签名校验由后续版本补充）
        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            return false;

        // 元数据约定：模块当前生效版本存安全设置 secure.json 的 "version" 键
        _registry.SetSecureValue(moduleId, "version", newVersion);

        // 2.1 骨架：不做文件级原子替换。
        // 注释：后续版本在此执行目录级替换（备份 → 解包到 _moduleDirectories 对应目录 → 原子切换），
        // 并在替换完成后把新版本号写入安全设置、推送 UpdateStatusChanged。
        return true;
    }

    /// <summary>
    /// Update 专用面权限校验：仅 Kind=Update 的更新模块（内置特殊权限模块）
    /// 可调用 SetSecurityConfig / ReplaceStandardModule；其余调用方一律拒绝。
    /// </summary>
    private void RequireUpdateCaller(string operation)
    {
        ModuleKind? callerKind = _currentCallerKind();
        if (callerKind != ModuleKind.Update)
            throw new InvalidOperationException($"{operation} 仅更新模块（Kind=Update）可调用，当前调用方 Kind 为 {callerKind?.ToString() ?? "未知"}。");
    }
}
