using System;
using System.Collections.Generic;
using Osiris.Core.Imaging;

namespace Osiris.Core.Imaging
{
    /// <summary>
    /// 证件照排版处理器（2.0 替代 1.x GenSettings 的排版能力，命名即职责）：
    /// 把照片网格居中排列到相纸（5寸/6寸/A4/A5/自定义），可带虚线裁剪辅助线。
    /// 纯 PixelSurface 合成，不依赖渲染后端。
    /// </summary>
    public static class LayoutProcessor
    {
        // 相纸预设（1.x 尺寸：5寸 1500x1050 / 6寸 1800x1200 / A4 3508x2480 / A5 2480x1748）
        public static readonly IReadOnlyDictionary<string, (int W, int H)> PaperPresets =
            new Dictionary<string, (int, int)>
            {
                ["5寸"] = (1500, 1050),
                ["6寸"] = (1800, 1200),
                ["A4"] = (3508, 2480),
                ["A5"] = (2480, 1748)
            };

        /// <summary>照片间隙（像素，与 1.x 一致）。</summary>
        public const int Gap = 40;

        /// <summary>辅助线样式。</summary>
        public enum GuideLineStyle { None = 0, Dash = 1, Solid = 2 }

        /// <summary>排版结果。</summary>
        public sealed class LayoutResult
        {
            public PixelSurface Paper { get; set; }
            public int Columns { get; set; }
            public int Rows { get; set; }
            public int Count => Columns * Rows;
        }

        /// <summary>
        /// 排版单张照片到相纸：网格居中排列，可画辅助线。
        /// </summary>
        public static LayoutResult Layout(PixelSurface photo, int paperWidth, int paperHeight,
                                          GuideLineStyle guideLine = GuideLineStyle.Dash)
        {
            if (photo == null) throw new ArgumentNullException(nameof(photo));
            if (paperWidth <= 0 || paperHeight <= 0) throw new ArgumentOutOfRangeException(nameof(paperWidth), "相纸尺寸必须为正");

            // 照片大于相纸时先等比缩小到相纸内（否则居中起点为负，BlockCopy 越界崩溃）
            if (photo.Width > paperWidth || photo.Height > paperHeight)
            {
                double scale = Math.Min((double)paperWidth / photo.Width,
                                        (double)paperHeight / photo.Height);
                int sw = Math.Max(1, (int)(photo.Width * scale));
                int sh = Math.Max(1, (int)(photo.Height * scale));
                photo = ScaleBilinear(photo, sw, sh);
            }

            int photoW = photo.Width, photoH = photo.Height;
            int cols = Math.Max(1, (paperWidth + Gap) / (photoW + Gap));
            int rows = Math.Max(1, (paperHeight + Gap) / (photoH + Gap));

            int contentWidth = cols * photoW + (cols - 1) * Gap;
            int contentHeight = rows * photoH + (rows - 1) * Gap;
            int startX = (paperWidth - contentWidth) / 2;
            int startY = (paperHeight - contentHeight) / 2;

            var paper = new PixelSurface(paperWidth, paperHeight);
            FillWhite(paper);

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                int x = startX + c * (photoW + Gap);
                int y = startY + r * (photoH + Gap);
                CopyPhoto(paper, photo, x, y);
                if (guideLine != GuideLineStyle.None)
                    DrawGuideRect(paper, x, y, photoW, photoH, guideLine == GuideLineStyle.Solid);
            }

            return new LayoutResult { Paper = paper, Columns = cols, Rows = rows };
        }

        /// <summary>按预设相纸排版。</summary>
        public static LayoutResult LayoutPreset(PixelSurface photo, string paperName, GuideLineStyle guideLine = GuideLineStyle.Dash)
        {
            if (!PaperPresets.TryGetValue(paperName, out var size))
                throw new ArgumentException("未知相纸预设: " + paperName, nameof(paperName));
            return Layout(photo, size.W, size.H, guideLine);
        }

        private static void FillWhite(PixelSurface paper)
        {
            var d = paper.Data;
            for (int i = 0; i < d.Length; i += 4)
            {
                d[i] = 255; d[i + 1] = 255; d[i + 2] = 255; d[i + 3] = 255;
            }
        }

        /// <summary>整块拷贝照片到相纸（逐行 BlockCopy）。</summary>
        private static void CopyPhoto(PixelSurface paper, PixelSurface photo, int dstX, int dstY)
        {
            var src = photo.Data;
            var dst = paper.Data;
            var rowBytes = photo.Width * 4;
            for (int r = 0; r < photo.Height; r++)
            {
                var srcOffset = r * photo.Stride;
                var dstOffset = ((dstY + r) * paper.Stride) + (dstX * 4);
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, rowBytes);
            }
        }

        /// <summary>双线性缩放照片（照片大于相纸时降采样，BGRA 各通道插值）。</summary>
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

        /// <summary>画裁剪辅助线（浅灰 1px 矩形边框）。</summary>
        private static void DrawGuideRect(PixelSurface paper, int x, int y, int w, int h, bool solid)
        {
            byte shade = 200;
            int step = solid ? 1 : 8;   // 虚线：8px 段

            // 四条边，逐段画
            for (int px = x; px < x + w; px += 1)
            {
                if (!solid && (px - x) % step >= step / 2) continue;
                SetPixel(paper, px, y, shade);
                SetPixel(paper, px, y + h - 1, shade);
            }
            for (int py = y; py < y + h; py += 1)
            {
                if (!solid && (py - y) % step >= step / 2) continue;
                SetPixel(paper, x, py, shade);
                SetPixel(paper, x + w - 1, py, shade);
            }
        }

        private static void SetPixel(PixelSurface surface, int x, int y, byte shade)
        {
            if (x < 0 || y < 0 || x >= surface.Width || y >= surface.Height) return;
            int o = (y * surface.Stride) + (x * 4);
            surface.Data[o] = shade;
            surface.Data[o + 1] = shade;
            surface.Data[o + 2] = shade;
            surface.Data[o + 3] = 255;
        }
    }
}
