using Osiris.Abstractions.Imaging;

namespace Osiris.Abstractions.Document;

/// <summary>
/// 图层：不可变数据（COW 语义）。
/// 任何修改都必须用 with 表达式或 With* 派生方法产生新实例，
/// 历史栈因此可 O(1) 保存"变换前引用"，实现零拷贝撤销。
/// </summary>
public sealed record Layer(PixelSurface Pixels)
{
    /// <summary>图层唯一 Id（历史命令定位图层用）。</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>图层显示名。</summary>
    public string Name { get; init; } = "图层";

    /// <summary>可见性。</summary>
    public bool Visible { get; init; } = true;

    /// <summary>不透明度（0~1，0 为全透明）。</summary>
    public double Opacity { get; init; } = 1.0;

    /// <summary>混合模式（与下层合成方式）。</summary>
    public BlendMode BlendMode { get; init; } = BlendMode.Normal;

    /// <summary>水平偏移（相对画布原点，像素）。</summary>
    public int OffsetX { get; init; }

    /// <summary>垂直偏移（相对画布原点，像素）。</summary>
    public int OffsetY { get; init; }

    /// <summary>以新像素面派生图层（其余属性不变）。</summary>
    public Layer WithPixels(PixelSurface pixels) => this with { Pixels = pixels };

    /// <summary>以新不透明度派生图层。</summary>
    public Layer WithOpacity(double opacity) => this with { Opacity = opacity };

    /// <summary>以新可见性派生图层。</summary>
    public Layer WithVisible(bool visible) => this with { Visible = visible };

    /// <summary>以新名称派生图层。</summary>
    public Layer WithName(string name) => this with { Name = name };

    /// <summary>以新偏移派生图层。</summary>
    public Layer WithOffset(int offsetX, int offsetY) => this with { OffsetX = offsetX, OffsetY = offsetY };
}
