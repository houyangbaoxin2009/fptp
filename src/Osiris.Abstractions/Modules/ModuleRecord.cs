namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块记录：模块注册表中的不可变数据行（来源于 module.json 清单或反射扫描兜底）。
/// 状态变更（启用/禁用/卸载）由注册表以 with 表达式派生新记录，保持不可变性。
/// </summary>
/// <param name="Id">模块唯一 Id（如 "fptp.idphoto"）。</param>
/// <param name="Name">模块显示名。</param>
/// <param name="Version">模块版本（SemVer）。</param>
/// <param name="Kind">分级：Standard 静态加载 / Extension ALC 动态加载。</param>
/// <param name="Status">当前状态（启用/禁用/已卸载）。</param>
/// <param name="Type">载体类型：Native 程序集 / Script tie 脚本。</param>
/// <param name="Language">实现语言（tie 预留）。</param>
/// <param name="EntryPoint">Native：dll 文件名（相对 LoadPath）；Script：.tie 文件路径。</param>
/// <param name="LoadPath">程序集所在目录（Native 用，ALC 解析基础；Script 为 null）。</param>
public sealed record ModuleRecord(
    string Id,
    string Name,
    string Version,
    ModuleKind Kind,
    ModuleStatus Status,
    ModuleType Type,
    ScriptLanguage Language,
    string? EntryPoint,
    string? LoadPath);
