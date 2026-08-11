namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块注册表契约（用户面）：记录全部模块（含 Removed）信息与模块级配置。
/// 实现归 Core（ModuleRegistry）：JSON 即时持久化，损坏文件重置为空；未记录 Id 的模块视为默认启用。
/// <para>权限规则：</para>
/// - 对 Standard 的 SetEnabled/MarkRemoved 一律拒绝（抛 InvalidOperationException）——用户不可禁用/卸载内置模块；
/// - SetConfig/GetConfig/ResetConfig 对全部模块（含 Standard）开放——用户可更改内置模块的设置；
/// - 内置模块文件不可被用户更改（替换权限仅归 Update 模块，经 IModuleUpdater）。
/// </summary>
public interface IModuleRegistry
{
    /// <summary>全部模块记录（含已卸载 Removed，供管理界面展示）。</summary>
    IReadOnlyList<ModuleRecord> Modules { get; }

    /// <summary>登记模块（从 module.json / 反射扫描）；重复 Id 覆盖旧记录。</summary>
    void Register(ModuleRecord record);

    /// <summary>按 Id 取模块记录；未登记返回 null。</summary>
    ModuleRecord? Get(string id);

    /// <summary>查询模块是否启用；未记录 Id 默认视为启用（返回 true）。</summary>
    bool IsEnabled(string id);

    /// <summary>设置模块启用/禁用（状态变更即时持久化；Standard 抛 InvalidOperationException）。</summary>
    void SetEnabled(string id, bool enabled);

    /// <summary>标记模块已卸载（持久化，此后不再加载；仅 Extension 合法，Standard 抛 InvalidOperationException）。</summary>
    void MarkRemoved(string id);

    /// <summary>
    /// 读取模块级配置（全部模块开放）；键不存在或类型不符返回 fallback（默认 null）。
    /// 键约定 "组.键"（如 "chgcolor.threshold"）以隔离命名空间。
    /// </summary>
    T? GetConfig<T>(string moduleId, string key, T? fallback = default);

    /// <summary>写入模块级配置（即时 JSON 落盘，全部模块开放）；value 为 null 等价删除该键。</summary>
    void SetConfig(string moduleId, string key, object? value);

    /// <summary>清空指定模块的全部配置并落盘（全部模块开放）。</summary>
    void ResetConfig(string moduleId);
}
