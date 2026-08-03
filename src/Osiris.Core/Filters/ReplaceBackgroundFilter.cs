using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Osiris.Core.Imaging;
using Osiris.Core.IO;
using Osiris.Core.Plugins;

namespace Osiris.Core.Filters
{
    /// <summary>
    /// 换底色滤镜（2.0 替代 1.x Prepalg.ReplaceBackground）：
    /// 两种模式：
    /// - 正常照片：色键算法。四角采样背景色，容差内替换，大羽化带消除头发白边。
    /// - 动漫角色：边缘感知 flood fill。从边框蔓延标记背景连通域，遇到描边线/强边缘即停，
    ///   适配硬边色块与描边线，复杂背景（门/场景）也能整体抠出，角色主体不被误换。
    /// 新背景可为任意颜色（ColorPicker）或图片（BackgroundImage 路径，经 CodecRegistry 解码铺满）。
    /// 新背景色 alpha=0 时输出透明背景（需 PNG 保存）。
    /// 参数：Mode(Choice)、NewColor(BGRA int, 默认蓝)、Tolerance(int, 默认 60)、
    ///       EdgeThreshold(int, 动漫描边阈值 默认 90)、BackgroundImage(string, 可选)。
    /// </summary>
    public sealed class ReplaceBackgroundFilter : IFilterProcessor
    {
        public const string ParamMode = "Mode";
        public const string ParamColor = "NewColor";
        public const string ParamTolerance = "Tolerance";
        /// <summary>动漫模式描边阈值（Sobel 幅值上限，越小越保守越容易停）。</summary>
        public const string ParamEdgeThreshold = "EdgeThreshold";
        /// <summary>背景图片路径参数（设置后以图片铺底，优先于纯色）。</summary>
        public const string ParamBackgroundImage = "BackgroundImage";
        /// <summary>正常模式边缘羽化带宽度（与 1.x 一致）。</summary>
        private const int Feather = 30;
        /// <summary>动漫模式边缘羽化带宽度（硬边，小羽化保护描边线）。</summary>
        private const int FeatherAnime = 4;

        public string Id => "fptp.builtin.replaceBackground";
        public string DisplayName => "换底色";

        public FilterParameters Defaults => new FilterParameters
        {
            [ParamMode] = "Normal",  // 默认正常照片模式
            [ParamColor] = ColorUtil.PackBgra(0, 0, 255),   // 默认蓝色
            [ParamTolerance] = 60,
            [ParamEdgeThreshold] = 90
        };

        /// <summary>参数描述：模式 + 任意颜色（ColorPicker）+ 背景图片（可选）+ 容差 + 描边阈值。</summary>
        public IReadOnlyList<FilterParameterDescriptor> Parameters => new[]
        {
            new FilterParameterDescriptor
            {
                Key = ParamMode, Label = "模式", Kind = FilterParameterKind.Choice,
                Choices = new[] { "正常照片", "动漫角色" },
                ChoiceValues = new object[] { "Normal", "Anime" }
            },
            new FilterParameterDescriptor
            {
                Key = ParamColor, Label = "目标颜色", Kind = FilterParameterKind.ColorPicker
            },
            new FilterParameterDescriptor
            {
                Key = ParamBackgroundImage, Label = "背景图片", Kind = FilterParameterKind.Image
            },
            new FilterParameterDescriptor
            {
                Key = ParamTolerance, Label = "容差", Kind = FilterParameterKind.Int,
                Min = 0, Max = 150
            },
            new FilterParameterDescriptor
            {
                Key = ParamEdgeThreshold, Label = "描边阈值", Kind = FilterParameterKind.Int,
                Min = 0, Max = 300
            }
        };

        public PixelSurface Apply(PixelSurface input, FilterParameters p, IProgress progress, CancellationToken ct)
        {
            bool anime = string.Equals(p.Get(ParamMode, "Normal"), "Anime", StringComparison.OrdinalIgnoreCase);
            int newColor = p.Get(ParamColor, (int)Defaults[ParamColor]);
            int tolerance = p.Get(ParamTolerance, 60);
            if (tolerance < 0) tolerance = 0;
            if (tolerance > 150) tolerance = 150;
            int edgeThreshold = p.Get(ParamEdgeThreshold, 90);
            if (edgeThreshold < 0) edgeThreshold = 0;
            if (edgeThreshold > 300) edgeThreshold = 300;

            // 背景图片：经 CodecRegistry 解码并双线性缩放到输入尺寸；失败则回退纯色
            PixelSurface bgImage = LoadBackgroundImage(p.Get<string>(ParamBackgroundImage, null), input);

            var result = anime
                ? ApplyAnime(input, newColor, tolerance, edgeThreshold, bgImage, progress, ct)
                : ApplyChromaKey(input, newColor, tolerance, bgImage, progress, ct);
            return result;
        }

