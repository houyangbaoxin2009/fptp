using System.Globalization;
using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Ui;

namespace Fpter;

/// <summary>
/// 滤镜命令：把指定滤镜应用到当前文档首图层（可撤销）。
/// 经 IDocumentService.ApplyLayerChange（oldLayer → newLayer，历史栈），
/// 与滤镜窗口应用同一条历史路径，命令执行后可 Ctrl+Z 撤销。
/// 参数取滤镜 Defaults；如需交互参数请走滤镜窗口。
/// </summary>
internal sealed class FilterCommand : ICommand
{
    private readonly IHostContext _host;
    private readonly IFilterProcessor _filter;
    private readonly string _id;
    private readonly string _displayName;

    public FilterCommand(IHostContext host, IFilterProcessor filter, string id, string displayName)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
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
        var documents = _host.Services.Get<IDocumentService>();
        var doc = documents?.Document;
        if (documents is null || doc is null || doc.Layers.Count == 0)
            return; // 无文档：静默忽略

        Layer layer = doc.Layers[0];
        if (layer.Pixels.Width == 0 || layer.Pixels.Height == 0)
            return;

        _host.Report.Report(0, L10n.T("正在执行：{0}…", _displayName));
        var parameters = new FilterParameters();
        foreach (string key in _filter.Defaults.Keys)
            parameters[key] = _filter.Defaults[key];

        PixelSurface result = _filter.Apply(layer.Pixels, parameters, _host.Report, CancellationToken.None);
        Layer newLayer = layer.WithPixels(result);
        documents.ApplyLayerChange(layer.Id, layer, newLayer);
        _host.Report.Report(100, L10n.T("{0}完成", _displayName));
    }
}