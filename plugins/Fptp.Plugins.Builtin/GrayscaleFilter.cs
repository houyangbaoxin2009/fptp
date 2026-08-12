using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fptp.Plugins.Builtin;

/// <summary>
/// 灰度滤镜（2.1 复刻 2.0 Core GrayscaleFilter）：RGB 转灰，BT.601 亮度公式
/// gray = 0.299R + 0.587G + 0.114B，保持 Alpha 不变。纯 PixelSurface 像素循环。
/// 输入为 BGRA 预乘像素：对预乘 RGB 做 BT.601 加权（r*a/255 等再加权）恰好等价于
/// 直通灰度的预乘形式，故无需先反预乘即可保持合成正确性（半透明边缘不偏色）。
/// 无参数（Parameters 空）。
/// </summary>
public sealed class GrayscaleFilter : IFilterProcessor
{
    /// <inheritdoc />
    public string Id => "fptp.grayscale";

    /// <inheritdoc />
    public string DisplayName => L10n.T("灰度");

    /// <inheritdoc />
    public FilterParameters Defaults => new();

    /// <inheritdoc />
    public IReadOnlyList<FilterParameterDescriptor> Parameters => [];

    /// <inheritdoc />
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);

        // COW 编辑会话：深拷贝源缓冲，就地写灰度结果，Commit 返回新像素面（源不变）
        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;
        int length = dst.Length;

        // 逐像素（BGRA 每 4 字节一像素）BT.601 加权平均；周期检查取消
        for (int i = 0; i + 3 < length; i += 4)
        {
            ct.ThrowIfCancellationRequested();

            byte b = dst[i];
            byte g = dst[i + 1];
            byte r = dst[i + 2];
            // 0.299/0.587/0.114 以整数 299/587/114 与 /1000 实现，避免浮点
            byte gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);

            dst[i] = gray;
            dst[i + 1] = gray;
            dst[i + 2] = gray;
            // Alpha 原样保留（第 i+3 字节不动）
        }

        progress?.Report(100, L10n.T("灰度完成"));
        return editor.Commit();
    }
}