        /// <summary>正常照片模式：四角采样 + 色键替换 + 大羽化。</summary>
        private static PixelSurface ApplyChromaKey(PixelSurface input, int newColor, int tolerance,
                                                   PixelSurface bgImage, IProgress progress, CancellationToken ct)
        {
            var src = input.Data;
            var output = new PixelSurface(input.Width, input.Height);
            var dst = output.Data;
            int len = input.Width * input.Height;
            var bgData = bgImage?.Data;

            // 采样背景色：四角取最接近色，避免单角被主体占据导致误采样
            int sample = ColorUtil.MostCommonCorner(
                ReadBgra(src, 0),
                ReadBgra(src, input.Width - 1),
                ReadBgra(src, (input.Height - 1) * input.Width),
                ReadBgra(src, input.Height * input.Width - 1));
            int sr = ColorUtil.R(sample), sg = ColorUtil.G(sample), sb = ColorUtil.B(sample);

            bool transparent = ColorUtil.A(newColor) == 0;
            int nr = ColorUtil.R(newColor), ng = ColorUtil.G(newColor), nb = ColorUtil.B(newColor);

            for (int i = 0; i < len; i++)
            {
                ct.ThrowIfCancellationRequested();
                int offset = i * 4;
                int pr = src[offset + 2], pg = src[offset + 1], pb = src[offset];
                int diff = System.Math.Abs(pr - sr) + System.Math.Abs(pg - sg) + System.Math.Abs(pb - sb);

                // 取背景目标像素：图片背景取同坐标像素，否则取目标色
                int br = nr, bg = ng, bb = nb, ba = transparent ? 0 : 255;
                if (bgData != null)
                {
                    br = bgData[offset + 2]; bg = bgData[offset + 1]; bb = bgData[offset]; ba = 255;
                }

                if (diff < tolerance)
                {
                    if (transparent && bgData == null)
                    {
                        // 透明目标：alpha 清零（保留原色，用于后续叠加）
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg; dst[offset + 2] = (byte)pb; dst[offset + 3] = 0;
                    }
                    else
                    {
                        dst[offset] = (byte)bb; dst[offset + 1] = (byte)bg;
                        dst[offset + 2] = (byte)br; dst[offset + 3] = (byte)ba;
                    }
                }
                else if (diff < tolerance + Feather)
                {
                    // 边缘羽化：按距离线性混合原色与新背景色
                    double t = (double)(tolerance + Feather - diff) / Feather;
                    if (transparent && bgData == null)
                    {
                        byte a = (byte)(255 * (1 - t));
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg;
                        dst[offset + 2] = (byte)pb; dst[offset + 3] = a;
                    }
                    else
                    {
                        dst[offset] = (byte)(pb + (bb - pb) * t);
                        dst[offset + 1] = (byte)(pg + (bg - pg) * t);
                        dst[offset + 2] = (byte)(pr + (br - pr) * t);
                        dst[offset + 3] = (byte)(255 * (1 - t) + ba * t);
                    }
                }
                else
                {
                    dst[offset] = src[offset]; dst[offset + 1] = src[offset + 1];
                    dst[offset + 2] = src[offset + 2]; dst[offset + 3] = src[offset + 3];
                }
            }

            if (progress != null) progress.Report(1.0, bgData != null ? "换底(图片)完成" : "换底色完成");
            return output;
        }

