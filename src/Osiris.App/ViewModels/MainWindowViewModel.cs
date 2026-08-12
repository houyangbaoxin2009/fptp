using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;
using Osiris.App.PluginHost;
using Osiris.App.Views;
using Osiris.Core.Plugins;
using Osiris.CoreModule.ViewModels;

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

    /// <summary>模块注册表（壳级工具窗口构造 VM 用；构造注入，非空）。</summary>
    public ModuleRegistry Registry { get; }

    /// <summary>命令表（Rebuild 时从模块贡献收集；快捷键路由执行用）。</summary>
    public IReadOnlyDictionary<string, ICommand> Commands { get; private set; } = new Dictionary<string, ICommand>();

    /// <summary>VSCode/VS 风格停靠工厂（画布 + 工具窗口可拖拽停靠/浮动/标签化）。</summary>
    public WorkbenchDockFactory DockFactory { get; }

    /// <summary>画布宿主内容（标准模块 SetCanvas 贡献）。</summary>
    [ObservableProperty] private object? canvasContent;

    /// <summary>画布视图模型（标准模块贡献的 CanvasDocumentViewModel；工具激活目标）。</summary>
    [ObservableProperty] private CanvasDocumentViewModel? canvasViewModel;

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
        Registry = registry;
        _services = services;
        DockFactory = new WorkbenchDockFactory();
        _ = DockFactory.Layout; // 强制创建布局（CreateLayout + InitLayout，供 DockControl 绑定）
        StatusText = L10n.T("就绪"); // 初始翻译（语言服务已由壳注入 L10n）
    }

    /// <summary>打开模块管理工具窗口（Dock 停靠，可拖拽/浮动；重复打开激活既有窗口；视图工厂防双父级）。</summary>
    [RelayCommand]
    private void OpenModuleManager()
    {
        DockFactory.ShowToolWindow("moduleManager", L10n.T("模块管理"),
            () => new ModuleManagerView { DataContext = new ModuleManagerViewModel(Registry, _services) });
    }

    /// <summary>
    /// 打开设置独立窗口（不可停靠工作区；非模态，CenterOwner）。
    /// 独立窗口设计：规避 Dock 浮动设置的卡死问题，且设置不允许停靠到工作区。
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        if (Registry is not { } registry) return;
        var owner = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime cdt ? cdt.MainWindow : null;
        // 传入共享语言服务：语言设置保存时切换语言包（LanguageChanged → 壳 Rebuild 即时刷新）
        var localization = _services.Get<Osiris.Abstractions.Localization.ILocalizationService>();
        // Avalonia 12：Owner setter 受保护，经 Show(owner) 重载设置 Owner 并显示（CenterOwner 定位）
        var dlg = new SettingsWindow(new SettingsViewModel(registry, localization));
        if (owner is not null)
            dlg.Show(owner);
        else
            dlg.Show();
    }

    /// <summary>关闭壳级工具窗口（模块管理"关闭"按钮等经此从停靠布局移除）。</summary>
    public void CloseToolWindow(string id) => DockFactory.CloseToolWindow(id);

    /// <summary>从模块贡献装配工作台（模块全部加载后由壳调用；语言切换后再次调用以刷新文本）。
    /// 面板停靠 Id 用**原文标题**作稳定标识（生命周期归模块，切换语言不重建布局），
    /// 菜单/工具栏/面板显示标题全部经 L10n 翻译（命令 DisplayName 惰性翻译，Rebuild 即取当前语言）。</summary>
    internal void Rebuild(AppUiService ui)
    {
        Commands = new Dictionary<string, ICommand>(ui.Commands);
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
            StatusText = string.Join("  |  ", ui.StatusItems.OrderBy(s => s.Order).Select(s => L10n.T(s.Text)));
        else
            StatusText = L10n.T("就绪"); // 无状态项时保持"就绪"（语言切换 Rebuild 时重新翻译）

        // 停靠工作台：画布注入中央文档，模块面板注入对应停靠区
        CanvasViewModel = ui.Canvas as CanvasDocumentViewModel;
        DockFactory.SetCanvasContext(ui.Canvas);
        foreach (var p in ui.Panels)
            DockFactory.AddToolPanel($"panel.{p.Title}", L10n.T(p.Title), p.Content, p.Side);
    }

    private static PanelHostViewModel ToHost(PanelContribution p) => new(L10n.T(p.Title), p.Content, p.Side);

    /// <summary>把 "文件/打开" 形式的路径贡献构建为菜单树（按 "/" 分层，叶子挂命令）。
    /// 路径段即翻译 key：每个段经 L10n.T 取当前语言文本（语言包 key 即中文原文）。</summary>
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
            result.Add(new MenuNodeViewModel(L10n.T(head), cmd, children, false));
        }
        return result;
    }

    private static string[] Segments(string path) => path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    private static string Head(string path) => Segments(path)[0];
}
