using Osiris.Abstractions;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace Fptm;

/// <summary>
/// 传统编辑模块（fptm）：老版 FPTP 的传统批处理 + 证件照工作流（换底色 / 智能裁切 / 排版）。
/// 原 fptm 拆分后仅保留传统面板与工作流；编辑工具与画笔已拆到 itool 模块，编辑命令已拆到 fpedit 模块。
/// 证件照工作流：换底色 / 智能裁切 / 排版命令 + 设置组（ISettingProvider），算法在 Workflow 目录，
/// 仅 fptm 本地使用，不注册为滤镜（不出现在滤镜窗口）。
/// 模块只引用 Abstractions（ABI 红线），文档操作经 IDocumentService 契约。
/// </summary>
[PluginExport]
public sealed class FptmModule : IModule, ISettingProvider
{
    /// <summary>模块 Id（module.json 与注册表一致）。</summary>
    public const string ModuleId = "fptm";

    /// <inheritdoc />
    public string Id => ModuleId;

    /// <inheritdoc />
    public string Name => L10n.T("传统编辑模块");

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public string MinHostVersion => "1.0.0";

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <summary>宿主上下文（传统面板视图经此访问服务）。静态供模块内视图访问。</summary>
    public static IHostContext? HostContext { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<SettingGroup> Groups => [Settings.Group];

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        HostContext = host;

        // 注册传统面板（批量处理）+ 工作流命令与设置
        if (host.Ui is { } ui)
        {
            ui.AddPanel("传统", () => new Views.TraditionalPanelView(), DockSide.Right);
            WorkflowCommands.Register(host);
        }
    }
}
