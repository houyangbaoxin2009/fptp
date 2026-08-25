using Osiris.Abstractions;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Ui;

namespace Fpter;

/// <summary>
/// 内置滤镜模块（原 Fptp.Plugins.Builtin 改名而来，id=fpter）：
/// 提供滤镜（灰度 / 动漫 / 反色 / 亮度对比度 / 饱和度 / 模糊 / 锐化 / 怀旧），
/// 作为 IFilterPlugin 贡献给滤镜窗口与壳体菜单。
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
    private readonly InvertFilter _invert = new();
    private readonly BrightnessContrastFilter _brightnessContrast = new();
    private readonly SaturationFilter _saturation = new();
    private readonly BlurFilter _blur = new();
    private readonly SharpenFilter _sharpen = new();
    private readonly SepiaFilter _sepia = new();

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
        _invert,
        _brightnessContrast,
        _saturation,
        _blur,
        _sharpen,
        _sepia,
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

        // 常用滤镜：反色 / 亮度对比度 / 饱和度 / 模糊 / 锐化 / 怀旧（滤镜窗口自动聚合，无需在此登记）
        ui.RegisterCommand(new FilterCommand(host, _invert, "fpter.invert", "反色"));
        ui.AddMenu("图像/反色", "fpter.invert", 14);

        ui.RegisterCommand(new FilterCommand(host, _brightnessContrast, "fpter.brightnessContrast", "亮度对比度"));
        ui.AddMenu("图像/亮度对比度", "fpter.brightnessContrast", 16);

        ui.RegisterCommand(new FilterCommand(host, _saturation, "fpter.saturation", "饱和度"));
        ui.AddMenu("图像/饱和度", "fpter.saturation", 18);

        ui.RegisterCommand(new FilterCommand(host, _blur, "fpter.blur", "模糊"));
        ui.AddMenu("图像/模糊", "fpter.blur", 20);

        ui.RegisterCommand(new FilterCommand(host, _sharpen, "fpter.sharpen", "锐化"));
        ui.AddMenu("图像/锐化", "fpter.sharpen", 22);

        ui.RegisterCommand(new FilterCommand(host, _sepia, "fpter.sepia", "怀旧"));
        ui.AddMenu("图像/怀旧", "fpter.sepia", 24);
    }
}