        /// <summary>
        /// 动漫角色模式：边缘感知 flood fill 抠图。
        /// 边框像素（颜色接近边框众数背景色）为种子，向内部蔓延标记背景连通域；
        /// 蔓延条件：与已标记像素颜色差异在容差内 且 目标像素 Sobel 边缘强度低于描边阈值。
        /// 描边线/强边缘阻断蔓延 → 角色主体（含闭包内的同色区域）不被误换；
        /// 背景连通域（含门/场景细节）整体替换。
        /// </summary>
        private static PixelSurface ApplyAnime(PixelSurface input, int newColor, int tolerance,
                                               int edgeThreshold, PixelSurface bgImage,
                                               IProgress progress, CancellationToken ct)
        {
            int w = input.Width, h = input.Height;
            var src = input.Data;
            var bgData = bgImage?.Data;
            int len = w * h;

            // 1. 边框众数色作为背景参考色
            int sample = SampleEdgeMode(src, w, h);
            int sr = ColorUtil.R(sample), sg = ColorUtil.G(sample), sb = ColorUtil.B(sample);

            // 2. Sobel 边缘图（梯度幅值）
            var edge = ComputeEdgeMap(src, w, h, ct);

            // 3. flood fill：数组栈 DFS，标记背景像素
            var visited = new bool[len];
            var stack = new int[len];
            int top = 0;

            // 种子：四边框像素中颜色接近背景参考色的（贴边角色像素不作种子）
            void PushSeed(int x, int y)
            {
                int idx = y * w + x;
                if (visited[idx]) return;
                int o = idx * 4;
                int d = Math.Abs(src[o + 2] - sr) + Math.Abs(src[o + 1] - sg) + Math.Abs(src[o] - sb);
                if (d < tolerance * 2 + 30)
                {
                    visited[idx] = true;
                    stack[top++] = idx;
                }
            }

            for (int x = 0; x < w; x++) { PushSeed(x, 0); PushSeed(x, h - 1); }
            for (int y = 1; y < h - 1; y++) { PushSeed(0, y); PushSeed(w - 1, y); }

            while (top > 0)
            {
                ct.ThrowIfCancellationRequested();
                int idx = stack[--top];
                int x = idx % w, y = idx / w;

                // 蔓延：4 邻域
                TrySpread(idx, x - 1, y, w, h, visited, stack, ref top, src, edge, tolerance, edgeThreshold);
                TrySpread(idx, x + 1, y, w, h, visited, stack, ref top, src, edge, tolerance, edgeThreshold);
                TrySpread(idx, x, y - 1, w, h, visited, stack, ref top, src, edge, tolerance, edgeThreshold);
                TrySpread(idx, x, y + 1, w, h, visited, stack, ref top, src, edge, tolerance, edgeThreshold);
            }

            // 4. 替换：背景像素换新背景；边缘带小羽化
            bool transparent = ColorUtil.A(newColor) == 0;
            int nr = ColorUtil.R(newColor), ng = ColorUtil.G(newColor), nb = ColorUtil.B(newColor);
            var output = new PixelSurface(w, h);
            var dst = output.Data;

            for (int i = 0; i < len; i++)
            {
                ct.ThrowIfCancellationRequested();
                int offset = i * 4;
                int pr = src[offset + 2], pg = src[offset + 1], pb = src[offset];
                int br = nr, bg = ng, bb = nb, ba = transparent ? 0 : 255;
                if (bgData != null)
                {
                    br = bgData[offset + 2]; bg = bgData[offset + 1]; bb = bgData[offset]; ba = 255;
                }

                if (visited[i])
                {
                    if (transparent && bgData == null)
                    {
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg;
                        dst[offset + 2] = (byte)pb; dst[offset + 3] = 0;
                    }
                    else
                    {
                        dst[offset] = (byte)bb; dst[offset + 1] = (byte)bg;
                        dst[offset + 2] = (byte)br; dst[offset + 3] = (byte)ba;
                    }
                }
                else if (IsNearBackgroundBoundary(i, w, h, visited))
                {
                    // 背景/前景边界带：小羽化，混合原色与新背景（保护描边线，避免生硬锯齿）
                    double t = 0.5;
                    if (transparent && bgData == null)
                    {
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg;
                        dst[offset + 2] = (byte)pb; dst[offset + 3] = (byte)(255 * (1 - t));
                    }
                    else
                    {
                        dst[offset] = (byte)(pb + (bb - pb) * t);
                        dst[offset + 1] = (byte)(pg + (bg - pg) * t);
                        dst[offset + 2] = (byte)(pr + (br - pr) * t);
                        dst[offset + 3] = (byte)(255 * (1 - t) + ba * t);
                    }
                }
                else
                {
                    dst[offset] = src[offset]; dst[offset + 1] = src[offset + 1];
                    dst[offset + 2] = src[offset + 2]; dst[offset + 3] = src[offset + 3];
                }
            }

            if (progress != null) progress.Report(1.0, bgData != null ? "换底(动漫/图片)完成" : "换底(动漫)完成");
            return output;
        }

