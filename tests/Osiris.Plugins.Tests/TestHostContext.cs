using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Ui;
using Osiris.Core.Plugins;

namespace Osiris.Plugins.Tests;

/// <summary>
/// 测试用宿主上下文（CliHostContext 的测试等价物）：
/// Ui=null（插件跳过命令/菜单注册），Services=ServiceRegistry（插件可经此获取服务），
/// 模拟 CLI/无头宿主形态。
/// </summary>
public sealed class TestHostContext : IHostContext
{
    public TestHostContext()
    {
        Services = new ServiceRegistry();
    }

    /// <summary>测试无活动文档。</summary>
    public OsirisDocument? ActiveDocument => null;

    /// <summary>服务注册表（Core ServiceRegistry 实现）。</summary>
    public IServiceRegistry Services { get; }

    /// <summary>无 UI 宿主：插件据此跳过 UI 注册路径。</summary>
    public IUiService? Ui => null;

    /// <summary>静默进度实现（滤镜/批处理内部上报不关心）。</summary>
    public IProgress Report => new NullProgress();

    /// <summary>静默进度实现。</summary>
    private sealed class NullProgress : IProgress
    {
        public void Report(double percent, string message)
        {
            // 测试不关心进度上报
        }
    }
}
