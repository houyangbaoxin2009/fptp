using System.Reflection;
using Xunit;

namespace Osiris.Plugins.Tests;

/// <summary>
/// ABI 红线测试：插件程序集的引用面必须只依赖 Osiris.Abstractions，
/// 不得引用 SkiaSharp / Avalonia / Osiris.Core（宿主渲染与实现层类型分裂禁止）。
/// </summary>
[Collection("Plugins")]
public class AbiRedLineTests
{
    [Fact]
    public void PluginAssembly_ReferencesOnlyAbstractions_NoSkiaAvaloniaCore()
    {
        // 意图：Fptp.Plugins.Builtin.dll 的 GetReferencedAssemblies()
        // 必须包含 Osiris.Abstractions 且不包含 SkiaSharp/Avalonia/Osiris.Core。
        string dllPath = System.IO.Path.Combine(PluginsBinLocator.Path, "Fptp.Plugins.Builtin.dll");
        Assert.True(File.Exists(dllPath), $"插件 dll 不存在: {dllPath}");

        // 元数据读取：GetReferencedAssemblies 只解析程序集名，不触发依赖加载
        AssemblyName[] references = Assembly.LoadFrom(dllPath).GetReferencedAssemblies();

        Assert.DoesNotContain(references, r => r.Name == "SkiaSharp");
        Assert.DoesNotContain(references, r => r.Name == "Avalonia");
        Assert.DoesNotContain(references, r => r.Name == "Osiris.Core");
        Assert.Contains(references, r => r.Name == "Osiris.Abstractions");
    }

    [Fact]
    public void FptmAssembly_ReferencesOnlyAbstractionsAndAvalonia_NoForbiddenAssemblies()
    {
        // 意图：Fptm.dll（传统编辑模块）ABI 约束——
        // 允许引用 Osiris.Abstractions + Avalonia（托管 UI 层，经 ALC 转发默认上下文）；
        // 禁止 SkiaSharp（native 依赖不可随 ALC 卸载）/ Osiris.Core / Osiris.Engine.Skia。
        string dllPath = System.IO.Path.Combine(PluginsBinLocator.Path, "Fptm", "Fptm.dll");
        Assert.True(File.Exists(dllPath), $"插件 dll 不存在: {dllPath}");

        AssemblyName[] references = Assembly.LoadFrom(dllPath).GetReferencedAssemblies();

        Assert.DoesNotContain(references, r => r.Name == "SkiaSharp");
        Assert.DoesNotContain(references, r => r.Name == "Osiris.Core");
        Assert.DoesNotContain(references, r => r.Name == "Osiris.Engine.Skia");
        Assert.Contains(references, r => r.Name == "Osiris.Abstractions");
    }
}
