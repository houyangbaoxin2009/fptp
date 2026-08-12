namespace Osiris.Abstractions.Modules;

/// <summary>
/// 模块签名校验器契约（安全骨架）：宿主在加载扩展模块前调用，校验模块来源可信度。
/// 2.1 默认实现为"放行所有模块"（App 注册），未来接入 Authenticode 数字签名校验或
/// 白名单机制——拒绝返回 false 的模块将不被加载（见 ModuleLoader 校验点）。
/// 设计：插件是 ALC 加载的任意代码，来源可信是防恶意模块的第一道防线（权限检测只是辅助）。
/// </summary>
public interface IModuleSignatureValidator
{
    /// <summary>
    /// 校验模块是否可信：返回 true 允许加载，false 拒绝（宿主记录拒绝原因）。
    /// 实现可基于 module.json 的 signature 字段、模块 DLL 数字签名、白名单目录等。
    /// </summary>
    /// <param name="moduleId">模块 Id（module.json id）。</param>
    /// <param name="moduleDirectory">模块根目录（含 module.json 与 DLL）。</param>
    /// <param name="signature">module.json 的 signature 字段（可空——无签名视为不可信来源）。</param>
    bool IsTrusted(string moduleId, string moduleDirectory, string? signature);
}
