using System;
using System.Threading;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Core.Filters
{
    /// <summary>
    /// 智能裁切滤镜（2.0 替代 1.x Prepalg.SmartCrop）：
    /// 按目标宽高比中心裁切源图，再高质量缩放（双线性插值）到目标尺寸。
    /// 证件照常用：宽高比决定裁左右还是裁上下，主体居中保留。
    /// 参数：Width(int)、Height(int)，默认 1 寸 295x413。
    /// </summary>
    public sealed class SmartCropFilter : IFilterProcessor
    {
        public const string ParamWidth = "Width";
        public const string ParamHeight = "Height";

        public string Id => "fptp.builtin.smartCrop";
        public string DisplayName => "智能裁切";

        public FilterParameters Defaults => new FilterParameters
        {
            [ParamWidth] = 295,     // 1 寸证件照
            [ParamHeight] = 413
        };

        public PixelSurface Apply(PixelSurface input, FilterParameters p, IProgress progress, CancellationToken ct)
        {
            int targetW = p.Get(ParamWidth, 295);
            int targetH = p.Get(ParamHeight, 413);
            if (targetW <= 0 || targetH <= 0) throw new ArgumentOutOfRangeException(nameof(p), "目标尺寸必须为正");

            // 中心裁切：比较宽高比决定裁左右还是裁上下
            double srcRatio = (double)input.Width / input.Height;
            double dstRatio = (double)targetW / targetH;

            int cropX = 0, cropY = 0, cropW = input.Width, cropH = input.Height;
            if (srcRatio > dstRatio)
            {
                cropW = (int)(input.Height * dstRatio);
                cropX = (input.Width - cropW) / 2;
            }
            else
            {
                cropH = (int)(input.Width / dstRatio);
                cropY = (input.Height - cropH) / 2;
            }

            return Resize(input, cropX, cropY, cropW, cropH, targetW, targetH, progress, ct);
        }

        /// <summary>裁剪源区并双线性缩放到目标尺寸（BGRA 预乘逐像素插值）。</summary>
        private static PixelSurface Resize(PixelSurface src, int cropX, int cropY,
                                           int cropW, int cropH, int outW, int outH,
                                           IProgress progress, CancellationToken ct)
        {
            var output = new PixelSurface(outW, outH);
            var srcData = src.Data;
            var dstData = output.Data;

            double scaleX = (double)cropW / outW;
            double scaleY = (double)cropH / outH;

            for (int y = 0; y < outH; y++)
            {
                ct.ThrowIfCancellationRequested();
                double sy = cropY + (y + 0.5) * scaleY - 0.5;
                int y0 = Clamp((int)Math.Floor(sy), 0, inputHeight(src) - 1);
                int y1 = Clamp(y0 + 1, 0, inputHeight(src) - 1);
                double fy = sy - y0;
                if (fy < 0) fy = 0; else if (fy > 1) fy = 1;

                for (int x = 0; x < outW; x++)
                {
                    double sx = cropX + (x + 0.5) * scaleX - 0.5;
                    int x0 = Clamp((int)Math.Floor(sx), 0, inputWidth(src) - 1);
                    int x1 = Clamp(x0 + 1, 0, inputWidth(src) - 1);
                    double fx = sx - x0;
                    if (fx < 0) fx = 0; else if (fx > 1) fx = 1;

                    // 双线性：2x2 邻域按权重混合（BGRA 各通道）
                    int o = (y * outW + x) * 4;
                    for (int c = 0; c < 4; c++)
                    {
                        double v00 = srcData[(y0 * inputWidth(src) + x0) * 4 + c];
                        double v10 = srcData[(y0 * inputWidth(src) + x1) * 4 + c];
                        double v01 = srcData[(y1 * inputWidth(src) + x0) * 4 + c];
                        double v11 = srcData[(y1 * inputWidth(src) + x1) * 4 + c];
                        double top = v00 + (v10 - v00) * fx;
                        double bot = v01 + (v11 - v01) * fx;
                        dstData[o + c] = (byte)(top + (bot - top) * fy);
                    }
                }
            }

            if (progress != null) progress.Report(1.0, "智能裁切完成");
            return output;
        }

        private static int inputWidth(PixelSurface s) => s.Width;
        private static int inputHeight(PixelSurface s) => s.Height;
        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
