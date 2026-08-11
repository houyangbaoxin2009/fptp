namespace Osiris.Abstractions.Ui;

/// <summary>
/// UI 服务契约：模块（插件）向壳贡献命令/菜单/工具栏/dock 面板/画布/状态栏。
/// 壳=模块运行时+空工作台框架，界面元素全部由模块贡献；
/// 壳实现此接口；无 UI 宿主（CLI/测试）时为 null，模块跳过 UI 注册。
/// </summary>
public interface IUiService
{
    /// <summary>注册命令（模块经此暴露可执行动作）。</summary>
    void RegisterCommand(ICommand command);

    /// <summary>贡献菜单项（路径如 "图像/换底色"）。</summary>
    void AddMenu(string path, string commandId, int order);

    /// <summary>贡献工具栏按钮（commandId 对应已注册命令；order 越小越靠前）。</summary>
    void AddToolbar(string commandId, int order);

    /// <summary>
    /// 贡献 dock 面板（content 为任意内容对象：宿主按类型渲染——若为已生成的控件实例，
    /// Dock 浮动/移动时可能触发"双父级"崩溃；模块贡献 UI 视图请用 AddPanel(title, viewFactory, side) 工厂重载）。
    /// </summary>
    void AddPanel(string title, object content, DockSide side = DockSide.Right);

    /// <summary>
    /// 贡献 dock 面板（视图工厂版）：每次 Dock 浮动/停靠重建内容时调用工厂生成**新的**控件实例，
    /// 避免同一控件实例跨窗口双父级崩溃；工厂内捕获的模块状态（如 ToolState 单例）保证新视图共享数据。
    /// </summary>
    void AddPanel(string title, Func<object> viewFactory, DockSide side = DockSide.Right);

    /// <summary>贡献画布控件（content 为宿主画布实例；仅标准模块提供，后注册者覆盖）。</summary>
    void SetCanvas(object canvas);

    /// <summary>贡献状态栏条目（order 越小越靠前）。</summary>
    void AddStatusItem(string text, int order);
}
