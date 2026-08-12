using Osiris.Abstractions.Modules;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 模块签名校验安全骨架测试：IModuleSignatureValidator 拒绝的模块不被加载
/// （ModuleLoader.LoadManifest 校验点拦截，onError 上报拒绝原因）。
/// </summary>
[Collection("Plugins")]
public class ModuleSignatureTests
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-sig-tests", Guid.NewGuid().ToString("N"));

    public ModuleSignatureTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 清理失败不影响测试结果 */ }
    }

    private ModuleRegistry CreateRegistry() => new(
        Path.Combine(_tempDir, "modules.json"),
        Path.Combine(_tempDir, "settings.json"),
        Path.Combine(_tempDir, "secure.json"),
        new JsonConfigStore());

    /// <summary>执行加载并收集错误：返回 (成功数, 错误列表)。</summary>
    private static (int Loaded, List<string> Errors) LoadPlugins(ModuleRegistry registry, TestHostContext context)
    {
        var errors = new List<string>();
        int loaded = ModuleLoader.LoadFromDirectory(
            PluginsBinLocator.Path, registry, context,
            (ctx, ex) => errors.Add($"{ctx}: {ex.Message}"));
        return (loaded, errors);
    }

    [Fact]
    public void Validator拒绝全部模块_加载数为0且上报拒绝原因()
    {
        ModuleRegistry registry = CreateRegistry();
        var context = new TestHostContext();
        // 拒绝所有模块的校验器（模拟"不可信来源"）
        context.Services.Register<IModuleSignatureValidator>(
            new RejectAllValidator());

        (int loaded, List<string> errors) = LoadPlugins(registry, context);

        Assert.Equal(0, loaded);                              // 全部被拦截
        Assert.NotEmpty(errors);                              // 拒绝原因已上报
        Assert.Contains(errors, e => e.Contains("未通过签名校验")); // 明确拒绝文案
    }

    [Fact]
    public void Validator放行全部_与默认行为一致()
    {
        ModuleRegistry registry = CreateRegistry();
        var context = new TestHostContext();
        context.Services.Register<IModuleSignatureValidator>(
            new TrustAllValidator());

        (int loaded, _) = LoadPlugins(registry, context);

        Assert.True(loaded > 0); // 与不注册 validator（默认放行）行为一致
    }

    /// <summary>拒绝所有模块的校验器（安全测试用）。</summary>
    private sealed class RejectAllValidator : IModuleSignatureValidator
    {
        public bool IsTrusted(string moduleId, string moduleDirectory, string? signature) => false;
    }

    /// <summary>信任所有模块的校验器（与默认放行等价）。</summary>
    private sealed class TrustAllValidator : IModuleSignatureValidator
    {
        public bool IsTrusted(string moduleId, string moduleDirectory, string? signature) => true;
    }
}
