using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 智能裁切滤镜（2.1 重写旧版 SmartCropFilter）：把照片裁切为证件照标准 35:45（宽:高，
/// 即二寸规格）比例的构图。按"上中下三段比例"垂直定位裁切窗口：
/// 上带 topRatio%（头顶留白带）、中带（面部主体，比例 = 100 - topRatio - bottomRatio）、
/// 下带 bottomRatio%（肩部带）；把面部带中心对齐裁切窗中心，得到证件照式构图。
/// 2.1 简化：只裁切不缩放（旧版缩放到固定 1 寸 295×413）。输出尺寸变化（裁切窗大小），
/// 宿主应将新尺寸 PixelSurface 作为新文档应用（2.0 行为：生成新文档替换当前）。
/// </summary>
public sealed class SmartCropFilter : IFilterProcessor
{
    /// <summary>参数键：上段（头顶留白）占源图高度百分比（0~100，默认 35）。</summary>
    public const string ParamTopRatio = "topRatio";

    /// <summary>参数键：下段（肩部）占源图高度百分比（0~100，默认 15）。</summary>
    public const string ParamBottomRatio = "bottomRatio";

    /// <summary>证件照标准宽高比 35:45（宽/高 = 7/9）。</summary>
    private const double TargetAspect = 35.0 / 45.0;

    /// <inheritdoc />
    public string Id => "fptp.smartCrop";

    /// <inheritdoc />
    public string DisplayName => L10n.T("智能裁切");

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamTopRatio] = 35,
        [ParamBottomRatio] = 15,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamTopRatio,
            Label = L10n.T("上段比例(头顶留白)"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 100,
            DefaultValue = 35,
        },
        new()
        {
            Key = ParamBottomRatio,
            Label = L10n.T("下段比例(肩部)"),
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 100,
            DefaultValue = 15,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        // 读取并钳制参数（三段比例各占 0~100；上下段和超过 100 时中带退化、仍可裁切）
        int topRatio = Math.Clamp(parameters.Get(ParamTopRatio, 35), 0, 100);
        int bottomRatio = Math.Clamp(parameters.Get(ParamBottomRatio, 15), 0, 100);

        int srcW = input.Width;
        int srcH = input.Height;

        // 计算裁切窗尺寸：维持 35:45（7:9）宽高比
        int cropW, cropH;
        if (srcW > srcH * TargetAspect)
        {
            // 源图偏宽 → 全高保留，按高定宽，水平居中裁左右
            cropH = srcH;
            cropW = Math.Max(1, (int)Math.Round(srcH * TargetAspect));
        }
        else
        {
            // 源图偏高/相等 → 全宽保留，按宽定高，垂直按三段比例定位
            cropW = srcW;
            cropH = Math.Max(1, (int)Math.Round(srcW / TargetAspect));
        }
        int cropX = Math.Max(0, (srcW - cropW) / 2);

        int cropY;
        if (cropH >= srcH)
        {
            cropY = 0; // 裁切窗覆盖全高，无需垂直定位
        }
        else
        {
            // 三段比例定位：中带（面部）起点=topRatio%、终点=100-bottomRatio%
            double midStart = topRatio / 100.0;
            double midEnd = 1.0 - bottomRatio / 100.0;
            double faceCenterY = srcH * (midStart + midEnd) / 2.0;
            // 面部带中心对齐裁切窗中心；钳制到合法范围（肩部不下移出界）
            cropY = Math.Clamp((int)Math.Round(faceCenterY - cropH / 2.0), 0, srcH - cropH);
        }

        // 拷贝裁切窗口到新尺寸像素面（尺寸变化 → 宿主按新文档应用）
        var output = PixelSurface.Create(cropW, cropH);
        var editor = output.CreateEditor();
        for (int y = 0; y < cropH; y++)
        {
            ct.ThrowIfCancellationRequested();
            // 源行（裁切窗内）逐行拷入目标行（行内连续 4 字节/像素，无行尾填充）
            ReadOnlySpan<byte> srcRow = input.Row(cropY + y);
            srcRow.Slice(cropX * 4, cropW * 4).CopyTo(editor.Row(y));
        }
        editor.MarkAllDirty();

        progress?.Report(100, L10n.T("智能裁切完成（{0}×{1}）", cropW, cropH));
        return editor.Commit();
    }
}
