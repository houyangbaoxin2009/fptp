namespace Osiris.Abstractions.Imaging;

/// <summary>
/// 图层混合模式：合成当前图层与下层结果时的混合方式。
/// 枚举定义（像素公式）由宿主渲染层实现，契约层只声明语义。
/// </summary>
public enum BlendMode
{
    /// <summary>正常：直接覆盖下层。</summary>
    Normal = 0,

    /// <summary>正片叠底：相乘整体变暗。</summary>
    Multiply = 1,

    /// <summary>滤色：反色相乘再取反，整体变亮。</summary>
    Screen = 2,

    /// <summary>叠加：结合 Multiply/Screen，保留明暗对比。</summary>
    Overlay = 3,
}
