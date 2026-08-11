using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;

namespace Fptp.Filters;

/// <summary>
/// 滤镜模块（fptp.filters）：提供可停靠的滤镜窗口（内置滤镜列表 + 参数 + 应用）。
/// 滤镜实现归各滤镜模块（fptp.idphoto 等 IFilterPlugin）；本模块只做统一展示与应用入口，
/// 经宿主注册的滤镜解析服务（Func&lt;string, IFilterProcessor?&gt; / Func&lt;IReadOnlyList&lt;IFilterProcessor&gt;&gt;）获取滤镜。
/// </summary>
[PluginExport]
public sealed class FiltersModule : IModule
{
    /// <summary>宿主上下文（窗口视图经此访问服务）。</summary>
    public static IHostContext? HostContext { get; private set; }

    /// <inheritdoc />
    public string Id => "fptp.filters";

    /// <inheritdoc />
    public string Name => "滤镜模块";

    /// <inheritdoc />
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        HostContext = host;

        if (host.Ui is { } ui)
            ui.AddPanel("滤镜", () => new Views.FilterWindowView(), DockSide.Right);
    }
}
