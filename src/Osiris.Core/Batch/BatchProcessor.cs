using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Progress;

namespace Osiris.Core.Batch;

/// <summary>
/// 批处理进度行（宿主适配用：UI 列表/控制台逐行展示，非 Run 内部回调）。
/// </summary>
public sealed record BatchProgress(int Completed, int Total, string CurrentFile, string Message);

/// <summary>批处理步骤：一个滤镜 Id + 其运行时参数（声明式数据，tie 可直接构造）。</summary>
public sealed class BatchStep
{
    /// <summary>滤镜 Id（如 "fptp.builtin.chgcolor"）。</summary>
    public string FilterId { get; init; } = "";

    /// <summary>滤镜参数（缺省为默认参数）。</summary>
    public FilterParameters Parameters { get; init; } = new();
}

/// <summary>批处理结果汇总。</summary>
public sealed record BatchResult(int Succeeded, int Failed, IReadOnlyList<string> Errors);

/// <summary>
/// 批处理管线（架构 10 节，零 UI / 零渲染后端依赖）：
/// 输入文件列表 → 逐张 decode → 逐步骤 resolveFilter+Apply → encode 到输出目录（同名）。
/// 编解码经委托注入（Engine.Skia 提供），滤镜经委托从注册表解析——Core 不直接引用 SkiaSharp。
/// 逐张失败收集错误不中断；取消（ct）立即中止并向上传播 OperationCanceledException。
/// </summary>
public static class BatchProcessor
{
    /// <summary>
    /// 执行批处理管线。
    /// </summary>
    /// <param name="inputFiles">输入图片路径列表。</param>
    /// <param name="outputDir">输出目录（不存在则创建；输出文件与输入同名）。</param>
    /// <param name="steps">滤镜步骤链（按序逐张应用）。</param>
    /// <param name="decode">解码委托：路径 → 像素面（返回 null 视为解码失败）。</param>
    /// <param name="encode">编码委托：路径 + 像素面 → 是否成功。</param>
    /// <param name="resolveFilter">滤镜解析委托：Id → IFilterProcessor（null 视为未找到）。</param>
    /// <param name="progress">进度上报（0~100 + 消息；可 null）。</param>
    /// <param name="ct">取消令牌：任一环节抛出 OperationCanceledException 即中止整个批处理。</param>
    public static BatchResult Run(
        IReadOnlyList<string> inputFiles,
        string outputDir,
        IReadOnlyList<BatchStep> steps,
        Func<string, PixelSurface?> decode,
        Func<string, PixelSurface, bool> encode,
        Func<string, IFilterProcessor?> resolveFilter,
        IProgress? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputFiles);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(decode);
        ArgumentNullException.ThrowIfNull(encode);
        ArgumentNullException.ThrowIfNull(resolveFilter);

        // 输出目录：先建后写（与源目录相同时避免丢失源文件——调用方保证文件名不冲突）
        Directory.CreateDirectory(outputDir);

        int succeeded = 0;
        int failed = 0;
        var errors = new List<string>();

        for (int i = 0; i < inputFiles.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            string file = inputFiles[i];
            string outputPath = Path.Combine(outputDir, Path.GetFileName(file));

            try
            {
                // 1) 解码：委托返回 null 视为解码失败
                PixelSurface? current = decode(file);
                if (current is null)
                    throw new InvalidOperationException("解码失败（返回 null）。");

                // 2) 滤镜链：逐步骤解析滤镜并应用（滤镜内部周期检查 ct）
                foreach (BatchStep step in steps)
                {
                    ct.ThrowIfCancellationRequested();
                    IFilterProcessor? filter = resolveFilter(step.FilterId);
                    if (filter is null)
                        throw new InvalidOperationException($"未找到滤镜: {step.FilterId}");
                    current = filter.Apply(current, step.Parameters, progress, ct);
                }

                // 3) 编码保存（同名输出）；返回 false 视为失败
                if (!encode(outputPath, current))
                    throw new InvalidOperationException("编码保存失败。");

                succeeded++;
            }
            catch (OperationCanceledException)
            {
                // 取消：不吞异常，立即中止整个批处理
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{file}: {ex.Message}");
            }

            // 进度：按完成数/总数百分比上报（0~100）
            double percent = (i + 1.0) / inputFiles.Count * 100.0;
            progress?.Report(percent, Path.GetFileName(file));
        }

        return new BatchResult(succeeded, failed, errors);
    }
}
