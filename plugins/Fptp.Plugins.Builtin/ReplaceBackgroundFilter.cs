using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Progress;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 换底色滤镜（2.1 复刻 2.0 Core ReplaceBackgroundFilter 的正常照片色键模式）：
/// 四角采样背景色 → 按容差（曼哈顿距离）把接近背景色的像素替换为目标色。
/// 2.1 简化：不做边缘羽化抗锯齿（旧版 Feather 带移除），透明像素原样保持。
/// 输入为 BGRA 预乘像素面：比较与替换前先 Unpremultiply 得到直通色（与参数可比对），
/// 目标色写入前 Premultiply（保持预乘不变量，避免半透明边缘偏色/黑边）。
/// </summary>
public sealed class ReplaceBackgroundFilter : IFilterProcessor
{
    /// <summary>参数键：目标颜色（uint PackBgra，默认蓝）。</summary>
    public const string ParamColor = "color";

    /// <summary>参数键：容差（0~200，默认 60）。</summary>
    public const string ParamTolerance = "tolerance";

    /// <summary>默认目标色：蓝色 0,0,255（直通色，PackBgra 值）。</summary>
    public static uint DefaultColor => PixelColor.PackBgra(0, 0, 255);

    /// <inheritdoc />
    public string Id => "fptp.replaceBackground";

    /// <inheritdoc />
    public string DisplayName => "换底色";

    /// <inheritdoc />
    public FilterParameters Defaults => new()
    {
        [ParamColor] = DefaultColor,
        [ParamTolerance] = 60,
    };

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters =>
    [
        new()
        {
            Key = ParamColor,
            Label = "目标颜色",
            Kind = FilterParameterKind.Color,
            DefaultValue = DefaultColor,
        },
        new()
        {
            Key = ParamTolerance,
            Label = "容差",
            Kind = FilterParameterKind.Int,
            Min = 0,
            Max = 200,
            DefaultValue = 60,
        },
    ];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(parameters);

        // 读取并钳制参数（防御宿主传入越界值）
        uint targetColor = parameters.Get(ParamColor, DefaultColor);
        int tolerance = parameters.Get(ParamTolerance, 60);
        tolerance = Math.Clamp(tolerance, 0, 200);

        // 四角采样背景参考色（各角先反预乘，与参数同处直通色空间比对）
        int width = input.Width;
        int height = input.Height;
        uint sample = SampleBackgroundColor(input);

        // 目标色写入前的预乘形式；若目标色 alpha=0（透明背景），像素只清 alpha 保留原 RGB
        uint targetPremul = PixelColor.Premultiply(targetColor);
        bool transparentTarget = PixelColor.A(targetColor) == 0;

        // COW 编辑会话：就地改写像素后 Commit 生成新像素面
        var editor = input.CreateEditor();

        // 逐像素色键替换；周期检查取消
        for (int y = 0; y < height; y++)
        {
            ct.ThrowIfCancellationRequested();
            Span<byte> row = editor.Row(y);
            int offset = 0;

            for (int x = 0; x < width; x++, offset += 4)
            {
                // 读取预乘像素并反预乘，得到直通色用于比较
                uint premul = ReadBgra(row, offset);
                if (PixelColor.A(premul) == 0)
                    continue; // 全透明像素（预乘全零）：原样保持

                uint straight = PixelColor.Unpremultiply(premul);

                // 与背景参考色做曼哈顿距离判定：容差内视为背景 → 替换
                if (PixelColor.ManhattanDistance(straight, sample) < tolerance)
                {
                    if (transparentTarget)
                    {
                        // 透明目标：保留原 RGB（已反预乘回直通），alpha 清零
                        row[offset] = PixelColor.B(straight);
                        row[offset + 1] = PixelColor.G(straight);
                        row[offset + 2] = PixelColor.R(straight);
                        row[offset + 3] = 0;
                    }
                    else
                    {
                        // 不透明目标：写入目标色的预乘形式（alpha=255 时即直通色）
                        row[offset] = PixelColor.B(targetPremul);
                        row[offset + 1] = PixelColor.G(targetPremul);
                        row[offset + 2] = PixelColor.R(targetPremul);
                        row[offset + 3] = PixelColor.A(targetPremul);
                    }
                }
            }
        }

        editor.MarkAllDirty();
        progress?.Report(100, "换底色完成");
        return editor.Commit();
    }

    /// <summary>四角采样背景参考色：取四角直通色中最接近其余三角的角（防主体占据单角误采样）。</summary>
    private static uint SampleBackgroundColor(PixelSurface input)
    {
        ReadOnlySpan<byte> topLeft = input.Row(0);
        ReadOnlySpan<byte> topRight = input.Row(0);
        ReadOnlySpan<byte> bottomLeft = input.Row(input.Height - 1);
        ReadOnlySpan<byte> bottomRight = input.Row(input.Height - 1);

        uint c0 = ReadBgra(topLeft, 0);
        uint c1 = ReadBgra(topRight, (input.Width - 1) * 4);
        uint c2 = ReadBgra(bottomLeft, 0);
        uint c3 = ReadBgra(bottomRight, (input.Width - 1) * 4);

        // 反预乘后再四角投票，保证与直通色参数同一比较空间
        return PixelColor.MostCommonCorner(
            PixelColor.Unpremultiply(c0),
            PixelColor.Unpremultiply(c1),
            PixelColor.Unpremultiply(c2),
            PixelColor.Unpremultiply(c3));
    }

    /// <summary>从 BGRA 预乘缓冲行读取像素值（uint 低位=蓝）。</summary>
    private static uint ReadBgra(ReadOnlySpan<byte> row, int offset)
        => (uint)(row[offset] | (row[offset + 1] << 8) | (row[offset + 2] << 16) | (row[offset + 3] << 24));
}