        /// <summary>flood fill 蔓延一步：目标像素未访问、与父像素颜色差在容差内、且边缘强度低于阈值才标记入栈。</summary>
        private static void TrySpread(int parent, int nx, int ny, int w, int h,
                                      bool[] visited, int[] stack, ref int top,
                                      byte[] src, byte[] edge, int tolerance, int edgeThreshold)
        {
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
            int child = ny * w + nx;
            if (visited[child]) return;

            int po = parent * 4, co = child * 4;
            int d = Math.Abs(src[po + 2] - src[co + 2])
                  + Math.Abs(src[po + 1] - src[co + 1])
                  + Math.Abs(src[po] - src[co]);
            // 颜色连续 且 不跨强边缘 → 视为背景延伸
            if (d < tolerance && edge[child] < edgeThreshold)
            {
                visited[child] = true;
                stack[top++] = child;
            }
        }

        /// <summary>目标像素是否紧邻已标记的背景像素（用于边界带羽化）。</summary>
        private static bool IsNearBackgroundBoundary(int idx, int w, int h, bool[] visited)
        {
            int x = idx % w, y = idx / w;
            if (x > 0 && visited[idx - 1]) return true;
            if (x < w - 1 && visited[idx + 1]) return true;
            if (y > 0 && visited[idx - w]) return true;
            if (y < h - 1 && visited[idx + w]) return true;
            return false;
        }

        /// <summary>Sobel 梯度幅值图（|Gx|+|Gy|，截断 255，BT.601 亮度）。</summary>
        private static byte[] ComputeEdgeMap(byte[] src, int w, int h, CancellationToken ct)
        {
            var edge = new byte[w * h];
            int stride = w * 4;
            for (int y = 0; y < h; y++)
            {
                ct.ThrowIfCancellationRequested();
                int rowBase = y * w;
                for (int x = 0; x < w; x++)
                {
                    int gx = SobelX(src, w, h, x, y, stride);
                    int gy = SobelY(src, w, h, x, y, stride);
                    int mag = Math.Abs(gx) + Math.Abs(gy);
                    edge[rowBase + x] = (byte)(mag > 255 ? 255 : mag);
                }
            }
            return edge;
        }

        private static int SobelX(byte[] data, int w, int h, int x, int y, int stride)
        {
            int p00 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
            int p10 = Luma(data, x, Clamp(y - 1, 0, h - 1), stride);
            int p20 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
            int p01 = Luma(data, Clamp(x - 1, 0, w - 1), y, stride);
            int p21 = Luma(data, Clamp(x + 1, 0, w - 1), y, stride);
            int p02 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
            int p12 = Luma(data, x, Clamp(y + 1, 0, h - 1), stride);
            int p22 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
            return -p00 - 2 * p01 - p02 + p20 + 2 * p21 + p22;
        }

        private static int SobelY(byte[] data, int w, int h, int x, int y, int stride)
        {
            int p00 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
            int p01 = Luma(data, Clamp(x - 1, 0, w - 1), y, stride);
            int p02 = Luma(data, Clamp(x - 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
            int p10 = Luma(data, x, Clamp(y - 1, 0, h - 1), stride);
            int p12 = Luma(data, x, Clamp(y + 1, 0, h - 1), stride);
            int p20 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y - 1, 0, h - 1), stride);
            int p21 = Luma(data, Clamp(x + 1, 0, w - 1), y, stride);
            int p22 = Luma(data, Clamp(x + 1, 0, w - 1), Clamp(y + 1, 0, h - 1), stride);
            return -p00 - 2 * p10 - p20 + p02 + 2 * p12 + p22;
        }

        private static int Luma(byte[] data, int x, int y, int stride)
        {
            int o = y * stride + x * 4;
            return (data[o + 2] * 299 + data[o + 1] * 587 + data[o] * 114) / 1000;
        }

