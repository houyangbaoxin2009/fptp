using Osiris.Abstractions.Document;

namespace Osiris.Core.Filters;

/// <summary>
/// 滤镜公共工具：像素循环范式封装（行级 Parallel.For 并行遍历）。
/// 具体业务滤镜归扩展模块（灰度/动画面归 Fpter，工作流归 Fptm），本类仅提供范式助手。
/// </summary>
public static class FilterSupport
{
    /// <summary>
    /// 逐像素并行遍历（只读范式）：对每个像素回调 (x, y, bgra 打包值)。
    /// 行间无数据依赖 → 按行并行（行内串行保持缓存友好）。
    /// 输出范式：滤镜先在回调中计算各像素结果，再经 PixelSurface.Create(width, height, data)
    /// 构造新像素面；写场景请用 PixelSurface.CreateEditor() 的可写 Span 直写。
    /// </summary>
    public static void ForEachPixel(PixelSurface surface, Action<int, int, int> pixelAction)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ArgumentNullException.ThrowIfNull(pixelAction);

        // 按行并行：每行内串行（行内缓存友好），行间无依赖可安全并行
        Parallel.For(0, surface.Height, y =>
        {
            ReadOnlySpan<byte> row = surface.Row(y);
            for (int x = 0; x < surface.Width; x++)
            {
                int offset = x * 4;
                // 打包 BGRA（int 低位=蓝）：单次读取四个字节合成像素值
                int bgra = row[offset] | (row[offset + 1] << 8) | (row[offset + 2] << 16) | (row[offset + 3] << 24);
                pixelAction(x, y, bgra);
            }
        });
    }
}
