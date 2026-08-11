namespace Osiris.Abstractions.Filters;

/// <summary>
/// 滤镜参数描述符：声明式描述一个参数
/// （Key/Label/种类/取值范围/候选/默认值），
/// 宿主据此自动生成设置 UI，插件无需提供任何视图代码。
/// </summary>
public sealed class FilterParameterDescriptor
{
    /// <summary>参数键（与 FilterParameters 索引键一致，组内唯一）。</summary>
    public string Key { get; init; } = "";

    /// <summary>参数显示名（UI 标签）。</summary>
    public string Label { get; init; } = "";

    /// <summary>参数种类（决定控件类型与值约定）。</summary>
    public FilterParameterKind Kind { get; init; }

    /// <summary>最小值（Int/Double 用；null 表示无下限）。</summary>
    public double? Min { get; init; }

    /// <summary>最大值（Int/Double 用；null 表示无上限）。</summary>
    public double? Max { get; init; }

    /// <summary>候选显示文本（Choice 用，与 ChoiceValues 下标一一对应）。</summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>候选实际值（Choice 用；null 时直接将 Choices 文本作为值）。</summary>
    public IReadOnlyList<object>? ChoiceValues { get; init; }

    /// <summary>默认值（运行时类型与 Kind 约定，如 Int → int）。</summary>
    public object? DefaultValue { get; init; }
}
