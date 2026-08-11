using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Dock.Settings;
using Osiris.Abstractions.Ui;

namespace Osiris.App.ViewModels;

/// <summary>
/// 工作台停靠工厂：构建 VSCode/VS 风格布局（左/右/底部工具停靠区 + 中央文档区画布）。
/// 全部工具窗口（模块面板、模块管理、设置）可拖拽停靠/浮动/标签化。
/// Dock 12 API：LayoutDocument→Document、LayoutTool→Tool、AddDockable(IDock, IDockable)。
/// </summary>
public sealed class WorkbenchDockFactory : Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private ToolDock? _leftDock;
    private ToolDock? _rightDock;
    private ToolDock? _bottomDock;

    // 全部 dockable 登记表（按 Id；面板/工具窗口重复注册忽略或激活既有项）
    private readonly Dictionary<string, IDockable> _dockables = new();

    /// <summary>停靠布局根（首次访问时自动 CreateLayout + InitLayout；XAML 绑定源）。</summary>
    public IRootDock? Layout
    {
        get
        {
            if (_rootDock is null)
            {
                _rootDock = CreateLayout();
                InitLayout(_rootDock);
            }
            return _rootDock;
        }
    }

    /// <summary>构建初始布局：左/右/底部工具区 + 中央画布文档。</summary>
    public override IRootDock CreateLayout()
    {
        // 中央画布文档（内容在 Rebuild 时经 SetCanvasContext 注入；不可关闭/浮动/图钉，可拖拽换位）
        var canvas = new Document
        {
            Id = "osiris.canvas",
            Title = "画布",
            CanClose = false,
            CanFloat = false,
            CanPin = false,
            CanDrag = true,
        };
        _dockables[canvas.Id] = canvas;

        var documentDock = new DocumentDock
        {
            Id = "osiris.documents",
            IsCollapsable = false,
            CanCreateDocument = false,
            EnableWindowDrag = true,
            VisibleDockables = CreateList<IDockable>(canvas),
        };
        _documentDock = documentDock;

        // 工具停靠区：左侧 / 右侧 / 底部（模块面板经 AddToolPanel 动态加入）
        _leftDock = new ToolDock
        {
            Id = "osiris.left",
            Alignment = Alignment.Left,
            Proportion = 0.18,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>(),
        };
        _rightDock = new ToolDock
        {
            Id = "osiris.right",
            Alignment = Alignment.Right,
            Proportion = 0.2,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>(),
        };
        _bottomDock = new ToolDock
        {
            Id = "osiris.bottom",
            Alignment = Alignment.Bottom,
            Proportion = 0.25,
            GripMode = GripMode.Visible,
            VisibleDockables = CreateList<IDockable>(),
        };

        // 横向：左 | 文档 | 右
        var mainHorizontal = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(
                _leftDock, new ProportionalDockSplitter(), documentDock, new ProportionalDockSplitter(), _rightDock),
        };

        // 纵向：主区 | 底部
        var mainLayout = new ProportionalDock
        {
            Orientation = Orientation.Vertical,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(mainHorizontal, new ProportionalDockSplitter(), _bottomDock),
        };

        var rootDock = CreateRootDock();
        rootDock.IsCollapsable = false;
        rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);
        rootDock.ActiveDockable = mainLayout;
        rootDock.DefaultDockable = mainLayout;
        return rootDock;
    }

    /// <summary>初始化布局：注册定位器（Dock 按 Id 查找布局对象与窗口宿主）。</summary>
    public override void InitLayout(IDockable layout)
    {
        DockableLocator = new Dictionary<string, Func<IDockable?>>
        {
            ["Root"] = () => _rootDock,
            ["osiris.documents"] = () => _documentDock,
        };
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => DockSettings.UseManagedWindows ? new ManagedHostWindow() : new HostWindow(),
        };
        base.InitLayout(layout);
    }

    /// <summary>把模块贡献的画布控件设为中央文档内容（Rebuild 时调用）。</summary>
    public void SetCanvasContext(object? content)
    {
        if (_dockables.TryGetValue("osiris.canvas", out var dockable) && dockable is Document doc)
            doc.Context = content;
    }

    /// <summary>添加模块面板（工具窗口）到对应停靠区；重复 Id 忽略（面板生命周期归模块）。</summary>
    public void AddToolPanel(string id, string title, object? content, DockSide side)
    {
        if (_dockables.ContainsKey(id))
            return; // 面板由模块生命周期管理，重复注册忽略

        var tool = new Tool
        {
            Id = id,
            Title = title,
            Context = content,
            CanClose = false,
            CanFloat = true,
            CanPin = false,
            CanDrag = true,
        };
        _dockables[id] = tool;
        ToolDock? target = side switch
        {
            DockSide.Left => _leftDock,
            DockSide.Bottom => _bottomDock,
            _ => _rightDock,
        };
        if (target is null)
            return;
        AddDockable(target, tool); // 加入布局并激活
        SetActiveDockable(tool);
    }

    /// <summary>打开壳级工具窗口（模块管理/设置）：作为可停靠工具加入右侧区并激活；可拖拽浮动。</summary>
    public void ShowToolWindow(string id, string title, object content)
    {
        // 已打开且仍在布局中 → 激活聚焦；已被用户点 Dock X 或 CloseToolWindow 移除 → 重建新窗口
        if (_dockables.TryGetValue(id, out var existing)
            && existing.Owner is IDock owner
            && owner.VisibleDockables?.Contains(existing) == true)
        {
            SetActiveDockable(existing);
            SetFocusedDockable(owner, existing);
            return;
        }
        if (existing is not null)
            _dockables.Remove(id);
        ShowToolWindowCore(id, title, content);
    }

    /// <summary>
    /// 打开壳级工具窗口（视图工厂版）：Context 传 PanelContentFactory（纯数据含工厂委托），
    /// Dock 模板经 LazyViewHost 在每次浮动/停靠重建内容时生成**新**视图实例——规避控件双父级崩溃。
    /// 壳级窗口（模块管理等）与模块面板统一走此模式。
    /// </summary>
    public void ShowToolWindow(string id, string title, Func<object> viewFactory)
        => ShowToolWindow(id, title, new PanelContentFactory(viewFactory));

    /// <summary>工具窗口核心：创建 Tool 并加入右侧停靠区。</summary>
    private void ShowToolWindowCore(string id, string title, object context)
    {
        var tool = new Tool
        {
            Id = id,
            Title = title,
            Context = context,
            CanClose = true,
            CanFloat = true,
            CanPin = false,
            CanDrag = true,
        };
        _dockables[id] = tool;
        if (_rightDock is null)
            return;
        AddDockable(_rightDock, tool);
        SetActiveDockable(tool);
    }

    /// <summary>关闭工具窗口：从布局移除（Dock 移除后对象可回收）。
    /// collapse=false：避免空停靠区被 Dock 折叠移除出布局（否则重开时 AddDockable 挂到孤儿 dock）。</summary>
    public void CloseToolWindow(string id)
    {
        if (!_dockables.TryGetValue(id, out var tool))
            return;
        if (tool.Owner is IDock parent)
            RemoveDockable(tool, collapse: false);
        _dockables.Remove(id);
    }
}
