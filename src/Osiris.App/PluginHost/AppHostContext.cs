using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Ui;
using Osiris.Core.Document;
using Osiris.Core.Plugins;

namespace Osiris.App.PluginHost;

/// <summary>
/// App 宿主上下文：实现 IHostContext，向模块暴露宿主能力面。
/// ActiveDocument 经服务注册表懒解析 DocumentService（DocumentService 由 CoreModule 在 Initialize 注册），
/// 保证与模块拿到的始终是同一实例。Report 记录最新进度供状态栏展示。
/// </summary>
internal sealed class AppHostContext : IHostContext
{
    private readonly ServiceRegistry _services;
    private readonly AppUiService _ui;
    private readonly StatusProgress _status = new();

    public AppHostContext(ServiceRegistry services, AppUiService ui)
    {
        _services = services;
        _ui = ui;
    }

    /// <summary>当前活动文档（懒解析自服务注册表；无文档时 null）。</summary>
    public OsirisDocument? ActiveDocument => _services.Get<DocumentService>()?.Document;

    /// <summary>服务注册表（模块互调）。</summary>
    public IServiceRegistry Services => _services;

    /// <summary>UI 服务（壳实现，模块贡献菜单/工具栏/面板/画布）。</summary>
    public IUiService? Ui => _ui;

    /// <summary>进度上报：记录最新值，供状态栏订阅展示。</summary>
    public IProgress Report => _status;

    /// <summary>最新进度记录器（状态栏绑定源）。</summary>
    public StatusProgress Status => _status;
}

/// <summary>
/// 进度记录器：保存最近一次上报的百分比与消息，变化时触发 Changed 事件（状态栏刷新用）。
/// </summary>
internal sealed class StatusProgress : IProgress
{
    /// <summary>进度变化事件（参数：百分比、消息）。</summary>
    public event Action<double, string>? Changed;

    /// <summary>最近一次百分比（0~100）。</summary>
    public double LatestPercent { get; private set; }

    /// <summary>最近一次消息。</summary>
    public string LatestMessage { get; private set; } = "";

    /// <summary>记录进度并触发事件。</summary>
    public void Report(double percent, string message)
    {
        LatestPercent = percent;
        LatestMessage = message;
        Changed?.Invoke(percent, message);
    }
}
