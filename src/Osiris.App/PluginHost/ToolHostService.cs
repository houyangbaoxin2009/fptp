using Osiris.Abstractions.Ui;
using Osiris.CoreModule.ViewModels;

namespace Osiris.App.PluginHost;

/// <summary>
/// 工具宿主服务（壳实现，注册进 Services）：聚合各模块（ITool）的交互工具，
/// ActivateTool 把指定工具设为画布当前激活工具（经画布 VM），并记录 CurrentToolId 供模块查询。
/// 操作窗口/画笔窗口点击工具按钮即经此切换。
/// </summary>
internal sealed class ToolHostService : IToolHostService
{
    private readonly Dictionary<string, IEditorTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<CanvasDocumentViewModel?> _canvasVm;
    private string? _currentToolId;

    /// <summary>构造：注入画布 VM 获取器（Dock 中画布文档的 CanvasDocumentViewModel）。</summary>
    public ToolHostService(Func<CanvasDocumentViewModel?> canvasVm) => _canvasVm = canvasVm;

    /// <summary>登记某模块（ITool）贡献的全部工具（重复 Id 覆盖）。</summary>
    public void RegisterModule(ITool module)
    {
        ArgumentNullException.ThrowIfNull(module);
        foreach (IEditorTool tool in module.Tools)
            _tools[tool.Id] = tool;
    }

    /// <inheritdoc />
    public IReadOnlyList<IEditorTool> Tools => _tools.Values.ToList();

    /// <inheritdoc />
    public IEditorTool? FindTool(string toolId) => _tools.GetValueOrDefault(toolId);

    /// <inheritdoc />
    public string? CurrentToolId => _currentToolId;

    /// <inheritdoc />
    public void ActivateTool(string toolId)
    {
        if (_tools.TryGetValue(toolId, out IEditorTool? tool) && _canvasVm() is { } vm)
        {
            vm.ActiveTool = tool;
            _currentToolId = toolId; // 记录当前激活工具 Id（模块取色/颜料盘目标判断用）
        }
    }
}
