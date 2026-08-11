using Osiris.Abstractions;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 内置证件照扩展模块（插件包）：向宿主贡献 4 个证件照滤镜 + 1 个设置组 + 命令菜单。
/// - 滤镜：灰度 / 换底色 / 智能裁切 / 动漫模式（IFilterProcessor，纯 PixelSurface 像素循环）；
/// - 设置：证件照设置组（换底色/动漫参数/排版相纸），经 ISettingProvider 即时 JSON 持久化；
/// - 命令：图像菜单 4 个滤镜命令 + 文件菜单排版输出命令（经 IUiService 注册，无 UI 宿主跳过）。
/// ABI 红线：仅引用 Osiris.Abstractions，不引用 SkiaSharp/Avalonia/Osiris.Core。
/// </summary>
[PluginExport]
public sealed class BuiltinPlugin : IFilterPlugin, ISettingProvider
{
    /// <summary>模块 Id（module.json 与注册表一致）。</summary>
    public const string ModuleId = "fptp.idphoto";

    /// <summary>模块显示名。</summary>
    public const string ModuleName = "证件照扩展模块";

    /// <summary>模块版本（与 module.json 一致）。</summary>
    public const string ModuleVersion = "1.0.0";

    /// <summary>要求的最低宿主版本。</summary>
    public const string HostVersion = "1.0.0";

    /// <summary>滤镜参数键 → 设置项键映射（命令组装参数时，把设置面板当前值写入滤镜参数包）。</summary>
    private static readonly (string ParamKey, string SettingKey)[] s_paramToSetting =
    [
        (ReplaceBackgroundFilter.ParamColor, "replaceBgColor"),
        (ReplaceBackgroundFilter.ParamTolerance, "replaceBgTolerance"),
        (AnimeFilter.ParamLevels, "animeLevels"),
        (AnimeFilter.ParamOutline, "animeOutline"),
    ];

    // ---- 滤镜实例（命令与 Filters 列表共享同一实例）----
    private readonly GrayscaleFilter _grayscale = new();
    private readonly ReplaceBackgroundFilter _replaceBackground = new();
    private readonly SmartCropFilter _smartCrop = new();
    private readonly AnimeFilter _anime = new();

    // ---- 设置项实例（宿主设置面板读写 Value 即 JSON 即时持久化；命令经此读当前值）----
    private readonly ColorSettingItem _replaceBgColor = new(ReplaceBackgroundFilter.DefaultColor)
    {
        GroupId = ModuleId,
        Key = "replaceBgColor",
        Label = "换底色",
        Description = "换底色滤镜使用的目标背景颜色",
        Scope = SettingScope.User,
    };

    private readonly NumberSettingItem _replaceBgTolerance = new(60, 10, 200, 5)
    {
        GroupId = ModuleId,
        Key = "replaceBgTolerance",
        Label = "换底容差",
        Description = "换底色滤镜的背景判定容差（越大替换范围越广）",
        Scope = SettingScope.User,
    };

    private readonly NumberSettingItem _animeLevels = new(8, 2, 16, 1)
    {
        GroupId = ModuleId,
        Key = "animeLevels",
        Label = "动漫色彩层次",
        Description = "动漫模式每通道量化级数（越大色彩层次越丰富）",
        Scope = SettingScope.User,
    };

    private readonly NumberSettingItem _animeOutline = new(60, 0, 200, 5)
    {
        GroupId = ModuleId,
        Key = "animeOutline",
        Label = "动漫描边强度",
        Description = "动漫模式边缘描边强度（越大描边越少）",
        Scope = SettingScope.User,
    };

    private readonly ChoiceSettingItem _layoutPaper = new(["5寸", "6寸", "A4"], "5寸")
    {
        GroupId = ModuleId,
        Key = "layoutPaper",
        Label = "排版相纸",
        Description = "排版输出命令使用的相纸规格",
        Scope = SettingScope.User,
    };

    // 宿主上下文（Initialize 注入；无 UI 宿主下仅贡献滤镜/设置）
    private IHostContext? _host;

    // 模块注册表服务（CoreModule 注册后经 Services 获取；null 表示未注册，跳过模块配置读取）
    private IModuleRegistry? _registry;

    /// <inheritdoc />
    public string Id => ModuleId;

