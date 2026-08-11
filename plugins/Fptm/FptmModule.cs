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
    public string Version => "2.1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "2.1.0.0";

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

        return
        [
            new SettingGroup { Id = "fptm.tools", DisplayName = "编辑工具", Items = tools },
            new SettingGroup { Id = "fptm.palette", DisplayName = "颜料盘", Items = palette },
        ];
    }

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);

        // 注入宿主到全部工具（工具不独立走插件加载器，由宿主模块统一提供上下文）
        foreach (IEditorTool tool in Tools)
            tool.Initialize(host);

        // 从注册表恢复工具状态与颜料盘（User 级配置）
        IModuleRegistry? registry = host.Services.Get<IModuleRegistry>();
        if (registry is not null)
            Editing.ToolState.Instance.Load(registry);

        // 注册编辑命令（操作窗口按钮经命令 Id 触发；不挂菜单——由窗口 UI 呈现）
        if (host.Ui is { } ui)
        {
            ui.RegisterCommand(new Commands.CopyCommand(host));
            ui.RegisterCommand(new Commands.PasteCommand(host));
            ui.RegisterCommand(new Commands.UndoCommand(host));
            ui.RegisterCommand(new Commands.RedoCommand(host));
        }
    }
}
