using System;
using System.Collections.Generic;
using System.Threading;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Core.Filters
{
    /// <summary>
    /// 动漫模式滤镜：照片转二次元卡通风格。
    /// 三步：轻度平滑去噪 → 颜色量化（减少色彩层次形成色块）→ 边缘检测描边（粗黑线勾轮廓）。
    /// 纯 PixelSurface 实现，不依赖渲染后端。
    /// 参数：Levels(量化级数, 默认 8)、Outline(描边强度, 默认 60)、Smooth(平滑半径, 默认 1)。
    /// </summary>
    public sealed class AnimeFilter : IFilterProcessor
    {
        public const string ParamLevels = "Levels";
        public const string ParamOutline = "Outline";
        public const string ParamSmooth = "Smooth";

        public string Id => "fptp.builtin.anime";
        public string DisplayName => "动漫模式";

        public FilterParameters Defaults => new FilterParameters
        {
            [ParamLevels] = 8,      // 每通道量化级数（越大层次越丰富）
            [ParamOutline] = 60,    // 边缘梯度阈值（越小描边越多）
            [ParamSmooth] = 1       // 平滑半径（0 关闭）
        };

        /// <summary>参数描述：量化级数 + 描边强度 + 平滑半径（数值框），壳据此生成对话框。</summary>
        public IReadOnlyList<FilterParameterDescriptor> Parameters => new[]
        {
            new FilterParameterDescriptor
            {
                Key = ParamLevels, Label = "色彩层次", Kind = FilterParameterKind.Int,
                Min = 2, Max = 16
            },
            new FilterParameterDescriptor
            {
                Key = ParamOutline, Label = "描边强度", Kind = FilterParameterKind.Int,
                Min = 0, Max = 200
            },
            new FilterParameterDescriptor
            {
                Key = ParamSmooth, Label = "平滑半径", Kind = FilterParameterKind.Int,
                Min = 0, Max = 3
            }
        };

        public PixelSurface Apply(PixelSurface input, FilterParameters p, IProgress progress, CancellationToken ct)
        {
            int levels = p.Get(ParamLevels, 8);
            int outline = p.Get(ParamOutline, 60);
            int smooth = p.Get(ParamSmooth, 1);
            if (levels < 2) levels = 2;
            if (levels > 16) levels = 16;
            if (outline < 0) outline = 0;
            if (smooth < 0) smooth = 0;
            if (smooth > 3) smooth = 3;

            int w = input.Width, h = input.Height;
            var src = input.Data;

            // 1. 轻度平滑（盒式模糊，仅当 radius > 0）
            var working = smooth > 0 ? BoxBlur(src, w, h, smooth, ct) : src;

            // 2. 颜色量化：每通道映射到 levels 级色块（消除渐变 → 卡通色块）
            int step = 256 / levels;
            var quantized = new byte[src.Length];
            for (int i = 0; i < src.Length; i += 4)
            {
                ct.ThrowIfCancellationRequested();
                quantized[i] = (byte)((working[i] / step) * step + step / 2);
                quantized[i + 1] = (byte)((working[i + 1] / step) * step + step / 2);
                quantized[i + 2] = (byte)((working[i + 2] / step) * step + step / 2);
                quantized[i + 3] = working[i + 3];
            }

            // 3. 边缘检测描边：Sobel 梯度幅值超阈值 → 深灰/黑轮廓
            var output = new PixelSurface(w, h);
            var dst = output.Data;
            if (outline > 0)
            {
                int gradThreshold = outline * 3; // 梯度幅值放大系数
                for (int y = 0; y < h; y++)
                {
                    ct.ThrowIfCancellationRequested();
                    int rowBase = y * w * 4;
                    for (int x = 0; x < w; x++)
                    {
                        int o = rowBase + x * 4;
                        int gx = SobelX(quantized, w, h, x, y);
                        int gy = SobelY(quantized, w, h, x, y);
                        int mag = Math.Abs(gx) + Math.Abs(gy);

                        if (mag > gradThreshold)
                        {
                            // 描边：取暗色（保留原色 30% 亮度，像勾线）
                            dst[o] = (byte)(quantized[o] * 0.3);
                            dst[o + 1] = (byte)(quantized[o + 1] * 0.3);
                            dst[o + 2] = (byte)(quantized[o + 2] * 0.3);
                            dst[o + 3] = 255;
                        }
                        else
                        {
                            dst[o] = quantized[o]; dst[o + 1] = quantized[o + 1];
                            dst[o + 2] = quantized[o + 2]; dst[o + 3] = quantized[o + 3];
                        }
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(quantized, 0, dst, 0, quantized.Length);
            }

            if (progress != null) progress.Report(1.0, "动漫模式完成");
            return output;
        }

        /// <summary>盒式模糊（半径 r 的方形邻域平均），边界越界取最近像素。</summary>
        private static byte[] BoxBlur(byte[] src, int w, int h, int radius, CancellationToken ct)
        {
            int len = src.Length;
            var dst = new byte[len];
            int stride = w * 4;
            int win = (radius * 2 + 1);
            int winSize = win * win;

            for (int y = 0; y < h; y++)
            {
                ct.ThrowIfCancellationRequested();
                int y0 = Clamp(y - radius, 0, h - 1);
                int y1 = Clamp(y + radius, 0, h - 1);
                for (int x = 0; x < w; x++)
                {
                    int x0 = Clamp(x - radius, 0, w - 1);
                    int x1 = Clamp(x + radius, 0, w - 1);
                    int sumB = 0, sumG = 0, sumR = 0, sumA = 0;
                    for (int yy = y0; yy <= y1; yy++)
                    {
                        int rowOff = yy * stride;
                        for (int xx = x0; xx <= x1; xx++)
                        {
                            int po = rowOff + xx * 4;
                            sumB += src[po]; sumG += src[po + 1]; sumR += src[po + 2]; sumA += src[po + 3];
                        }
                    }
                    int o = y * stride + x * 4;
                    dst[o] = (byte)(sumB / winSize); dst[o + 1] = (byte)(sumG / winSize);
                    dst[o + 2] = (byte)(sumR / winSize); dst[o + 3] = (byte)(sumA / winSize);
                }
            }
            return dst;
        }

        /// <summary>Sobel X 梯度（亮度用 BT.601 加权）。</summary>
        private static int SobelX(byte[] data, int w, int h, int x, int y)
        {
            int stride = w * 4;
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

        /// <summary>Sobel Y 梯度。</summary>
        private static int SobelY(byte[] data, int w, int h, int x, int y)
        {
            int stride = w * 4;
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

        /// <summary>像素亮度（BT.601）。</summary>
        private static int Luma(byte[] data, int x, int y, int stride)
        {
            int o = y * stride + x * 4;
            return (data[o + 2] * 299 + data[o + 1] * 587 + data[o] * 114) / 1000;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