    /// <inheritdoc />
    public string Name => ModuleName;

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
        _replaceBackground,
        _smartCrop,
        _anime,
    ];

    /// <inheritdoc />
    public IReadOnlyList<SettingGroup> Groups =>
    [
        new SettingGroup
        {
            Id = ModuleId,
            DisplayName = "证件照",
            Items =
            [
                _replaceBgColor,
                _replaceBgTolerance,
                _animeLevels,
                _animeOutline,
                _layoutPaper,
            ],
        },
    ];

    /// <summary>当前排版相纸名（布局命令读取）。</summary>
    public string LayoutPaperName => _layoutPaper.Value;

    /// <inheritdoc />
    public void Initialize(IHostContext host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _host = host;
        _registry = host.Services.Get<IModuleRegistry>();

        // 无 UI 宿主（CLI/测试）：仅贡献滤镜与设置，跳过命令菜单注册
        if (host.Ui is not { } ui)
            return;

        // 图像菜单：4 个滤镜命令（菜单路径如 "图像/灰度"，host 自动创建中间节点）
        ui.RegisterCommand(new FilterCommand(host, this, _grayscale, "fptp.grayscale", "灰度"));
        ui.AddMenu("图像/灰度", "fptp.grayscale", 10);

        ui.RegisterCommand(new FilterCommand(host, this, _replaceBackground, "fptp.replaceBackground", "换底色"));
        ui.AddMenu("图像/换底色", "fptp.replaceBackground", 11);

        ui.RegisterCommand(new FilterCommand(host, this, _anime, "fptp.anime", "动漫模式"));
        ui.AddMenu("图像/动漫模式", "fptp.anime", 12);

        ui.RegisterCommand(new FilterCommand(host, this, _smartCrop, "fptp.smartCrop", "智能裁切"));
        ui.AddMenu("图像/智能裁切", "fptp.smartCrop", 13);

        // 文件菜单：排版输出（相纸规格来自设置项 layoutPaper）
        ui.RegisterCommand(new LayoutCommand(host, this, "fptp.layout", "排版输出"));
        ui.AddMenu("文件/排版输出", "fptp.layout", 20);
    }

    /// <summary>
    /// 组装滤镜执行参数包（三级回退）：
    /// 1) 滤镜 Defaults 打底；2) 用户设置项当前值覆盖（设置面板即时持久化的值）；
    /// 3) 模块级配置覆盖（经 IModuleRegistry，宿主 Core 模块注册后生效，未注册则跳过）。
    /// </summary>
    public FilterParameters BuildParameters(IFilterProcessor filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // 1. Defaults 打底
        var parameters = new FilterParameters();
        foreach (string key in filter.Defaults.Keys)
            parameters[key] = filter.Defaults[key];

        // 2. 设置项覆盖（参数键 → 设置项当前值）
        foreach ((string paramKey, string settingKey) in s_paramToSetting)
        {
            SettingItem? item = FindSetting(settingKey);
            if (item is null)
                continue;
            object? value = ConvertSettingToParam(paramKey, item);
            if (value is not null)
                parameters[paramKey] = value;
        }

        // 3. 模块级配置覆盖（数值统一按 double 读取——SetConfig 归一化为 double 持久化）
        if (_registry is { } registry)
        {
            foreach ((string paramKey, _) in s_paramToSetting)
            {
                double? config = registry.GetConfig<double>(ModuleId, paramKey);
                if (config is null)
                    continue;
                parameters[paramKey] = paramKey == ReplaceBackgroundFilter.ParamColor
                    ? (uint)Math.Clamp(config.Value, 0, uint.MaxValue)
                    : (int)Math.Round(config.Value);
            }
        }

        return parameters;
    }

    /// <summary>按键查设置项实例。</summary>
    private SettingItem? FindSetting(string settingKey) => settingKey switch
    {
        "replaceBgColor" => _replaceBgColor,
        "replaceBgTolerance" => _replaceBgTolerance,
        "animeLevels" => _animeLevels,
        "animeOutline" => _animeOutline,
        "layoutPaper" => _layoutPaper,
        _ => null,
    };

    /// <summary>设置项当前值 → 滤镜参数值（数值项 double → int；颜色项原样 uint）。</summary>
    private static object? ConvertSettingToParam(string paramKey, SettingItem item) => paramKey switch
    {
        ReplaceBackgroundFilter.ParamColor => ((ColorSettingItem)item).Value,
        ReplaceBackgroundFilter.ParamTolerance => (int)Math.Round(((NumberSettingItem)item).Value),
        AnimeFilter.ParamLevels => (int)Math.Round(((NumberSettingItem)item).Value),
        AnimeFilter.ParamOutline => (int)Math.Round(((NumberSettingItem)item).Value),
        _ => null,
    };
}


