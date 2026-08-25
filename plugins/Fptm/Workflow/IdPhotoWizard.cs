using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Progress;

namespace Fptm.Workflow;

/// <summary>一键证件照参数（传统面板向导收集，纯数据行）。</summary>
/// <param name="PresetIndex">智能裁切尺寸预设下标（SmartCrop.SizePresets）。</param>
/// <param name="Color">目标底色（uint PackBgra，0xAARRGGBB）。</param>
/// <param name="Tolerance">换底容差（0~200）。</param>
/// <param name="Feather">边缘羽化（0~20）。</param>
/// <param name="Paper">排版相纸名称（"不排版" 或 LayoutComposer 预设/模板/自定义）。</param>
/// <param name="CustomW">自定义相纸宽（Paper=自定义时生效）。</param>
/// <param name="CustomH">自定义相纸高（Paper=自定义时生效）。</param>
/// <param name="Guides">是否画裁剪引导线。</param>
public sealed record IdPhotoWizardOptions(
    int PresetIndex, uint Color, int Tolerance, int Feather,
    string? Paper, int CustomW, int CustomH, bool Guides);

/// <summary>
/// 一键证件照向导（fptm）：串联 智能裁切(SmartCrop) → 换底色(BackgroundReplace) →
/// 可选排版(LayoutComposer)，单步从普通照片生成证件照（或相纸拼版）。
/// 纯像素管线，零 UI 依赖（命令/面板/测试共用）；任一步不支持时回退上一产物。
/// </summary>
public static class IdPhotoWizard
{
    /// <summary>不排版标识（Request Paper 为该值时只返回换底/裁切结果）。</summary>
    public const string NoLayoutPaper = "不排版";

    /// <summary>
    /// 执行一键证件照：返回最终像素面。
    /// 尺寸预设越界自动钳制；换底/裁切参数越界由各自滤镜钳制。
    /// </summary>
    public static PixelSurface Run(PixelSurface source, IdPhotoWizardOptions options, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        // 1) 智能裁切（尺寸预设）
        var crop = new FilterParameters
        {
            [SmartCrop.ParamPreset] = Math.Clamp(options.PresetIndex, 0, SmartCrop.SizePresets.Length - 1),
        };
        PixelSurface cropped = new SmartCrop().Apply(source, crop, progress, ct);

        // 2) 换底色（目标色 + 容差 + 羽化；仅换底色，不使用背景图片）
        var background = new FilterParameters
        {
            [BackgroundReplace.ParamColor] = options.Color,
            [BackgroundReplace.ParamTolerance] = Math.Clamp(options.Tolerance, 0, 200),
            [BackgroundReplace.ParamFeather] = Math.Clamp(options.Feather, 0, 20),
        };
        PixelSurface replaced = new BackgroundReplace().Apply(cropped, background, progress, ct);

        // 3) 排版（可选；未知相纸/自定义尺寸非法 → 回退已换底结果）
        string paper = options.Paper ?? NoLayoutPaper;
        if (paper == NoLayoutPaper)
            return replaced;
        if (paper == LayoutComposer.CustomPaper && (options.CustomW <= 0 || options.CustomH <= 0))
            return replaced;

        PixelSurface? composed = LayoutComposer.Compose(replaced, paper, out _, out _, options.CustomW, options.CustomH, options.Guides);
        return composed ?? replaced;
    }
}