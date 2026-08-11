using Osiris.Abstractions.Document;
using Osiris.Abstractions.Imaging;
using SkiaSharp;

namespace Osiris.Engine.Skia;

/// <summary>
/// 文档渲染器：把 OsirisDocument 逐层合成到 SKCanvas（无 Avalonia 依赖，可离屏测试）。
/// App 画布实时绘制与导出共用此实现，是 2.1 渲染协议（docs 第 7 节）的核心。
/// </summary>
public static class DocumentRenderer
{
    /// <summary>
    /// 合成整份文档到指定画布：白底填充 → 逐层（仅 Visible）经 canvas.Save/Translate 定位，
    /// 用 ZeroCopyImage 零拷贝建图，按 Opacity 与 BlendMode 绘制，最后 Restore 还原变换。
    /// 画布状态（视口缩放等）由调用方在进入前设置，本方法只做文档→画布的层合成。
    /// </summary>
    public static void Render(SKCanvas canvas, OsirisDocument document)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(document);

        // 画布底色：白底（与 2.0 一致，证件照产品语义；导出结果同底色）。
        canvas.Clear(new SKColor(255, 255, 255));

        // 逐层从底到顶合成（Layers 索引 0 为底层）。
        foreach (Layer layer in document.Layers)
        {
            // 隐藏层或完全透明层直接跳过，不产生任何绘制开销。
            if (!layer.Visible || layer.Opacity <= 0.0)
                continue;

            // 零拷贝视图：不复制像素，仅包装底层数组（using 保证图像与固定句柄释放）。
            using SKImage image = ZeroCopyImage.CreateView(layer.Pixels);

            canvas.Save();
            try
            {
                // 图层定位：画布原点 + 图层像素偏移（视口缩放由调用方画布状态决定）。
                canvas.Translate(layer.OffsetX, layer.OffsetY);

                // 不透明度与混合模式统一经 SKPaint 合成：
                // 预乘像素下用 Alpha 调制颜色 = 通道等比缩放，即正确的"整体变透明"。
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    BlendMode = ToSkiaBlend(layer.BlendMode),
                    Color = new SKColor(255, 255, 255).WithAlpha((byte)(layer.Opacity * 255.0)),
                };

                canvas.DrawImage(image, 0f, 0f, paint);
            }
            finally
            {
                canvas.Restore();
            }
        }

        // TODO(性能)：当前为 2.1 全量绘制——每帧重绘整画布并遍历全部可见图层。
        // 超大图层/视口场景应反算可见文档矩形，只画与视口相交的图层并做部分重绘
        // （见 docs/2.1-architecture.md 第 7 节"渲染前反算可见文档矩形，只画相交图层"）。
    }

    /// <summary>离屏渲染整份文档为 SKImage（raster 后端；快照生命周期由调用方管理）。</summary>
    public static SKImage RenderToImage(OsirisDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // 离屏 raster 表面：Bgra8888 预乘与 PixelSurface 布局一致，可零拷贝互转。
        var info = new SKImageInfo(document.Width, document.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("SKSurface 离屏表面创建失败。");

        Render(surface.Canvas, document);
        return surface.Snapshot();
    }

    /// <summary>离屏渲染并把结果回读为 PixelSurface（导出与缩略图共用）。</summary>
    public static PixelSurface RenderToPixelSurface(OsirisDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using SKImage image = RenderToImage(document);
        return SkiaCodec.ReadToPixelSurface(image);
    }

    /// <summary>Abstractions 混合模式 → Skia 混合模式（Normal 默认 SrcOver，未来新增枚举兜底同 SrcOver）。</summary>
    private static SKBlendMode ToSkiaBlend(BlendMode mode) => mode switch
    {
        BlendMode.Multiply => SKBlendMode.Multiply,
        BlendMode.Screen => SKBlendMode.Screen,
        BlendMode.Overlay => SKBlendMode.Overlay,
        _ => SKBlendMode.SrcOver,
    };
}
