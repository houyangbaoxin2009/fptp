using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace Fptm;

/// <summary>
/// 传统编辑模块（fptm）：老版 FPTP 功能集合（除滤镜）。
/// 提供 9 个编辑工具（选取/套索/智能框选/滴管/铅笔/钢笔/毛笔/刷子/颜料桶）、
/// 编辑命令（复制/粘贴/撤销/重做）、工具状态与颜料盘设置组。
/// 模块只引用 Abstractions（ABI 红线），文档操作经 IDocumentService 契约。
/// </summary>
[PluginExport]
public sealed class FptmModule : IModule, IToolPlugin, ISettingProvider
{
    private readonly Tools.PencilTool _pencil = new();
    private readonly Tools.PenTool _pen = new();
    private readonly Tools.InkBrushTool _inkBrush = new();
    private readonly Tools.BrushTool _brush = new();
    private readonly Tools.SelectRectTool _selectRect = new();
    private readonly Tools.LassoTool _lasso = new();
    private readonly Tools.MagicWandTool _magicWand = new();
    private readonly Tools.EyedropperTool _eyedropper = new();
    private readonly Tools.PaintBucketTool _bucket = new();

    /// <inheritdoc />
    public string Id => "fptm";

    /// <inheritdoc />
    public string Name => "传统编辑模块";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>9 个编辑工具（操作窗口/画笔窗口经此获取列表并切换）。</summary>
    public IReadOnlyList<IEditorTool> Tools =>
    [
        _selectRect, _lasso, _magicWand, _eyedropper,
        _pencil, _pen, _inkBrush, _brush, _bucket,
    ];

    /// <summary>设置组：编辑工具（各画笔工具颜色/大小）+ 颜料盘（9 槽）。设置窗口左侧导航显示两项。</summary>
    public IReadOnlyList<SettingGroup> Groups { get; } = BuildGroups();

    /// <summary>构建设置组（与 ToolState 默认值对齐；GroupId=模块 Id 供注册表回退默认值）。</summary>
    private static IReadOnlyList<SettingGroup> BuildGroups()
    {
        var tools = new List<SettingItem>
        {
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("pencil")) { GroupId = "fptm", Key = "pencilColor", Label = "铅笔颜色", Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("pen")) { GroupId = "fptm", Key = "penColor", Label = "钢笔颜色", Scope = SettingScope.User },
            new NumberSettingItem(Editing.ToolState.Instance.GetSize("pen"), 1, 10, 1) { GroupId = "fptm", Key = "penSize", Label = "钢笔大小", Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("inkBrush")) { GroupId = "fptm", Key = "inkBrushColor", Label = "毛笔颜色", Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("brush")) { GroupId = "fptm", Key = "brushColor", Label = "刷子颜色", Scope = SettingScope.User },
            new NumberSettingItem(Editing.ToolState.Instance.GetSize("brush"), 1, 50, 1) { GroupId = "fptm", Key = "brushSize", Label = "刷子大小", Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("bucket")) { GroupId = "fptm", Key = "bucketColor", Label = "颜料桶颜色", Scope = SettingScope.User },
        };

        var palette = new List<SettingItem>();
        for (int i = 1; i <= 9; i++)
            palette.Add(new ColorSettingItem(Editing.ToolState.Instance.GetSlot(i - 1))
            {
                GroupId = "fptm",
                Key = $"slot{i}",
                Label = $"颜料槽 {i}",
                Scope = SettingScope.User,
            });

