namespace Osiris.Abstractions.Settings;

/// <summary>
/// 设置级别：决定设置的展示面与写权限。
/// User=用户设置（面板展示，用户可改）；
/// Core=核心设置（面板展示，用户可改——决定核心行为的参数）；
/// Security=安全设置（面板隐藏，仅更新模块经 IModuleUpdater.SetSecurityConfig 可改，
/// 用户/普通模块可读不可写；存储上与 User/Core 隔离于独立 secure.json 文件）。
/// </summary>
public enum SettingScope
{
    /// <summary>用户设置：面板展示，用户可改。</summary>
    User = 0,

    /// <summary>核心设置：面板展示，用户可改（决定软件核心行为的参数）。</summary>
    Core = 1,

    /// <summary>安全设置：面板隐藏，仅更新模块可写（用户/普通模块只读）。</summary>
    Security = 2,
}
