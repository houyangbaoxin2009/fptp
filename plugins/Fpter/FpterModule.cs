using Osiris.Abstractions;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;

namespace Fpter;

/// <summary>
/// 内置滤镜模块（原 Fptp.Plugins.Builtin 改名而来，id=fpter）：
/// 只提供滤镜（灰度 / 动漫），作为 IFilterPlugin 贡献给滤镜窗口与壳体菜单。
/// 换底色 / 智能裁切 / 排版已迁移到 fptm 工作流模块，本模块不再承担。
/// ABI 红线：仅引用 Osiris.Abstractions，不引用 SkiaSharp/Avalonia/Osiris.Core。
/// </summary>
[PluginExport]
public sealed class FpterModule : IFilterPlugin
{
    /// <summary>模块 Id（module.json 与注册表一致）。</summary>
    public const string ModuleId = "fpter";

    /// <summary>模块显示名。</summary>
    public const string ModuleName = "内置滤镜模块";

    /// <summary>模块版本（与 module.json 一致）。</summary>
    public const string ModuleVersion = "1.0.0";

    /// <summary>要求的最低宿主版本。</summary>
    public const string HostVersion = "1.0.0";

    /// <summary>宿主上下文（滤镜窗口视图经此访问服务）。静态供模块内视图访问。</summary>
    public static IHostContext? HostContext { get; private set; }

    // ---- 滤镜实例（命令与 Filters 列表共享同一实例）----
    private readonly GrayscaleFilter _grayscale = new();
    private readonly AnimeFilter _anime = new();

    /// <inheritdoc />
    public string Id => ModuleId;

    /// <inheritdoc />
    public string Name => L10n.T(ModuleName);

    /// <inheritdoc />
    public string Version => ModuleVersion;

    /// <inheritdoc />
    public string MinHostVersion => HostVersion;

    /// <inheritdoc />
    public ModuleKind Kind => ModuleKind.Extension;

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies => [];

    /// <inheritdoc />
    public IReadOnlyList<IFilterProcessor> Filters =>
    [
        _grayscale,
        _anime,
    ];

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        HostContext = host;

        // 无 UI 宿主（CLI/测试）：仅贡献滤镜，跳过命令/面板注册
        if (host.Ui is not { } ui)
            return;

        // 滤镜窗口（可停靠）：聚合全部 IFilterPlugin 的滤镜（含本模块灰度/动漫）
        ui.AddPanel("滤镜", () => new Views.FilterWindowView(), DockSide.Right);

        // 图像菜单：2 个滤镜命令（宿主自动创建中间节点）
        ui.RegisterCommand(new FilterCommand(host, _grayscale, "fpter.grayscale", "灰度"));
        ui.AddMenu("图像/灰度", "fpter.grayscale", 10);

        ui.RegisterCommand(new FilterCommand(host, _anime, "fpter.anime", "动漫模式"));
        ui.AddMenu("图像/动漫模式", "fpter.anime", 12);
    }
}