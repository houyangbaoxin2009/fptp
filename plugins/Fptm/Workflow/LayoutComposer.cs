using Osiris.Abstractions.Document;
using Osiris.Algorithms;

namespace Fptm.Workflow;

/// <summary>
/// 证件照排版工作流（原 Fptp.Plugins.Builtin.LayoutComposer 迁移，改用 Osiris.Algorithms.Scaling）：
/// 把单张照片按网格居中排列到相纸（5寸/6寸/A4/A5/自定义），只做像素拷贝。
/// 相纸预设 5寸 1500×1050 / 6寸 1800×1200 / A5 2480×1748 / A4 3508×2480；可选画虚线裁剪引导线。
/// 照片大于相纸时先等比双线性缩小到相纸内（否则居中起点为负导致越界）。
/// </summary>
public static class LayoutComposer
{
    /// <summary>照片间隙（像素）。</summary>
    private const int Gap = 40;

    /// <summary>自定义相纸标识（Settings Choice 值；此时用 CustomWidth/Height）。</summary>
    public const string CustomPaper = "自定义";

    /// <summary>相纸预设：key → 宽×高。</summary>
    public static readonly IReadOnlyDictionary<string, (int Width, int Height)> PaperPresets =
        new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            ["5寸"] = (1500, 1050),
            ["6寸"] = (1800, 1200),
            ["A5"] = (2480, 1748),
            ["A4"] = (3508, 2480),
        };

    /// <summary>
    /// 排版单张照片到相纸：返回相纸像素面与网格行列数。
    /// paperName 为"自定义"时用 customW/customH，否则查预设；未知且非自定义返回 null。
    /// showGuides 为 true 时在每张照片四周画虚线裁剪引导线（画在照片之上）。
    /// </summary>
    public static PixelSurface? Compose(
        PixelSurface photo,
        string paperName,
        out int columns,
        out int rows,
        int customW = 0,
        int customH = 0,
        bool showGuides = false)
    {
        ArgumentNullException.ThrowIfNull(photo);
        ArgumentNullException.ThrowIfNull(paperName);

        columns = 1;
        rows = 1;

        int paperW, paperH;
        if (paperName == CustomPaper)
        {
            if (customW <= 0 || customH <= 0)
                return null;
            paperW = customW;
            paperH = customH;
        }
        else
        {
            if (!PaperPresets.TryGetValue(paperName, out var paper))
                return null;
            paperW = paper.Width;
            paperH = paper.Height;
        }

        if (photo.Width > paperW || photo.Height > paperH)
        {
            double scale = Math.Min((double)paperW / photo.Width, (double)paperH / photo.Height);
            photo = Scaling.ScaleBilinear(photo, Math.Max(1, (int)(photo.Width * scale)), Math.Max(1, (int)(photo.Height * scale)));
        }

        int photoW = photo.Width, photoH = photo.Height;
        columns = Math.Max(1, (paperW + Gap) / (photoW + Gap));
        rows = Math.Max(1, (paperH + Gap) / (photoH + Gap));

        int contentWidth = columns * photoW + (columns - 1) * Gap;
        int contentHeight = rows * photoH + (rows - 1) * Gap;
        int startX = (paperW - contentWidth) / 2;
        int startY = (paperH - contentHeight) / 2;

        var editor = PixelSurface.Create(paperW, paperH).CreateEditor();
        FillWhite(editor);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                CopyPhoto(editor, photo, startX + c * (photoW + Gap), startY + r * (photoH + Gap));

        if (showGuides)
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    DrawCutGuide(editor, startX + c * (photoW + Gap), startY + r * (photoH + Gap), photoW, photoH, paperW, paperH);

        editor.MarkAllDirty();
        return editor.Commit();
    }

    private static void FillWhite(PixelSurfaceEditor editor)
    {
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 255; pixels[i + 1] = 255; pixels[i + 2] = 255; pixels[i + 3] = 255;
        }
    }

    private static void CopyPhoto(PixelSurfaceEditor paper, PixelSurface photo, int dstX, int dstY)
    {
        int rowBytes = photo.Width * 4;
        for (int r = 0; r < photo.Height; r++)
        {
            int dstRow = dstY + r;
            if (dstRow < 0 || dstRow >= paper.Height) continue;
            if (dstX >= paper.Width) continue;

            ReadOnlySpan<byte> src = photo.Row(r);
            Span<byte> dstRowSpan = paper.Row(dstRow);
            int copyStart = dstX < 0 ? -dstX : 0;
            int visible = Math.Min(rowBytes - copyStart * 4, paper.RowBytes - dstX * 4);
            if (visible <= 0) continue;
            src.Slice(copyStart * 4, visible).CopyTo(dstRowSpan.Slice(dstX * 4, visible));
        }
    }

    /// <summary>在照片矩形四周画虚线裁剪引导线（浅灰，钳制到相纸范围）。</summary>
    private static void DrawCutGuide(PixelSurfaceEditor paper, int x, int y, int w, int h, int paperW, int paperH)
    {
        const int dashLen = 12, gapLen = 8;
        foreach (var (ax, ay, isHorizontal) in new[]
                 {
                     (x, y, true), (x, y + h - 1, true), (x, y, false), (x + w - 1, y, false),
                 })
        {
            int len = isHorizontal ? w : h;
            for (int i = 0; i < len; i += dashLen + gapLen)
            {
                int end = Math.Min(i + dashLen, len);
                for (int t = i; t < end; t++)
                {
                    int px = isHorizontal ? ax + t : ax;
                    int py = isHorizontal ? ay : ay + t;
                    if (px < 0 || py < 0 || px >= paperW || py >= paperH) continue;
                    SetPixelGray(paper, px, py);
                }
            }
        }
    }

    private static void SetPixelGray(PixelSurfaceEditor paper, int x, int y)
    {
        Span<byte> row = paper.Row(y);
        int o = x * 4;
        row[o] = 160; row[o + 1] = 160; row[o + 2] = 160; row[o + 3] = 216;
    }
}