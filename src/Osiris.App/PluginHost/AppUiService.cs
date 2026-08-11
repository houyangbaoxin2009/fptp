using Osiris.Abstractions.Ui;

namespace Osiris.App.PluginHost;

/// <summary>
/// App UI 服务：实现 IUiService，收集各模块贡献的菜单/工具栏/面板/画布/命令，
/// 供工作台（MainWindowViewModel.Rebuild）统一装配。壳本身不定义任何业务 UI。
/// </summary>
internal sealed class AppUiService : IUiService
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly List<MenuContribution> _menus = new();
    private readonly List<ToolbarContribution> _toolbarItems = new();
    private readonly List<PanelContribution> _panels = new();
    private readonly List<StatusContribution> _statusItems = new();
    private object? _canvas;

    /// <summary>全部已注册命令（Id → ICommand）。</summary>
    public IReadOnlyDictionary<string, ICommand> Commands => _commands;

    /// <summary>菜单贡献列表（路径如 "文件/打开"）。</summary>
    public IReadOnlyList<MenuContribution> Menus => _menus;

    /// <summary>工具栏贡献列表。</summary>
    public IReadOnlyList<ToolbarContribution> ToolbarItems => _toolbarItems;

    /// <summary>面板贡献列表。</summary>
    public IReadOnlyList<PanelContribution> Panels => _panels;

    /// <summary>状态栏贡献列表。</summary>
    public IReadOnlyList<StatusContribution> StatusItems => _statusItems;

    /// <summary>画布控件（标准模块 SetCanvas 贡献）。</summary>
    public object? Canvas => _canvas;

    /// <summary>注册命令（同 Id 覆盖）。</summary>
    public void RegisterCommand(ICommand command) => _commands[command.Id] = command;

    /// <summary>贡献菜单项（路径如 "图像/换底色"）。</summary>
    public void AddMenu(string path, string commandId, int order) => _menus.Add(new MenuContribution(path, commandId, order));

    /// <summary>贡献工具栏按钮。</summary>
    public void AddToolbar(string commandId, int order) => _toolbarItems.Add(new ToolbarContribution(commandId, order));

    /// <summary>贡献停靠面板。</summary>
    public void AddPanel(string title, object content, DockSide side = DockSide.Right) => _panels.Add(new PanelContribution(title, content, side));

    /// <summary>贡献画布控件（后注册覆盖）。</summary>
    public void SetCanvas(object canvas) => _canvas = canvas;

    /// <summary>贡献状态栏文本。</summary>
    public void AddStatusItem(string text, int order) => _statusItems.Add(new StatusContribution(text, order));
}

/// <summary>菜单贡献：路径 + 命令 Id + 排序。</summary>
internal sealed record MenuContribution(string Path, string CommandId, int Order);

/// <summary>工具栏贡献：命令 Id + 排序。</summary>
internal sealed record ToolbarContribution(string CommandId, int Order);

/// <summary>面板贡献：标题 + 内容 + 停靠侧。</summary>
internal sealed record PanelContribution(string Title, object? Content, DockSide Side);

/// <summary>状态栏贡献：文本 + 排序。</summary>
internal sealed record StatusContribution(string Text, int Order);
