namespace Osiris.Abstractions.Ui;

/// <summary>
/// 工具宿主服务：画布工具的注册与激活入口（壳实现并注册进 Services）。
/// 模块（fptm 等）经 host.Services.Get&lt;IToolHostService&gt;() 获取：
/// - Tools：全部模块贡献的交互工具（IToolPlugin.Tools 聚合）；
/// - ActivateTool(toolId)：把指定工具设为当前画布激活工具（壳路由到画布 VM）。
/// 工具窗口（操作窗口/画笔窗口）点击工具按钮即调用 ActivateTool 切换。
/// </summary>
public interface IToolHostService
{
    /// <summary>全部已注册工具（各 IToolPlugin 聚合）。</summary>
    IReadOnlyList<IEditorTool> Tools { get; }

    /// <summary>按工具 Id 查找工具；未找到返回 null。</summary>
    IEditorTool? FindTool(string toolId);

    /// <summary>激活指定工具为当前画布工具（壳经画布 VM 设置 ActiveTool 并重绘）。</summary>
    void ActivateTool(string toolId);
}
