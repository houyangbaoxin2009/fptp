namespace Osiris.Abstractions.Filters;

/// <summary>
/// 滤镜参数种类：决定宿主生成的设置控件类型与运行时值约定。
/// </summary>
public enum FilterParameterKind
{
    /// <summary>布尔开关（值类型 bool）。</summary>
    Bool = 0,

    /// <summary>整数（值类型 int，受 Min/Max 约束）。</summary>
    Int = 1,

    /// <summary>浮点数（值类型 double，受 Min/Max 约束）。</summary>
    Double = 2,

    /// <summary>选项（值类型 int 或 ChoiceValues 元素，候选见 Choices/ChoiceValues）。</summary>
    Choice = 3,

    /// <summary>颜色（值类型 uint，PackBgra 格式）。</summary>
    Color = 4,
}
