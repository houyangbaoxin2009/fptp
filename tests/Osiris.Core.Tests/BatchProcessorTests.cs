using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Core.Batch;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// BatchProcessor 批处理管线测试：成功计数、逐文件失败不中断、取消传播、未知滤镜失败。
/// </summary>
public class BatchProcessorTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fptp-batch-tests", Guid.NewGuid().ToString("N"));
    private readonly string _outDir;

    public BatchProcessorTests()
    {
        _outDir = Path.Combine(_tempDir, "out");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        // 临时文件清理（失败不掩盖测试结果）
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    private static PixelSurface CreateSample() => PixelSurface.Create(2, 2);

    /// <summary>滤镜解析委托：两个 fake 滤镜，其余返回 null（未找到）。</summary>
    private static IFilterProcessor? Resolve(string id) => id switch
    {
        "test.double" => new DoubleBrightnessFilter(),
        "test.half" => new HalfBrightnessFilter(),
        _ => null,
    };

    [Fact]
    public void Run_ThreeFiles_TwoSteps_AllSucceed()
    {
        // 意图：3 文件 × 2 步滤镜链全部成功 → Succeeded==3、Failed==0、输出目录生成 3 个文件。
        string[] inputFiles = new[] { "a.png", "b.png", "c.png" }
            .Select(name => Path.Combine(_tempDir, name)).ToArray();
        BatchStep[] steps = [new BatchStep { FilterId = "test.double" }, new BatchStep { FilterId = "test.half" }];

        BatchResult result = BatchProcessor.Run(
            inputFiles, _outDir, steps,
            decode: _ => CreateSample(),
            encode: (path, surface) => { File.WriteAllBytes(path, [1, 2, 3]); return true; },
            resolveFilter: Resolve,
            progress: null,
            ct: CancellationToken.None);

        Assert.Equal(3, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);
        Assert.Equal(3, Directory.EnumerateFiles(_outDir).Count());
    }

    [Fact]
    public void Run_OneDecodeFails_ContinuesAndReportsFailed()
    {
        // 意图：某文件解码失败（decode 返回 null）不中断整批，Failed 计数并收集错误信息。
        string[] inputFiles =
        [
            Path.Combine(_tempDir, "ok.png"),
            Path.Combine(_tempDir, "bad.png"),
            Path.Combine(_tempDir, "ok2.png"),
        ];
        BatchStep[] steps = [new BatchStep { FilterId = "test.half" }];

        BatchResult result = BatchProcessor.Run(
            inputFiles, _outDir, steps,
            decode: path => path.Contains("bad") ? null : CreateSample(),
            encode: (path, surface) => true,
            resolveFilter: Resolve,
            progress: null,
            ct: CancellationToken.None);

        Assert.Equal(2, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.Contains("bad.png", result.Errors[0]);
    }

    [Fact]
    public void Run_PreCanceledToken_ThrowsOperationCanceledException()
    {
        // 意图：预取消的 CancellationToken 应立即中止整个批处理并向上传播 OCE。
        string[] inputFiles = [Path.Combine(_tempDir, "a.png")];
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            BatchProcessor.Run(
                inputFiles, _outDir, [new BatchStep { FilterId = "test.double" }],
                decode: _ => CreateSample(),
                encode: (path, surface) => true,
                resolveFilter: Resolve,
                progress: null,
                ct: cts.Token));
    }

    [Fact]
    public void Run_UnknownFilter_CountsAsFailure()
    {
        // 意图：未知滤镜 Id 按该文件失败处理，不中断整体。
        string[] inputFiles = [Path.Combine(_tempDir, "a.png")];

        BatchResult result = BatchProcessor.Run(
            inputFiles, _outDir, [new BatchStep { FilterId = "no.such.filter" }],
            decode: _ => CreateSample(),
            encode: (path, surface) => true,
            resolveFilter: Resolve,
            progress: null,
            ct: CancellationToken.None);

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Contains("no.such.filter", result.Errors[0]);
    }
}
