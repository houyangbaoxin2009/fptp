using Osiris.Abstractions;
using Osiris.Abstractions.Document;

namespace FpSDK;

/// <summary>
/// 宿主上下文便捷访问：把 <see cref="IHostContext"/> 的常用能力收拢为强类型助手，
/// 供模块在 <see cref="ModuleBase.OnInitialize"/> 内少写判空与类型转换。
/// </summary>
public sealed class FpContext
{
    private readonly IHostContext _host;

    private FpContext(IHostContext host) => _host = host;

    /// <summary>包装指定宿主上下文。</summary>
    public static FpContext From(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        return new FpContext(host);
    }

    /// <summary>原始宿主上下文。</summary>
    public IHostContext Host => _host;

    /// <summary>当前活动文档（无文档时为 null）。</summary>
    public OsirisDocument? ActiveDocument => _host.ActiveDocument;

    /// <summary>是否有 UI 宿主（CLI/测试下为 false，应跳过 UI 注册）。</summary>
    public bool HasUi => _host.Ui is not null;

    /// <summary>按接口类型获取已注册服务（未注册返回 null）。</summary>
    public T? Service<T>() where T : class => _host.Services.Get<T>();

    /// <summary>注册模块服务（同类型重复注册则覆盖）。</summary>
    public void Register<T>(T service) where T : class => _host.Services.Register(service);

    /// <summary>上报进度。</summary>
    public void Report(double percent, string message) => _host.Report.Report(percent, message);
}