        // 快捷键组（User 级，存于注册表）：键=命令 Id，值=快捷键文本（壳 KeyDown 路由解析执行）。
        // 默认：编辑操作 + 工具切换 + 颜料盘槽位（Ctrl+A+1..9）。
        var hotkeys = new List<SettingItem>
        {
            Hotkey("fptm.copy", "复制", "Ctrl+C"),
            Hotkey("fptm.paste", "粘贴", "Ctrl+V"),
            Hotkey("fptm.undo", "撤销", "Ctrl+Z"),
            Hotkey("fptm.redo", "重做", "Ctrl+Y"),
            Hotkey("fptm.palette1", "颜料槽 1", "Ctrl+A+1"),
            Hotkey("fptm.palette2", "颜料槽 2", "Ctrl+A+2"),
            Hotkey("fptm.palette3", "颜料槽 3", "Ctrl+A+3"),
            Hotkey("fptm.palette4", "颜料槽 4", "Ctrl+A+4"),
            Hotkey("fptm.palette5", "颜料槽 5", "Ctrl+A+5"),
            Hotkey("fptm.palette6", "颜料槽 6", "Ctrl+A+6"),
            Hotkey("fptm.palette7", "颜料槽 7", "Ctrl+A+7"),
            Hotkey("fptm.palette8", "颜料槽 8", "Ctrl+A+8"),
            Hotkey("fptm.palette9", "颜料槽 9", "Ctrl+A+9"),
            Hotkey("fptm.preset1", "预设 1", "Ctrl+B+1"),
            Hotkey("fptm.preset2", "预设 2", "Ctrl+B+2"),
            Hotkey("fptm.preset3", "预设 3", "Ctrl+B+3"),
            Hotkey("fptm.preset4", "预设 4", "Ctrl+B+4"),
            Hotkey("fptm.preset5", "预设 5", "Ctrl+B+5"),
            Hotkey("fptm.preset6", "预设 6", "Ctrl+B+6"),
            Hotkey("fptm.preset7", "预设 7", "Ctrl+B+7"),
            Hotkey("fptm.preset8", "预设 8", "Ctrl+B+8"),
            Hotkey("fptm.preset9", "预设 9", "Ctrl+B+9"),
        };

        return
        [
            new SettingGroup { Id = "fptm.tools", DisplayName = "编辑工具", Items = tools },
            new SettingGroup { Id = "fptm.palette", DisplayName = "颜料盘", Items = palette },
            new SettingGroup { Id = "fptm.hotkeys", DisplayName = "快捷键", Items = hotkeys },
        ];
    }

    /// <summary>快捷键设置项（TextSettingItem：键=命令 Id，值=快捷键文本）。</summary>
    private static TextSettingItem Hotkey(string key, string label, string defaultValue) => new(defaultValue)
    {
        GroupId = "fptm",
        Key = key,
        Label = label,
        Scope = SettingScope.User,
    };

    /// <summary>宿主上下文（窗口视图经此访问服务：工具激活/注册表/命令）。静态供模块内视图访问。</summary>
    public static IHostContext? HostContext { get; private set; }

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        HostContext = host;

        // 注入宿主到全部工具（工具不独立走插件加载器，由宿主模块统一提供上下文）
        foreach (IEditorTool tool in Tools)
            tool.Initialize(host);

        // 从注册表恢复工具状态与颜料盘（User 级配置）
        IModuleRegistry? registry = host.Services.Get<IModuleRegistry>();
        if (registry is not null)
            Editing.ToolState.Instance.Load(registry);

        // 注册编辑命令 + 工具窗口面板（操作窗口/画笔窗口：视图工厂——每次 Dock 浮动重建新实例，防双父级）
        if (host.Ui is { } ui)
        {
            ui.RegisterCommand(new Commands.CopyCommand(host));
            ui.RegisterCommand(new Commands.PasteCommand(host));
            ui.RegisterCommand(new Commands.UndoCommand(host));
            ui.RegisterCommand(new Commands.RedoCommand(host));

            // 颜料盘槽位命令（壳快捷键路由 Ctrl+A+1..9 执行：应用槽位色到当前画笔工具）
            for (int i = 0; i < Editing.ToolState.Instance.Slots.Length; i++)
                ui.RegisterCommand(new Commands.PaletteSlotCommand(i));
            // 预设槽位命令（壳快捷键路由 Ctrl+B+1..9 执行：应用整套画笔颜色预设）
            for (int i = 0; i < Editing.ToolState.PresetCount; i++)
                ui.RegisterCommand(new Commands.PresetSlotCommand(i));

            ui.AddPanel("操作", () => new Views.OperationWindowView(), DockSide.Right);
            ui.AddPanel("画笔", () => new Views.BrushWindowView(), DockSide.Right);
            ui.AddPanel("传统", () => new Views.TraditionalPanelView(), DockSide.Right);
        }
    }
}


