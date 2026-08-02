using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Osiris.Core.Filters;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Core.Imaging
{
    /// <summary>批量处理选项（1.x BatchBox 能力：裁切/灰度/换底/排版 组合链）。</summary>
    public sealed class BatchOptions
    {
        public bool Crop;
        public bool Grayscale;
        public bool ReplaceBackground;
        /// <summary>排版相纸名（null = 不排版）。</summary>
        public string LayoutPaper;
    }

    /// <summary>批量处理结果。</summary>
    public sealed class BatchResult
    {
        public int Succeeded;
        public int Failed;
        /// <summary>输入图片总数。</summary>
        public int Total;
    }

    /// <summary>
    /// 批量处理：遍历输入目录图片，按选项依次执行 裁切/灰度/换底/排版，逐张失败不中断。
    /// IO 经委托注入（读图/写图），Core 零渲染后端依赖（CLI/App 各自提供 Skia 实现）。
    /// </summary>
    public static class BatchProcessor
    {
        /// <summary>支持的图片扩展名（不分大小写）。</summary>
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };

        /// <summary>执行批量处理。</summary>
        /// <param name="filters">全部可用滤镜（按 Id 后缀匹配，避免依赖顺序）。</param>
        /// <param name="readImage">读图：路径 → 像素面。</param>
        /// <param name="writeImage">写图：像素面 → 输出路径。</param>
        /// <param name="progress">进度回调（可为 null），每张完成后报告。</param>
        /// <param name="fileDone">逐张完成回调（可为 null）：文件名、是否成功、异常。</param>
        public static BatchResult Run(
            string inputDir, string outputDir, BatchOptions options,
            IReadOnlyList<IFilterProcessor> filters,
            Func<string, PixelSurface> readImage,
            Action<PixelSurface, string> writeImage,
            IProgress progress = null,
            Action<string, bool, Exception> fileDone = null)
        {
            var result = new BatchResult();

            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException("输入目录不存在: " + inputDir);
            Directory.CreateDirectory(outputDir);

            IFilterProcessor Find(string idSuffix)
            {
                foreach (var f in filters)
                    if (f.Id.EndsWith(idSuffix, StringComparison.OrdinalIgnoreCase))
                        return f;
                return null;
            }

            var crop = options.Crop ? Find("smartCrop") : null;
            var gray = options.Grayscale ? Find("grayscale") : null;
            var bg = options.ReplaceBackground ? Find("replaceBackground") : null;
            if (options.Crop && crop == null) throw new ArgumentException("未找到裁切滤镜");
            if (options.Grayscale && gray == null) throw new ArgumentException("未找到灰度滤镜");
            if (options.ReplaceBackground && bg == null) throw new ArgumentException("未找到换底色滤镜");
            if (options.LayoutPaper != null && !LayoutProcessor.PaperPresets.ContainsKey(options.LayoutPaper))
                throw new ArgumentException("未知相纸预设: " + options.LayoutPaper);

            var images = Directory.GetFiles(inputDir, "*.*")
                .Where(IsImage)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            result.Total = images.Length;
            if (images.Length == 0) return result;

            var ct = System.Threading.CancellationToken.None;
            for (int i = 0; i < images.Length; i++)
            {
                var file = images[i];
                var outFile = Path.Combine(outputDir,
                    Path.GetFileNameWithoutExtension(file) + ".png");
                try
                {
                    PixelSurface cur = readImage(file);
                    if (crop != null) cur = crop.Apply(cur, crop.Defaults, progress, ct);
                    if (gray != null) cur = gray.Apply(cur, gray.Defaults, progress, ct);
                    if (bg != null) cur = bg.Apply(cur, bg.Defaults, progress, ct);
                    if (options.LayoutPaper != null)
                        cur = LayoutProcessor.LayoutPreset(cur, options.LayoutPaper).Paper;

                    writeImage(cur, outFile);
                    result.Succeeded++;
                    fileDone?.Invoke(file, true, null);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    fileDone?.Invoke(file, false, ex);
                }
                progress?.Report((i + 1.0) / images.Length, Path.GetFileName(file));
            }

            return result;
        }

        /// <summary>是否为支持的图片文件。</summary>
        public static bool IsImage(string path)
        {
            var ext = Path.GetExtension(path);
            return ext != null && ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }
    }
}
