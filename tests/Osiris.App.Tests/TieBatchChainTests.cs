using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Filters;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Ui;
using Osiris.Core.Batch;
using Osiris.Core.Plugins;
using Osiris.Core.Storage;
using Osiris.Core.Tie;
using Osiris.Engine.Skia;
using Xunit;

namespace Osiris.App.Tests;

/// <summary>
/// tie 脚本滤镜进批处理链（BatchStep 数据化 ↔ 脚本滤镜联动）端到端测试：
/// 用仓库随附的官方示例插件 plugins/TieDemo（棕褐滤镜 + 强度参数自描述）经
/// ModuleLoader 加载 → BatchProcessor 以真实 PNG（Skia 编解码）跑 batch 步骤
/// intensity=100 → 输出像素等于 sepia(60%) 数学期望（蓝 255,0,0 → 33,42,48）。
/// </summary>
public class TieBatchChainTests
{
    /// <summary>向上定位仓库根（含 plugins/TieDemo）。</summary>
    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "plugins", "TieDemo")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("找不到仓库 plugins/TieDemo（官方 tie 示例插件）。");
    }

    private static void CopyDir(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(source, file);
            string target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    [Fact]
    public void Batch_脚本滤镜步骤_真实图片端到端()
    {
        // tiec 不可用（未随包/未设 FPTP_TIE_HOME）时跳过——该环节非本测试目标
        if (TieRunner.FindTiec() is null)
            return;

        string repo = FindRepoRoot();
        string work = Path.Combine(Path.GetTempPath(), "osiris-batch-tie", Guid.NewGuid().ToString("N"));
        try
        {
            // ---- 装载官方 tie 示例插件（源码即产物，整目录复制） ----
            string plugins = Path.Combine(work, "plugins");
            CopyDir(Path.Combine(repo, "plugins", "TieDemo"), Path.Combine(plugins, "TieDemo"));

            var registry = new ModuleRegistry(
                Path.Combine(work, "modules.data.tie"),
                Path.Combine(work, "settings.data.tie"),
                Path.Combine(work, "secure.data.tie"),
                new TieDataConfigStore());
            int loaded = ModuleLoader.LoadFromDirectory(plugins, registry, new StubHost(),
                (name, ex) => throw new InvalidOperationException($"模块加载失败 [{name}]: {ex.Message}"));
            Assert.Equal(1, loaded);

            IFilterPlugin tieModule = Assert.Single(registry.GetInstances().OfType<IFilterPlugin>());
            IFilterProcessor filter = Assert.Single(tieModule.Filters);
            Assert.Equal("tie.demo.script", filter.Id);

            // ---- 真实图片 2×2（蓝/红），Skia 编码为 PNG ----
            string inPng = Path.Combine(work, "in.png");
            string outDir = Path.Combine(work, "out");
            var input = PixelSurface.Create(2, 2).CreateEditor();
            Span<byte> px = input.Pixels;
            px[0] = 255; px[1] = 0; px[2] = 0; px[3] = 255;      // 蓝 BGRA
            px[4] = 0; px[5] = 0; px[6] = 255; px[7] = 255;      // 红 BGRA
            SkiaCodec.EncodePng(input.Commit(), inPng);

            // ---- 批处理：BatchStep {tie.demo.script, intensity=100}，decode/encode 走 Skia ----
            var steps = new[]
            {
                new BatchStep { FilterId = filter.Id, Parameters = new FilterParameters { ["intensity"] = 100 } },
            };
            Func<string, IFilterProcessor?> resolve = id => registry.GetInstances()
                .OfType<IFilterPlugin>()
                .SelectMany(m => m.Filters)
                .FirstOrDefault(f => f.Id == id);

            BatchResult result = BatchProcessor.Run(
                [inPng], outDir, steps,
                SkiaCodec.Decode,
                (path, surface) => { SkiaCodec.EncodePng(surface, path); return true; },
                resolve, progress: null, CancellationToken.None);

            Assert.Equal(1, result.Succeeded);
            Assert.Empty(result.Errors);

            // ---- 输出像素 = sepia(100)（blue 255,0,0 → b=33,g=42,r=48） ----
            string outPng = Path.Combine(outDir, "in.png");
            Assert.True(File.Exists(outPng));
            PixelSurface output = SkiaCodec.Decode(outPng)!;
            Assert.Equal(new byte[] { 33, 42, 48, 255 }, output.Row(0)[0..4].ToArray());
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { /* 清理失败忽略 */ }
        }
    }

    /// <summary>测试宿主上下文（无 UI/文档，Ui=null 跳过 UI 注册）。</summary>
    private sealed class StubHost : IHostContext
    {
        public StubHost() => Services = new ServiceRegistry();

        public OsirisDocument? ActiveDocument => null;
        public IServiceRegistry Services { get; }
        public IUiService? Ui => null;
        public IProgress Report => new NullProgress();

        private sealed class NullProgress : IProgress
        {
            public void Report(double percent, string message) { /* 测试不关心进度 */ }
        }
    }
}