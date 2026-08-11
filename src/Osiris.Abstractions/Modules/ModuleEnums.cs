namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块分级：决定加载方式、分发形态与用户操作权限。
/// Standard 随产品分发、静态加载（编译期引用）；Extension 独立安装、ALC 动态加载可卸载；
/// Update 为特殊权限的内置更新模块。
/// </summary>
public enum ModuleKind
{
    /// <summary>标准模块（内置）：用户不可拆卸/更改其文件（仅 Update 模块可替换），但用户可更改其设置。</summary>
    Standard = 0,

    /// <summary>扩展模块：用户可自由安装/卸载/更改文件与设置。</summary>
    Extension = 1,

    /// <summary>更新模块（内置，特殊权限）：唯一有权替换/更新内置模块（Standard）文件的模块。</summary>
    Update = 2,
}

/// <summary>
/// 模块状态：注册表持久化记录，决定模块是否参与加载。
/// Removed 用于已卸载的扩展模块，避免其 module.json 再次被扫描加载。
/// </summary>
public enum ModuleStatus
{
    /// <summary>启用：正常加载。</summary>
    Enabled = 0,

    /// <summary>禁用：跳过加载（用户手动关闭）。</summary>
    Disabled = 1,

    /// <summary>已卸载：标记移除，不再加载（仅扩展模块可用）。</summary>
    Removed = 2,
}

/// <summary>
/// 模块实现语言：DotNet 为当前主力，Tie 为预留的脚本语言（语言中立契约）。
/// 契约层零语言依赖——tie 模块经同一 IHostContext 注册命令/滤镜/设置组。
/// </summary>
public enum ScriptLanguage
{
    /// <summary>.NET 程序集（C#/F# 等编译产物）。</summary>
    DotNet = 0,

    /// <summary>Tie 脚本语言（预留，未来由 TieHost 标准模块解释执行）。</summary>
    Tie = 1,
}

/// <summary>
/// 模块载体类型：决定 EntryPoint 的含义与加载路径。
/// </summary>
public enum ModuleType
{
    /// <summary>原生程序集：EntryPoint 为 dll 文件名，宿主反射扫描 [PluginExport] 实现 IModule 的类型。</summary>
    Native = 0,

    /// <summary>Tie 脚本：EntryPoint 为 .tie 文件路径，由 TieHost 解释执行。</summary>
    Script = 1,
}
