namespace Osiris.Abstractions.Ui;

/// <summary>
/// 编辑器工具契约：画布交互工具（套索/画笔/移动等）的插件入口。
/// 宿主把画布鼠标事件转换为 ToolMouseEvent（文档坐标）后分发给当前激活工具，
/// 并每帧调用 DrawOverlay 让工具绘制覆盖层提示。
/// </summary>
public interface IEditorTool : IPlugin
{
    /// <summary>工具被激活为当前工具（可在此重置内部状态）。</summary>
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
