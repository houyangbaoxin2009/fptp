using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 通用滤镜命令：把滤镜应用到当前文档首图层（2.1 简化——ABI 无 DocumentService/历史栈，
/// 直接以滤镜结果 with 派生新图层替换文档首层；宿主在命令执行后统一刷新画布）。
/// 参数由插件 BuildParameters 组装：设置项 > 模块配置 > 滤镜 Defaults 三级回退。
/// 滤镜输出尺寸变化时（智能裁切），宿主按新图层像素面尺寸处理（2.0 行为为生成新文档）。
/// </summary>
internal sealed class FilterCommand : ICommand
{
    private readonly IHostContext _host;
    private readonly BuiltinPlugin _plugin;
    private readonly IFilterProcessor _filter;
    private readonly string _id;
    private readonly string _displayName;

    public FilterCommand(IHostContext host, BuiltinPlugin plugin, IFilterProcessor filter, string id, string displayName)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    /// <inheritdoc />
    public string Id => _id;

    /// <inheritdoc />
    public string DisplayName => L10n.T(_displayName);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        var doc = _host.ActiveDocument;
        if (doc is null || doc.Layers.Count == 0)
            return; // 无文档：静默忽略（宿主菜单应通过 CanExecute 禁用，此处防御）

        // 组装参数（设置项/模块配置/Default 三级回退），执行滤镜（COW：源像素面不变）
        var layer = doc.Layers[0];
        var parameters = _plugin.BuildParameters(_filter);
        _host.Report.Report(0, L10n.T("正在执行：{0}…", _displayName));
        var result = _filter.Apply(layer.Pixels, parameters, _host.Report, CancellationToken.None);

        // 以滤镜结果派生新图层替换文档首层（COW：其余属性不变，历史可经旧引用回退）
        doc.Layers[0] = layer.WithPixels(result).WithName(_displayName);
        _host.Report.Report(100, L10n.T("{0}完成", _displayName));
    }
}

/// <summary>
/// 排版输出命令：把当前文档首图层照片按设置项选定的相纸（5寸/6寸/A4）网格居中排版，
/// 生成相纸尺寸的单层文档内容替换当前文档（插件内 LayoutComposer 轻量实现，不依赖 Core）。
/// 2.0 行为：排版结果作为新文档加载（LoadDocument，原文档保留在历史中可撤销回退）；
/// 2.1 契约版 ABI 无 DocumentService/文档替换接口，故直接替换当前文档内容，宿主命令后刷新。
/// </summary>
internal sealed class LayoutCommand : ICommand
{
    private readonly IHostContext _host;
    private readonly BuiltinPlugin _plugin;
    private readonly string _id;
    private readonly string _displayName;

    public LayoutCommand(IHostContext host, BuiltinPlugin plugin, string id, string displayName)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _id = id ?? throw new ArgumentNullException(nameof(id));
        _displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    /// <inheritdoc />
    public string Id => _id;

    /// <inheritdoc />
    public string DisplayName => L10n.T(_displayName);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        var doc = _host.ActiveDocument;
        if (doc is null || doc.Layers.Count == 0)
            return;

        // 从设置项读取相纸预设；排版器返回 null 表示未知预设
        string paperName = _plugin.LayoutPaperName;
        PixelSurface? paper = LayoutComposer.Compose(doc.Layers[0].Pixels, paperName, out int columns, out int rows);
        if (paper is null)
        {
            _host.Report.Report(0, L10n.T("未知相纸预设：{0}", paperName));
            return;
        }

        // 相纸结果作为单层替换当前文档内容（相纸尺寸即层像素面尺寸）
        var newLayer = new Layer(paper) { Name = L10n.T("排版({0})", paperName) };
        doc.Layers.Clear();
        doc.Layers.Add(newLayer);
        _host.Report.Report(100, L10n.T("排版完成：{0}，{1}×{2} 张", paperName, columns, rows));
    }
}
