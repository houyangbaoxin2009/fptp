namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块更新交互服务（经 host.Services.Get&lt;IModuleUpdater&gt;() 获取，所有模块可用）。
/// 权限分级：
/// - 通用面（所有模块可调）：GetUpdateStatus / RequestUpdate / UpdateStatusChanged 事件 /
///   GetSecurityConfig（安全设置可读不可写）；
/// - Update 专用面（实现须校验调用方 Kind=Update）：ReplaceStandardModule（替换内置模块文件）/
///   SetSecurityConfig（写安全设置）。
/// 2.1 实现本地状态与文件替换骨架（下载/签名校验后续版本补充）。
/// </summary>
public interface IModuleUpdater
{
    // ---- 通用面：任何模块均可调用，用于与更新模块交互 ----

    /// <summary>查询模块更新状态（当前安装版本/可更新版本/是否有更新/提示消息）。</summary>
    UpdateStatus GetUpdateStatus(string moduleId);

    /// <summary>请求更新指定模块（异步执行，进度经 UpdateStatusChanged 事件推送）。</summary>
    void RequestUpdate(string moduleId);

    /// <summary>更新状态变更事件（RequestUpdate 后推送最新 UpdateStatus，含完成/失败）。</summary>
    event EventHandler<UpdateStatus>? UpdateStatusChanged;

    /// <summary>
    /// 读取安全设置（可读不可写）；键不存在或类型不符返回 fallback（默认 null）。
    /// 安全设置存储于独立 secure.json，用户面写入口一律拒绝。
    /// </summary>
    T? GetSecurityConfig<T>(string moduleId, string key, T? fallback = default);

    // ---- Update 专用面：实现校验调用方 Kind=Update，其他模块调用抛 InvalidOperationException ----

    /// <summary>
    /// 替换/更新内置模块。packagePath 为新的模块包（dll 或压缩包）路径；
    /// 成功替换并更新注册表版本后返回 true，失败（权限不符/文件缺失/校验失败）返回 false。
    /// </summary>
    bool ReplaceStandardModule(string moduleId, string newVersion, string packagePath);

    /// <summary>写入安全设置（仅更新模块；写入即隔离落盘 secure.json）。</summary>
    void SetSecurityConfig(string moduleId, string key, object? value);
}

/// <summary>模块更新状态（通用面 GetUpdateStatus 返回，经 UpdateStatusChanged 事件推送）。</summary>
/// <param name="ModuleId">模块 Id。</param>
/// <param name="InstalledVersion">当前已安装版本。</param>
/// <param name="LatestVersion">可更新到的最新版本（无可用更新为 null）。</param>
/// <param name="UpdateAvailable">是否存在可用的更新。</param>
/// <param name="Message">状态/错误提示消息（如"正在下载…"、"校验失败"；可空）。</param>
public sealed record UpdateStatus(
    string ModuleId,
    string InstalledVersion,
    string? LatestVersion,
    bool UpdateAvailable,
    string? Message);
