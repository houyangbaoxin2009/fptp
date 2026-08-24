using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Progress;

namespace Fpter;

/// <summary>
/// 灰度滤镜：RGB 转灰，BT.601 亮度公式 gray = 0.299R + 0.587G + 0.114B，保持 Alpha 不变。
/// 输入为 BGRA 预乘像素：对预乘 RGB 做 BT.601 加权（r*a/255 等再加权）恰好等价于
/// 直通灰度的预乘形式，故无需先反预乘即可保持合成正确性（半透明边缘不偏色）。
/// 无参数（Parameters 空）。
/// </summary>
public sealed class GrayscaleFilter : IFilterProcessor
{
    /// <inheritdoc />
    public string Id => "fpter.grayscale";

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

        var editor = input.CreateEditor();
        Span<byte> dst = editor.Pixels;
        int length = dst.Length;

        for (int i = 0; i + 3 < length; i += 4)
        {
            ct.ThrowIfCancellationRequested();

            byte b = dst[i];
            byte g = dst[i + 1];
            byte r = dst[i + 2];
            byte gray = (byte)((r * 299 + g * 587 + b * 114) / 1000);

            dst[i] = gray;
            dst[i + 1] = gray;
            dst[i + 2] = gray;
        }

        progress?.Report(100, L10n.T("灰度完成"));
        return editor.Commit();
    }
}