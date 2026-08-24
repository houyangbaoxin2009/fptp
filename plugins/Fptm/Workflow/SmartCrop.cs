using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;
using Osiris.Algorithms;

namespace Fptm.Workflow;

/// <summary>
/// 智能裁切工作流（原 Fptp.Plugins.Builtin.SmartCropFilter 迁移而来）：
/// 把照片裁切为证件照标准比例并可选缩放到目标尺寸。三段比例垂直定位裁切窗
/// （上带 topRatio% 头顶留白、中带面部、下带 bottomRatio% 肩部），面部带中心对齐。
/// 尺寸预设：原始(35:45) / 1寸(295×413) / 小2寸(390×567) / 2寸(413×579) / 3寸(649×1000)。
/// 本类实现 IFilterProcessor 仅复用 Apply 签名；fptm **不注册为 IFilterPlugin**，
/// 故不出现在滤镜窗口，仅由 fptm 工作流命令/面板调用。
/// </summary>
public sealed class SmartCrop : IFilterProcessor
{
    /// <summary>参数键：上段（头顶留白）占源图高度百分比（0~100，默认 35）。</summary>
    public const string ParamTopRatio = "topRatio";

    /// <summary>参数键：下段（肩部）占源图高度百分比（0~100，默认 15）。</summary>
    public const string ParamBottomRatio = "bottomRatio";

    /// <summary>参数键：尺寸预设（int，SizePresets 下标；0=原始 35:45，1~4=标准尺寸）。</summary>
    public const string ParamPreset = "sizePreset";

    /// <summary>证件照标准宽高比 35:45（宽/高 = 7/9），用于"原始"预设。</summary>
    private const double RawAspect = 35.0 / 45.0;

    /// <summary>尺寸预设：名称 + 宽×高。下标 0 为比例保留（宽高 0 表示仅按 35:45 裁切不缩放）。</summary>
    public static readonly (string Name, int Width, int Height)[] SizePresets =
    [
        ("原始(35:45)", 0, 0),
        ("1寸(295×413)", 295, 413),
        ("小2寸(390×567)", 390, 567),
        ("2寸(413×579)", 413, 579),
        ("3寸(649×1000)", 649, 1000),
    ];

    /// <inheritdoc />
    public string Id => "fpter.smartCrop";

    /// <inheritdoc />
    public string DisplayName => L10n.T("智能裁切");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamTopRatio] = 35,
        [ParamBottomRatio] = 15,
        [ParamPreset] = 0,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamPreset,
            Label = L10n.T("尺寸预设"),
            Kind = FilterParameterKind.Choice,
            Choices = SizePresets.Select(p => p.Name).ToArray(),
            ChoiceValues = Enumerable.Range(0, SizePresets.Length).Cast<object>().ToArray(),
            DefaultValue = 0,
        },
        new() { Key = ParamTopRatio, Label = L10n.T("上段比例(头顶留白)"), Kind = FilterParameterKind.Int, Min = 0, Max = 100, DefaultValue = 35 },
        new() { Key = ParamBottomRatio, Label = L10n.T("下段比例(肩部)"), Kind = FilterParameterKind.Int, Min = 0, Max = 100, DefaultValue = 15 },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        int topRatio = Math.Clamp(parameters.Get(ParamTopRatio, 35), 0, 100);
        int bottomRatio = Math.Clamp(parameters.Get(ParamBottomRatio, 15), 0, 100);

        int presetIndex = Math.Clamp(parameters.Get(ParamPreset, 0), 0, SizePresets.Length - 1);
        var preset = SizePresets[presetIndex];
        double targetAspect = preset.Width > 0 && preset.Height > 0
            ? (double)preset.Width / preset.Height
            : RawAspect;

        int srcW = input.Width, srcH = input.Height;

        int cropW, cropH;
        if (srcW > srcH * targetAspect)
        {
            cropH = srcH;
            cropW = Math.Max(1, (int)Math.Round(srcH * targetAspect));
        }
        else
        {
            cropW = srcW;
            cropH = Math.Max(1, (int)Math.Round(srcW / targetAspect));
        }
        int cropX = Math.Max(0, (srcW - cropW) / 2);

        int cropY;
        if (cropH >= srcH)
        {
            cropY = 0;
        }
        else
        {
            double midStart = topRatio / 100.0;
            double midEnd = 1.0 - bottomRatio / 100.0;
            double faceCenterY = srcH * (midStart + midEnd) / 2.0;
            cropY = Math.Clamp((int)Math.Round(faceCenterY - cropH / 2.0), 0, srcH - cropH);
        }

        var cropped = CopyCrop(input, cropX, cropY, cropW, cropH, ct);

        if (preset.Width > 0 && preset.Height > 0
            && (preset.Width != cropped.Width || preset.Height != cropped.Height))
        {
            PixelSurface scaled = Scaling.ScaleBilinear(cropped, preset.Width, preset.Height);
            progress?.Report(100, L10n.T("智能裁切完成（{0}×{1}，{2}）", scaled.Width, scaled.Height, preset.Name));
            return scaled;
        }

        progress?.Report(100, L10n.T("智能裁切完成（{0}×{1}）", cropped.Width, cropped.Height));
        return cropped;
    }

    private static PixelSurface CopyCrop(PixelSurface input, int x, int y, int width, int height, CancellationToken ct)
    {
        var output = PixelSurface.Create(width, height);
        var editor = output.CreateEditor();
        for (int row = 0; row < height; row++)
        {
            ct.ThrowIfCancellationRequested();
            input.Row(y + row).Slice(x * 4, width * 4).CopyTo(editor.Row(row));
        }
        editor.MarkAllDirty();
        return editor.Commit();
    }
}