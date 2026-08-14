using Osiris.Abstractions;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace FpEdit;

/// <summary>
/// 编辑模块（fpedit）：从 fptm 拆出的编辑命令 + 操作面板部分。
/// 提供编辑命令（复制/粘贴/撤销/重做）、操作窗口面板与像素剪贴板。
/// 模块只引用 Abstractions（ABI 红线），文档操作经 IDocumentService 契约。
/// </summary>
[PluginExport]
public sealed class FpEditModule : IModule, ISettingProvider
{
    /// <inheritdoc />
    public string Id => "fpedit";

    /// <inheritdoc />
    public string Name => L10n.T("编辑模块");

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>设置组：快捷键（编辑命令）。设置窗口左侧导航显示一项。</summary>
    public IReadOnlyList<SettingGroup> Groups { get; } = BuildGroups();

    /// <summary>构建设置组（GroupId=模块 Id 供注册表回退默认值）。</summary>
    private static IReadOnlyList<SettingGroup> BuildGroups()
    {
        // 快捷键组（User 级，存于注册表）：键=命令 Id，值=快捷键文本（壳 KeyDown 路由解析执行）。
        // 默认：复制/粘贴/撤销/重做。
        var hotkeys = new List<SettingItem>
        {
            Hotkey("fpedit.copy", L10n.T("复制"), "Ctrl+C"),
            Hotkey("fpedit.paste", L10n.T("粘贴"), "Ctrl+V"),
            Hotkey("fpedit.undo", L10n.T("撤销"), "Ctrl+Z"),
            Hotkey("fpedit.redo", L10n.T("重做"), "Ctrl+Y"),
        };

        return
        [
            new SettingGroup { Id = "fpedit.hotkeys", DisplayName = L10n.T("快捷键"), Items = hotkeys },
        ];
    }

    /// <summary>快捷键设置项（TextSettingItem：键=命令 Id，值=快捷键文本）。</summary>
    private static TextSettingItem Hotkey(string key, string label, string defaultValue) => new(defaultValue)
    {
        GroupId = "fpedit",
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

        // 注册编辑命令 + 操作窗口面板（视图工厂——每次 Dock 浮动重建新实例，防双父级）
        if (host.Ui is { } ui)
        {
            ui.RegisterCommand(new Commands.CopyCommand(host));
            ui.RegisterCommand(new Commands.PasteCommand(host));
            ui.RegisterCommand(new Commands.UndoCommand(host));
            ui.RegisterCommand(new Commands.RedoCommand(host));

            ui.AddPanel("操作", () => new Views.OperationWindowView(), DockSide.Right);
        }
    }
}