        /// <summary>
        /// 边框一圈颜色众数采样（动漫模式背景参考色）：统计上/下/左/右边缘像素的量化颜色直方图，
        /// 取出现次数最多的颜色中心值。比四角采样更抗噪，角色占据边角也不易误采样。
        /// </summary>
        private static int SampleEdgeMode(byte[] data, int w, int h)
        {
            var counts = new Dictionary<int, int>();
            // 量化到 16 级/通道再统计，抗抗锯齿噪点
            void Scan(int x, int y)
            {
                int o = y * w * 4 + x * 4;
                int key = ((data[o + 2] >> 4) << 8) | ((data[o + 1] >> 4) << 4) | (data[o] >> 4);
                counts.TryGetValue(key, out int c);
                counts[key] = c + 1;
            }

            for (int x = 0; x < w; x++) { Scan(x, 0); Scan(x, h - 1); }
            for (int y = 1; y < h - 1; y++) { Scan(0, y); Scan(w - 1, y); }

            int bestKey = 0, bestCount = -1;
            foreach (var kv in counts)
                if (kv.Value > bestCount) { bestCount = kv.Value; bestKey = kv.Key; }

            // 反量化到量化格中心
            int br = ((bestKey >> 8) & 0xF) * 16 + 8;
            int bg = ((bestKey >> 4) & 0xF) * 16 + 8;
            int bb = (bestKey & 0xF) * 16 + 8;
            return ColorUtil.PackBgra((byte)br, (byte)bg, (byte)bb);
        }

        /// <summary>经 CodecRegistry 解码背景图片并双线性缩放到输入尺寸；路径无效/无法解码返回 null（回退纯色）。</summary>
        private static PixelSurface LoadBackgroundImage(string path, PixelSurface input)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var ext = Path.GetExtension(path);
                var importer = CodecRegistry.FindImporter(ext);
                if (importer == null) return null;
                PixelSurface loaded;
                using (var fs = File.OpenRead(path))
                    loaded = importer.Read(fs, ext);
                if (loaded == null) return null;

                if (loaded.Width == input.Width && loaded.Height == input.Height)
                    return loaded;

                return ScaleBilinear(loaded, input.Width, input.Height);
            }
            catch
            {
                return null; // 解码失败不阻断换底，回退纯色
            }
        }

        /// <summary>双线性缩放背景图到输入尺寸（BGRA 逐像素插值）。</summary>
        private static PixelSurface ScaleBilinear(PixelSurface src, int outW, int outH)
        {
            var output = new PixelSurface(outW, outH);
            var srcData = src.Data;
            var dstData = output.Data;
            double scaleX = (double)src.Width / outW;
            double scaleY = (double)src.Height / outH;

            for (int y = 0; y < outH; y++)
            {
                double sy = (y + 0.5) * scaleY - 0.5;
                int y0 = Clamp((int)Math.Floor(sy), 0, src.Height - 1);
                int y1 = Clamp(y0 + 1, 0, src.Height - 1);
                double fy = Clamp01(sy - y0);

                for (int x = 0; x < outW; x++)
                {
                    double sx = (x + 0.5) * scaleX - 0.5;
                    int x0 = Clamp((int)Math.Floor(sx), 0, src.Width - 1);
                    int x1 = Clamp(x0 + 1, 0, src.Width - 1);
                    double fx = Clamp01(sx - x0);

                    int o = (y * outW + x) * 4;
                    for (int c = 0; c < 4; c++)
                    {
                        double v00 = srcData[(y0 * src.Width + x0) * 4 + c];
                        double v10 = srcData[(y0 * src.Width + x1) * 4 + c];
                        double v01 = srcData[(y1 * src.Width + x0) * 4 + c];
                        double v11 = srcData[(y1 * src.Width + x1) * 4 + c];
                        double top = v00 + (v10 - v00) * fx;
                        double bot = v01 + (v11 - v01) * fx;
                        dstData[o + c] = (byte)(top + (bot - top) * fy);
                    }
                }
            }
            return output;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static int ReadBgra(byte[] data, int pixelIndex)
        {
            int o = pixelIndex * 4;
            return data[o] | (data[o + 1] << 8) | (data[o + 2] << 16) | (data[o + 3] << 24);
        }
    }
}
