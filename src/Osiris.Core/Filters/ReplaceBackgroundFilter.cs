using System.Collections.Generic;
using System.Threading;
using Osiris.Core.Imaging;
using Osiris.Core.Plugins;

namespace Osiris.Core.Filters
{
    /// <summary>
    /// 换底色滤镜（2.0 替代 1.x Prepalg.ReplaceBackground）：
    /// 色键算法 + 边缘羽化。以四角采样色为基准，容差内像素替换为目标色，
    /// 容差~容差+羽化带之间的过渡像素按距离线性混合，消除头发边缘白边。
    /// 新背景色 alpha=0 时输出透明背景（需 PNG 保存）。
    /// 参数：NewColor(BGRA int, 默认 0xFF0000FF 蓝色)、Tolerance(int, 默认 60)。
    /// </summary>
    public sealed class ReplaceBackgroundFilter : IFilterProcessor
    {
        public const string ParamColor = "NewColor";
        public const string ParamTolerance = "Tolerance";
        /// <summary>边缘羽化带宽度（与 1.x 一致）。</summary>
        private const int Feather = 30;

        public string Id => "fptp.builtin.replaceBackground";
        public string DisplayName => "换底色";

        public FilterParameters Defaults => new FilterParameters
        {
            [ParamColor] = ColorUtil.PackBgra(0, 0, 255),   // 默认蓝色
            [ParamTolerance] = 60
        };

        /// <summary>参数描述：目标颜色（下拉）+ 容差（数值框），壳据此生成对话框。</summary>
        public IReadOnlyList<FilterParameterDescriptor> Parameters => new[]
        {
            new FilterParameterDescriptor
            {
                Key = ParamColor, Label = "目标颜色", Kind = FilterParameterKind.Color,
                Choices = new[] { "蓝色", "红色", "白色", "透明" },
                ChoiceValues = new object[]
                {
                    ColorUtil.PackBgra(0, 0, 255),
                    ColorUtil.PackBgra(255, 0, 0),
                    ColorUtil.PackBgra(255, 255, 255),
                    ColorUtil.PackBgra(0, 0, 0, 0)
                }
            },
            new FilterParameterDescriptor
            {
                Key = ParamTolerance, Label = "容差", Kind = FilterParameterKind.Int,
                Min = 0, Max = 150
            }
        };

        public PixelSurface Apply(PixelSurface input, FilterParameters p, IProgress progress, CancellationToken ct)
        {
            int newColor = p.Get(ParamColor, (int)Defaults[ParamColor]);
            int tolerance = p.Get(ParamTolerance, 60);
            if (tolerance < 0) tolerance = 0;
            if (tolerance > 150) tolerance = 150;

            var src = input.Data;
            var output = new PixelSurface(input.Width, input.Height);
            var dst = output.Data;
            int len = input.Width * input.Height;

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

                if (diff < tolerance)
                {
                    if (transparent)
                    {
                        // 透明目标：alpha 清零（保留原色，用于后续叠加）
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg; dst[offset + 2] = (byte)pb; dst[offset + 3] = 0;
                    }
                    else
                    {
                        dst[offset] = (byte)nb; dst[offset + 1] = (byte)ng;
                        dst[offset + 2] = (byte)nr; dst[offset + 3] = 255;
                    }
                }
                else if (diff < tolerance + Feather)
                {
                    // 边缘羽化：按距离线性混合原色与新背景色
                    double t = (double)(tolerance + Feather - diff) / Feather;
                    if (transparent)
                    {
                        byte a = (byte)(255 * (1 - t));
                        dst[offset] = (byte)pr; dst[offset + 1] = (byte)pg;
                        dst[offset + 2] = (byte)pb; dst[offset + 3] = a;
                    }
                    else
                    {
                        dst[offset] = (byte)(pb + (nb - pb) * t);
                        dst[offset + 1] = (byte)(pg + (ng - pg) * t);
                        dst[offset + 2] = (byte)(pr + (nr - pr) * t);
                        dst[offset + 3] = 255;
                    }
                }
                else
                {
                    dst[offset] = src[offset]; dst[offset + 1] = src[offset + 1];
                    dst[offset + 2] = src[offset + 2]; dst[offset + 3] = src[offset + 3];
                }
            }

            if (progress != null) progress.Report(1.0, "换底色完成");
            return output;
        }

        private static int ReadBgra(byte[] data, int pixelIndex)
        {
            int o = pixelIndex * 4;
            return data[o] | (data[o + 1] << 8) | (data[o + 2] << 16) | (data[o + 3] << 24);
        }
    }
}
