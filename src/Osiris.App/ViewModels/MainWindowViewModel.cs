using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;
using Osiris.App.PluginHost;
using Osiris.App.Views;
using Osiris.Core.Plugins;

namespace Osiris.App.ViewModels;

/// <summary>菜单节点（工作台装配后的菜单树数据）。</summary>
public sealed record MenuNodeViewModel(string Title, ICommand? Command, IReadOnlyList<MenuNodeViewModel> Children, bool IsSeparator)
{
    /// <summary>Avalonia 绑定用命令包装（契约命令 → System.Windows.Input.ICommand）。</summary>
    public System.Windows.Input.ICommand? AvaloniaCommand => Command is null ? null : new CommandAdapter(Command);
}

/// <summary>工具栏项（工作台装配后的工具栏数据）。</summary>
public sealed record CommandItemViewModel(string CommandId, string DisplayName, ICommand Command)
{
    /// <summary>Avalonia 绑定用命令包装。</summary>
    public System.Windows.Input.ICommand? AvaloniaCommand => new CommandAdapter(Command);
}

/// <summary>面板宿主项（工作台装配后的 dock 面板数据）。</summary>
public sealed record PanelHostViewModel(string Title, object? Content, DockSide Side);

/// <summary>
/// 主窗口视图模型：从 AppUiService 收集的模块贡献装配工作台（菜单树/工具栏/面板/画布/状态栏）。
/// 壳零业务 UI——全部内容来自模块。
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ModuleRegistry _registry;
    private readonly IServiceRegistry _services;

    /// <summary>画布宿主内容（标准模块 SetCanvas 贡献）。</summary>
    [ObservableProperty] private object? canvasContent;

    /// <summary>菜单树（顶部菜单栏数据源）。</summary>
    [ObservableProperty] private IReadOnlyList<MenuNodeViewModel> menus = [];

    /// <summary>工具栏按钮（顶部工具栏数据源）。</summary>
    [ObservableProperty] private IReadOnlyList<CommandItemViewModel> toolbarItems = [];

    /// <summary>右侧停靠面板。</summary>
    [ObservableProperty] private IReadOnlyList<PanelHostViewModel> rightPanels = [];

    /// <summary>左侧停靠面板。</summary>
    [ObservableProperty] private IReadOnlyList<PanelHostViewModel> leftPanels = [];

    /// <summary>底部停靠面板。</summary>
    [ObservableProperty] private IReadOnlyList<PanelHostViewModel> bottomPanels = [];

    /// <summary>状态栏文本。</summary>
    [ObservableProperty] private string statusText = "就绪";

    public MainWindowViewModel(ModuleRegistry registry, IServiceRegistry services)
    {
        _registry = registry;
        _services = services;
    }

    /// <summary>打开模块管理窗口（模块列表 + 设置入口）。</summary>
    [RelayCommand]
    private void OpenModuleManager()
    {
        var owner = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime cdt
            ? cdt.MainWindow : null;
        var dlg = new ModuleManagerWindow(new ModuleManagerViewModel(_registry, _services));
        if (owner is not null) dlg.ShowDialog(owner);
        else dlg.Show();
    }

    /// <summary>从模块贡献装配工作台（模块全部加载后由壳调用一次；AppUiService 为程序集内部类型，本方法同程序集可见即可）。</summary>
    internal void Rebuild(AppUiService ui)
    {
        Menus = BuildMenuTree(ui.Menus, ui.Commands);
        ToolbarItems = ui.ToolbarItems
            .OrderBy(t => t.Order)
            .Where(t => ui.Commands.TryGetValue(t.CommandId, out var _))
            .Select(t => new CommandItemViewModel(t.CommandId, ui.Commands[t.CommandId].DisplayName, ui.Commands[t.CommandId]))
            .ToList();
        CanvasContent = ui.Canvas;
        RightPanels = ui.Panels.Where(p => p.Side == DockSide.Right).Select(ToHost).ToList();
        LeftPanels = ui.Panels.Where(p => p.Side == DockSide.Left).Select(ToHost).ToList();
        BottomPanels = ui.Panels.Where(p => p.Side == DockSide.Bottom).Select(ToHost).ToList();
        if (ui.StatusItems.Count > 0)
            StatusText = string.Join("  |  ", ui.StatusItems.OrderBy(s => s.Order).Select(s => s.Text));
    }

    private static PanelHostViewModel ToHost(PanelContribution p) => new(p.Title, p.Content, p.Side);

    /// <summary>把 "文件/打开" 形式的路径贡献构建为菜单树（按 "/" 分层，叶子挂命令）。</summary>
    private static List<MenuNodeViewModel> BuildMenuTree(
        IEnumerable<MenuContribution> menus,
        IReadOnlyDictionary<string, ICommand> commands)
    {
        var result = new List<MenuNodeViewModel>();
        foreach (var group in menus.OrderBy(m => m.Order).GroupBy(m => Head(m.Path)))
        {
            string head = group.Key;
            var entries = group.ToList();
            var leaf = entries.FirstOrDefault(m => Segments(m.Path).Length == 1);
            var subMenus = entries.Where(m => Segments(m.Path).Length > 1)
                .Select(m => new MenuContribution(string.Join("/", Segments(m.Path).Skip(1)), m.CommandId, m.Order));
            var children = BuildMenuTree(subMenus, commands);
            ICommand? cmd = leaf is not null && commands.TryGetValue(leaf.CommandId, out var c) ? c : null;
            result.Add(new MenuNodeViewModel(head, cmd, children, false));
        }
        return result;
    }

    private static string[] Segments(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    private static string Head(string path) => Segments(path)[0];
}
