namespace Osiris.Abstractions.Ui;

/// <summary>鼠标按键（工具交互用）。</summary>
public enum ToolMouseButton
{
    /// <summary>左键。</summary>
    Left = 0,

    /// <summary>中键。</summary>
    Middle = 1,

    /// <summary>右键。</summary>
    Right = 2,
}

/// <summary>修饰键组合（[Flags] 可按位组合，如 Shift+Control）。</summary>
[Flags]
public enum ToolModifiers
{
    /// <summary>无修饰键。</summary>
    None = 0,

    /// <summary>Shift。</summary>
    Shift = 1,

    /// <summary>Control。</summary>
    Control = 2,

    /// <summary>Alt。</summary>
    Alt = 4,
}

/// <summary>
/// 鼠标事件（文档像素坐标，非控件坐标）。
/// 画布已完成坐标逆变换，工具直接消费本坐标进行交互/命中判断。
/// </summary>
public readonly record struct ToolMouseEvent(int X, int Y, ToolMouseButton Button, ToolModifiers Modifiers);
