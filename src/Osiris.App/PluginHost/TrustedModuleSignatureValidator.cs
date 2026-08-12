using Osiris.Abstractions.Modules;
using Osiris.Core.Plugins;
using Osiris.Core.Security;

namespace Osiris.App.PluginHost;

/// <summary>
/// 模块签名校验器（哈希白名单实现）：校验模块主 DLL 的 SHA-256 ∈（内置信任名单 ∪ 用户信任名单）。
/// - 内置名单（trusted-modules.json 随产品分发）：官方模块防篡改——DLL 被替换 → 哈希失配 → 拒绝加载。
/// - 用户名单（%APPDATA%/Fptp/trusted-modules.json）：外部模块经确认框"确认加载"后写入，后续启动自动通过。
/// - 无内置名单文件（开发模式）→ 降级放行（防锁死开发；外部模块仍走确认流程）。
/// - 校验数据源：module.json 的 entryPoint 指定的主 DLL；signature 字段为预留（可选外部声明）。
/// </summary>
internal sealed class TrustedModuleSignatureValidator : IModuleSignatureValidator
{
    private readonly ModuleTrustStore _store;

    public TrustedModuleSignatureValidator(ModuleTrustStore store) => _store = store;

    /// <inheritdoc />
    public bool IsTrusted(string moduleId, string moduleDirectory, string? signature)
    {
        // 主 DLL 路径：module.json entryPoint（与 ModuleLoader 同源）
        string entryPoint = ModuleLoader.ReadEntryPoint(moduleDirectory) ?? "";
        string dllPath = Path.Combine(moduleDirectory, entryPoint);
        string? hash = HashUtil.Sha256File(dllPath);
        if (hash is null)
            return false; // 主 DLL 缺失/不可读 → 不可信

        // 信任名单命中 → 可信
        if (_store.IsTrusted(moduleId, hash))
            return true;

        // 无内置名单（开发模式）→ 降级放行（哈希校验未启用）；有名单 → 严格拒绝（防篡改）
        return !_store.HasBuiltinTrustFile;
    }
}
