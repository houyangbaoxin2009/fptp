namespace Osiris.Abstractions.Ui;

/// <summary>
/// 编辑器工具契约：画布交互工具（套索/画笔/移动等）的插件入口。
/// 宿主把画布鼠标事件转换为 ToolMouseEvent（文档坐标）后分发给当前激活工具，
/// 并每帧调用 DrawOverlay 让工具绘制覆盖层提示。
/// </summary>
public interface IEditorTool : IPlugin
{
    /// <summary>
    /// 视觉状态变化事件：工具在操作过程中（套索收集点、矩形拖动、画笔预览等）修改了覆盖层/视觉状态时触发，
    /// 宿主（画布）订阅后请求重绘——实现"操作中实时渲染"（DrawOverlay 随每次重绘被调用）。
    /// </summary>
    event Action? VisualChanged;

    /// <summary>工具被激活为当前工具（宿主在状态切换时清理内部状态）。</summary>
    void Activate();

    /// <summary>工具被停用（清理临时状态/覆盖层）。</summary>
    void Deactivate();

    /// <summary>鼠标按下（开始一笔交互）。</summary>
    void MouseDown(ToolMouseEvent e);

    /// <summary>鼠标移动（按住拖动时每帧触发）。</summary>
    void MouseMove(ToolMouseEvent e);

    /// <summary>鼠标抬起（结束一笔交互，如套索落定生成选区）。</summary>
    void MouseUp(ToolMouseEvent e);

    /// <summary>绘制工具覆盖层（每帧渲染时调用）。</summary>
    void DrawOverlay(IToolOverlay overlay);
}
