using Osiris.Abstractions.Document;

namespace Osiris.Core.Imaging;

/// <summary>
/// 证件照排版处理器（2.0 GenSettings 排版能力的新架构重写）：
/// 把单张照片按 columns×rows 网格排版到相纸（5寸/6寸/A4 预设），网格整体居中。
/// 纯 PixelSurface 像素合成（预乘 BGRA 白底 + 照片逐行拷贝），零渲染后端依赖。
/// 照片大于单元格时等比缩小保持清晰，透明照片像素直接拷贝（白底兜底）。
/// </summary>
public static class LayoutProcessor
{
    /// <summary>
    /// 相纸尺寸预设（像素，300dpi 基准；调用方可按 dpi 参数缩放）：
    /// 5寸 1270×889、6寸 1524×1016、A4 2480×3508（竖版 A4）。
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (int Width, int Height)> PaperPresets =
        new Dictionary<string, (int Width, int Height)>(StringComparer.Ordinal)
        {
            ["5寸"] = (1270, 889),
            ["6寸"] = (1524, 1016),
            ["A4"] = (2480, 3508),
        };

    /// <summary>排版结果：白底相纸像素面 + 实际排版行列数。</summary>
    public sealed record LayoutResult(PixelSurface Paper, int Columns, int Rows);

    /// <summary>
    /// 按预设相纸排版：照片等比缩小适应单元格（保持宽高比），网格居中，白底输出。
    /// </summary>
    /// <param name="photo">源照片（BGRA 预乘）。</param>
    /// <param name="paperName">相纸预设名（"5寸"/"6寸"/"A4"）。</param>
    /// <param name="columns">排版列数。</param>
    /// <param name="rows">排版行数。</param>
    /// <param name="gapPx">照片间间隙（像素）。</param>
    /// <param name="dpi">输出分辨率（预设以 300dpi 定义，相纸按 dpi/300 缩放）。</param>
    public static LayoutResult LayoutPreset(PixelSurface photo, string paperName, int columns, int rows, int gapPx = 8, int dpi = 300)
    {
        ArgumentNullException.ThrowIfNull(photo);
        if (!PaperPresets.TryGetValue(paperName, out (int Width, int Height) preset))
            throw new ArgumentException($"未知相纸预设: {paperName}（可选：{string.Join(" / ", PaperPresets.Keys)}）", nameof(paperName));
        if (columns <= 0 || rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns), "排版行列数必须为正");
        if (gapPx < 0)
            throw new ArgumentOutOfRangeException(nameof(gapPx), "间隙不能为负");

        // 相纸按 dpi 缩放（预设定义于 300dpi）
        double dpiScale = dpi / 300.0;
        int paperWidth = Math.Max(1, (int)Math.Round(preset.Width * dpiScale));
        int paperHeight = Math.Max(1, (int)Math.Round(preset.Height * dpiScale));

        // 单元格尺寸：相纸宽度去掉横向间隙后均分给每列（防 0 保护）
        int cellWidth = Math.Max(1, (paperWidth - (columns - 1) * gapPx) / columns);
        int cellHeight = Math.Max(1, (paperHeight - (rows - 1) * gapPx) / rows);

        // 照片等比缩放适应单元格：仅缩小不放大（避免插值模糊降低清晰度）
        double fit = Math.Min((double)cellWidth / photo.Width, (double)cellHeight / photo.Height);
        PixelSurface scaled = fit < 1.0
            ? ScaleBilinear(photo, Math.Max(1, (int)(photo.Width * fit)), Math.Max(1, (int)(photo.Height * fit)))
            : photo;
        int photoWidth = scaled.Width;
        int photoHeight = scaled.Height;

        // 网格整体居中：内容块 = 列×照片 + 间隙，起点 = (相纸 - 内容块)/2
        int contentWidth = columns * photoWidth + (columns - 1) * gapPx;
        int contentHeight = rows * photoHeight + (rows - 1) * gapPx;
        int startX = Math.Max(0, (paperWidth - contentWidth) / 2);
        int startY = Math.Max(0, (paperHeight - contentHeight) / 2);

        // 白底相纸：预乘 BGRA 全 255 = 不透明白
        PixelSurfaceEditor paper = PixelSurface.Create(paperWidth, paperHeight).CreateEditor();
        paper.Pixels.Fill(255);

        // 逐格拷贝照片（行间隔 gapPx，格内照片恰好填充槽位）
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int x = startX + col * (photoWidth + gapPx);
                int y = startY + row * (photoHeight + gapPx);
                CopyPhoto(paper, scaled, x, y);
            }
        }

        return new LayoutResult(paper.Commit(), columns, rows);
    }

    /// <summary>整块拷贝照片到相纸（逐行拷贝；越界部分裁剪）。</summary>
    private static void CopyPhoto(PixelSurfaceEditor paper, PixelSurface photo, int dstX, int dstY)
    {
        // 防御性裁剪到相纸边界（网格居中通常不越界）
        int clipWidth = Math.Min(photo.Width, paper.Width - dstX);
        int clipHeight = Math.Min(photo.Height, paper.Height - dstY);
        if (clipWidth <= 0 || clipHeight <= 0)
            return;

        int rowBytes = clipWidth * 4;
        for (int r = 0; r < clipHeight; r++)
            photo.Row(r)[..rowBytes].CopyTo(paper.Row(dstY + r)[(dstX * 4)..]);
    }

    /// <summary>双线性插值缩放（BGRA 各通道独立插值；预乘像素直接插值保持一致性）。</summary>
    private static PixelSurface ScaleBilinear(PixelSurface source, int outWidth, int outHeight)
    {
        PixelSurfaceEditor editor = PixelSurface.Create(outWidth, outHeight).CreateEditor();
        Span<byte> dst = editor.Pixels;
        double scaleX = (double)source.Width / outWidth;
        double scaleY = (double)source.Height / outHeight;
        ReadOnlySpan<byte> src = source.Pixels;

        for (int y = 0; y < outHeight; y++)
        {
            // 目标行中心反投影到源坐标，取四邻域加权
            double sy = (y + 0.5) * scaleY - 0.5;
            int y0 = Clamp((int)Math.Floor(sy), 0, source.Height - 1);
            int y1 = Clamp(y0 + 1, 0, source.Height - 1);
            double fy = Clamp01(sy - y0);

            for (int x = 0; x < outWidth; x++)
            {
                double sx = (x + 0.5) * scaleX - 0.5;
                int x0 = Clamp((int)Math.Floor(sx), 0, source.Width - 1);
                int x1 = Clamp(x0 + 1, 0, source.Width - 1);
                double fx = Clamp01(sx - x0);

                int outIndex = (y * outWidth + x) * 4;
                for (int c = 0; c < 4; c++)
                {
                    // 2×2 双线性：先水平插值再垂直插值
                    int s00 = (y0 * source.RowBytes + x0 * 4) + c;
                    int s10 = (y0 * source.RowBytes + x1 * 4) + c;
                    int s01 = (y1 * source.RowBytes + x0 * 4) + c;
                    int s11 = (y1 * source.RowBytes + x1 * 4) + c;

                    double top = src[s00] + (src[s10] - src[s00]) * fx;
                    double bottom = src[s01] + (src[s11] - src[s01]) * fx;
                    dst[outIndex + c] = (byte)(top + (bottom - top) * fy);
                }
            }
        }
        return editor.Commit();
    }

    /// <summary>钳制到 [min, max]。</summary>
    private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);

    /// <summary>钳制到 [0, 1]。</summary>
    private static double Clamp01(double value) => value < 0 ? 0 : (value > 1 ? 1 : value);
}
