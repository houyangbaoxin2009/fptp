using Osiris.Abstractions;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace Itool;

/// <summary>
/// 画笔工具模块（itool）：从 fptm 拆出的工具 + 画笔部分。
/// 提供 9 个编辑工具（选取/套索/智能框选/滴管/铅笔/钢笔/毛笔/刷子/颜料桶）、
/// 画笔窗口（工具切换/颜色/大小/颜料盘/预设）、颜料盘槽位与预设槽位命令。
/// 模块只引用 Abstractions（ABI 红线），文档操作经 IDocumentService 契约。
/// </summary>
[PluginExport]
public sealed class ItoolModule : IModule, ITool, ISettingProvider
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
    public string Id => "itool";

    /// <inheritdoc />
    public string Name => L10n.T("画笔工具模块");

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>9 个编辑工具（画笔窗口经此获取列表并切换）。</summary>
    public IReadOnlyList<IEditorTool> Tools =>
    [
        _selectRect, _lasso, _magicWand, _eyedropper,
        _pencil, _pen, _inkBrush, _brush, _bucket,
    ];

    /// <summary>设置组：编辑工具（各画笔工具颜色/大小）+ 颜料盘（9 槽）+ 快捷键。设置窗口左侧导航显示。</summary>
    public IReadOnlyList<SettingGroup> Groups { get; } = BuildGroups();

    /// <summary>构建设置组（与 ToolState 默认值对齐；GroupId=模块 Id 供注册表回退默认值）。</summary>
    private static IReadOnlyList<SettingGroup> BuildGroups()
    {
        var tools = new List<SettingItem>
        {
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("pencil")) { GroupId = "itool", Key = "pencilColor", Label = L10n.T("铅笔颜色"), Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("pen")) { GroupId = "itool", Key = "penColor", Label = L10n.T("钢笔颜色"), Scope = SettingScope.User },
            new NumberSettingItem(Editing.ToolState.Instance.GetSize("pen"), 1, 10, 1) { GroupId = "itool", Key = "penSize", Label = L10n.T("钢笔大小"), Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("inkBrush")) { GroupId = "itool", Key = "inkBrushColor", Label = L10n.T("毛笔颜色"), Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("brush")) { GroupId = "itool", Key = "brushColor", Label = L10n.T("刷子颜色"), Scope = SettingScope.User },
            new NumberSettingItem(Editing.ToolState.Instance.GetSize("brush"), 1, 50, 1) { GroupId = "itool", Key = "brushSize", Label = L10n.T("刷子大小"), Scope = SettingScope.User },
            new ColorSettingItem(Editing.ToolState.Instance.GetColor("bucket")) { GroupId = "itool", Key = "bucketColor", Label = L10n.T("颜料桶颜色"), Scope = SettingScope.User },
        };

        var palette = new List<SettingItem>();
        for (int i = 1; i <= 9; i++)
            palette.Add(new ColorSettingItem(Editing.ToolState.Instance.GetSlot(i - 1))
            {
                GroupId = "itool",
                Key = $"slot{i}",
                Label = L10n.T("颜料槽 {0}", i),
                Scope = SettingScope.User,
            });

        // 快捷键组（User 级，存于注册表）：键=命令 Id，值=快捷键文本（壳 KeyDown 路由解析执行）。
        // 默认：颜料盘槽位（Ctrl+A+1..9）+ 预设槽位（Ctrl+B+1..9）。
        var hotkeys = new List<SettingItem>
        {
            Hotkey("itool.palette1", L10n.T("颜料槽 1"), "Ctrl+A+1"),
            Hotkey("itool.palette2", L10n.T("颜料槽 2"), "Ctrl+A+2"),
            Hotkey("itool.palette3", L10n.T("颜料槽 3"), "Ctrl+A+3"),
            Hotkey("itool.palette4", L10n.T("颜料槽 4"), "Ctrl+A+4"),
            Hotkey("itool.palette5", L10n.T("颜料槽 5"), "Ctrl+A+5"),
            Hotkey("itool.palette6", L10n.T("颜料槽 6"), "Ctrl+A+6"),
            Hotkey("itool.palette7", L10n.T("颜料槽 7"), "Ctrl+A+7"),
            Hotkey("itool.palette8", L10n.T("颜料槽 8"), "Ctrl+A+8"),
            Hotkey("itool.palette9", L10n.T("颜料槽 9"), "Ctrl+A+9"),
            Hotkey("itool.preset1", L10n.T("预设 1"), "Ctrl+B+1"),
            Hotkey("itool.preset2", L10n.T("预设 2"), "Ctrl+B+2"),
            Hotkey("itool.preset3", L10n.T("预设 3"), "Ctrl+B+3"),
            Hotkey("itool.preset4", L10n.T("预设 4"), "Ctrl+B+4"),
            Hotkey("itool.preset5", L10n.T("预设 5"), "Ctrl+B+5"),
            Hotkey("itool.preset6", L10n.T("预设 6"), "Ctrl+B+6"),
            Hotkey("itool.preset7", L10n.T("预设 7"), "Ctrl+B+7"),
            Hotkey("itool.preset8", L10n.T("预设 8"), "Ctrl+B+8"),
            Hotkey("itool.preset9", L10n.T("预设 9"), "Ctrl+B+9"),
        };

        return
        [
            new SettingGroup { Id = "itool.tools", DisplayName = L10n.T("编辑工具"), Items = tools },
            new SettingGroup { Id = "itool.palette", DisplayName = L10n.T("颜料盘"), Items = palette },
            new SettingGroup { Id = "itool.hotkeys", DisplayName = L10n.T("快捷键"), Items = hotkeys },
        ];
    }

    /// <summary>快捷键设置项（TextSettingItem：键=命令 Id，值=快捷键文本）。</summary>
    private static TextSettingItem Hotkey(string key, string label, string defaultValue) => new(defaultValue)
    {
        GroupId = "itool",
        Key = key,
        Label = label,
        Scope = SettingScope.User,
    };

    /// <summary>宿主上下文（画笔窗口视图经此访问服务：工具激活/注册表/命令）。静态供模块内视图访问。</summary>
    public static IHostContext? HostContext { get; private set; }

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        HostContext = host;

        // 注入宿主到全部工具（工具不独立走插件加载器，由宿主模块统一提供上下文）
        foreach (IEditorTool tool in Tools)
            tool.Initialize(host);

        // 从注册表恢复工具状态与颜料盘（User 级配置，键前缀 itool）
        IModuleRegistry? registry = host.Services.Get<IModuleRegistry>();
        if (registry is not null)
            Editing.ToolState.Instance.Load(registry);

        // 注册颜料盘槽位/预设槽位命令 + 画笔窗口面板（视图工厂：每次 Dock 浮动重建新实例，防双父级）
        if (host.Ui is { } ui)
        {
            // 颜料盘槽位命令（壳快捷键路由 Ctrl+A+1..9 执行：应用槽位色到当前画笔工具）
            for (int i = 0; i < Editing.ToolState.Instance.Slots.Length; i++)
                ui.RegisterCommand(new Commands.PaletteSlotCommand(i));
            // 预设槽位命令（壳快捷键路由 Ctrl+B+1..9 执行：应用整套画笔颜色预设）
            for (int i = 0; i < Editing.ToolState.PresetCount; i++)
                ui.RegisterCommand(new Commands.PresetSlotCommand(i));

            // 编辑命令：裁切到选区（按当前选区包围盒裁切画布，可撤销）
            ui.RegisterCommand(new Commands.CropToSelectionCommand(host));
            ui.AddMenu("图像/裁切到选区", "itool.cropToSelection", 26);

            ui.AddPanel("画笔", () => new Views.BrushWindowView(), DockSide.Right);
        }
    }
}
