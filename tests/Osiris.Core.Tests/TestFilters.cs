using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Progress;

namespace Osiris.Core.Tests;

/// <summary>
/// 批处理测试用 fake 滤镜：纯像素算术（RGB×2 / RGB÷2），
/// 只用于验证 BatchProcessor 管线对滤镜的驱动行为，不引入真实业务滤镜。
/// </summary>
public sealed class DoubleBrightnessFilter : IFilterProcessor
{
    public string Id => "test.double";
    public string DisplayName => "加亮";
    public FilterParameters Defaults => new();
    public IReadOnlyList<FilterParameterDescriptor> Parameters => [];

    /// <summary>RGB 通道翻倍（截断 255），Alpha 不变——验证滤镜被管线按步调用。</summary>
    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        PixelSurfaceEditor editor = input.CreateEditor();
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i + 3 < pixels.Length; i += 4)
        {
            ct.ThrowIfCancellationRequested();
            pixels[i] = (byte)Math.Min(255, pixels[i] * 2);
            pixels[i + 1] = (byte)Math.Min(255, pixels[i + 1] * 2);
            pixels[i + 2] = (byte)Math.Min(255, pixels[i + 2] * 2);
        }
        return editor.Commit();
    }
}

/// <summary>RGB 通道减半，Alpha 不变。</summary>
public sealed class HalfBrightnessFilter : IFilterProcessor
{
    public string Id => "test.half";
    public string DisplayName => "减暗";
    public FilterParameters Defaults => new();
    public IReadOnlyList<FilterParameterDescriptor> Parameters => [];

    public PixelSurface Apply(PixelSurface input, FilterParameters parameters, IProgress? progress, CancellationToken ct)
    {
        PixelSurfaceEditor editor = input.CreateEditor();
        Span<byte> pixels = editor.Pixels;
        for (int i = 0; i + 3 < pixels.Length; i += 4)
        {
            ct.ThrowIfCancellationRequested();
            pixels[i] = (byte)(pixels[i] / 2);
            pixels[i + 1] = (byte)(pixels[i + 1] / 2);
            pixels[i + 2] = (byte)(pixels[i + 2] / 2);
        }
        return editor.Commit();
    }
}
