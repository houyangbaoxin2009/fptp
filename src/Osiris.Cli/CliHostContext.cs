using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Ui;
using Osiris.Core.Plugins;

namespace Osiris.Cli;

/// <summary>
/// CLI 宿主上下文（IHostContext 的 CLI 实现，见架构 4.8/4.9 节）：
/// - ActiveDocument = null：CLI 无文档模型，模块应跳过依赖文档的逻辑；
/// - Services = ServiceRegistry：预注册 IModuleRegistry / IModuleUpdater，模块经 host.Services 互调；
/// - Ui = null：无 UI 宿主，模块契约约定跳过 UI 注册（Ui==null 即"仅贡献 CLI 命令"信号）；
/// - Report = ConsoleProgress：进度/消息打到 stderr，百分比变化才打印防刷屏。
/// 与 GUI 同一安全设计：本上下文只注入共享注册表，不注入任何特权回调
/// （IModuleUpdater 的 Update 专用面在 CLI 下恒被拒绝——"CLI 无特权"）。
/// </summary>
internal sealed class CliHostContext : IHostContext
{
    // 服务注册表（模块互调）：注册 IModuleRegistry / IModuleUpdater 等宿主服务
    private readonly ServiceRegistry _services = new();

    /// <summary>构造：以共享模块注册表构建宿主服务面（CLI 与 GUI 共用同一注册表实例族）。</summary>
    /// <param name="registry">共享 ModuleRegistry（modules.json/settings.json/secure.json 与 GUI 同路径）。</param>
    public CliHostContext(ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // 模块可经 Services 获取：注册表（状态/配置读写）与更新器（更新状态查询、安全设置只读）。
        // 不传入模块目录与调用方 Kind 回调 → ModuleUpdater 缺省 currentCallerKind=null，
        // 使 ReplaceStandardModule/SetSecurityConfig（Update 专用面）在 CLI 下必然抛异常——CLI 无特权。
        _services.Register<IModuleRegistry>(registry);
        _services.Register<IModuleUpdater>(new ModuleUpdater(registry));
        Report = new ConsoleProgress();
    }

    /// <inheritdoc />
    public OsirisDocument? ActiveDocument => null;

    /// <inheritdoc />
    public IServiceRegistry Services => _services;

    /// <inheritdoc />
    public IUiService? Ui => null;

    /// <inheritdoc />
    public IProgress Report { get; }
}
