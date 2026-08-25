using Osiris.Abstractions.Document;
using Osiris.Algorithms;

namespace Fptm.Workflow;

/// <summary>
/// 证件照排版工作流（原 Fptp.Plugins.Builtin.LayoutComposer 迁移，改用 Osiris.Algorithms.Scaling）：
/// 把单张照片按网格居中排列到相纸（5寸/6寸/A4/A5/自定义/拼版模板），只做像素拷贝。
/// 相纸预设 5寸 1500×1050 / 6寸 1800×1200 / A5 2480×1748 / A4 3508×2480；可选画虚线裁剪引导线。
/// 拼版模板（LayoutTemplates）：按证件照标准尺寸缩放照片后固定列×行排布（6寸·1寸×8、6寸·2寸×4）。
/// 照片大于相纸（或模板尺寸）时先等比双线性缩小到相纸内（否则居中起点为负导致越界）。
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

    /// <summary>证件照拼版模板：相纸尺寸 + 照片标准尺寸 + 固定网格（列×行）。</summary>
    public sealed record LayoutTemplate(string Name, int PaperW, int PaperH, int PhotoW, int PhotoH, int Columns, int Rows);

    /// <summary>拼版模板预设（6寸相纸常用证件照版式）。</summary>
    public static readonly IReadOnlyList<LayoutTemplate> LayoutTemplates =
    [
        new("6寸·1寸×8", 1800, 1200, 295, 413, 4, 2),
        new("6寸·2寸×4", 1800, 1200, 413, 579, 2, 2),
    ];

    /// <summary>按名称查找拼版模板；未命中返回 null。</summary>
    public static LayoutTemplate? FindTemplate(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        foreach (LayoutTemplate tpl in LayoutTemplates)
            if (tpl.Name == name)
                return tpl;
        return null;
    }

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
        LayoutTemplate? template = null;
        if (paperName == CustomPaper)
        {
            if (customW <= 0 || customH <= 0)
                return null;
            paperW = customW;
            paperH = customH;
        }
        else if ((template = FindTemplate(paperName)) is not null)
        {
            // 拼版模板：相纸尺寸取自模板（6寸 1800×1200）
            paperW = template.PaperW;
            paperH = template.PaperH;
        }
        else
        {
            if (!PaperPresets.TryGetValue(paperName, out var paper))
                return null;
            paperW = paper.Width;
            paperH = paper.Height;
        }

        // 拼版模板：照片先缩放到标准证件照尺寸，网格固定列×行；否则按相纸自动网格。
        if (template is not null)
        {
            if (photo.Width != template.PhotoW || photo.Height != template.PhotoH)
                photo = Scaling.ScaleBilinear(photo, template.PhotoW, template.PhotoH);
            columns = template.Columns;
            rows = template.Rows;
        }
        else if (photo.Width > paperW || photo.Height > paperH)
        {
            double scale = Math.Min((double)paperW / photo.Width, (double)paperH / photo.Height);
            photo = Scaling.ScaleBilinear(photo, Math.Max(1, (int)(photo.Width * scale)), Math.Max(1, (int)(photo.Height * scale)));
            columns = Math.Max(1, (paperW + Gap) / (photo.Width + Gap));
            rows = Math.Max(1, (paperH + Gap) / (photo.Height + Gap));
        }
        else
        {
            columns = Math.Max(1, (paperW + Gap) / (photo.Width + Gap));
            rows = Math.Max(1, (paperH + Gap) / (photo.Height + Gap));
        }

        int photoW = photo.Width, photoH = photo.Height;
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