using Osiris.Abstractions.Document;
using Osiris.Abstractions.Progress;

namespace Osiris.Abstractions.Filters;

/// <summary>
/// 滤镜处理器契约：插件实现并注册，宿主（App 界面/CLI 批处理）统一调用。
/// ABI 红线：输入输出只允许 PixelSurface（byte[]），禁止 SK/Avalonia 类型；
/// 参数经声明式描述（Parameters）+ 运行时值（FilterParameters）传递。
/// </summary>
public interface IFilterProcessor
{
    /// <summary>滤镜唯一 Id（如 "fptp.builtin.chgcolor"）。</summary>
    string Id { get; }

    /// <summary>滤镜显示名（UI 滤镜列表展示）。</summary>
    string DisplayName { get; }

    /// <summary>默认参数（用户未调参时使用，键与 Parameters 声明一致）。</summary>
    FilterParameters Defaults { get; }

    /// <summary>参数声明（宿主据此自动生成设置 UI，无需插件提供视图）。</summary>
    IReadOnlyList<FilterParameterDescriptor> Parameters { get; }

    /// <summary>
    /// 执行滤镜：输入保持不变，返回处理后的新像素面；
    /// 可经 progress 上报进度（0~100）、经 ct 响应取消（应周期检查并抛 OperationCanceledException）。
    /// </summary>
    PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct);
}
