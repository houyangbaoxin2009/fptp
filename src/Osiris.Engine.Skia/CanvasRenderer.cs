using System;
using System.Runtime.InteropServices;
using Osiris.Core.Document;
using Osiris.Core.Imaging;
using SkiaSharp;

namespace Osiris.Engine.Skia
{
    /// <summary>画布渲染器：将文档合成到 SKCanvas（L0 合成层）。</summary>
    public sealed class CanvasRenderer
    {
        /// <summary>合成整份文档到指定尺寸画布。</summary>
        public SKBitmap Render(OsirisDocument doc)
        {
            var bmp = new SKBitmap(doc.Width, doc.Height);
            using (var canvas = new SKCanvas(bmp))
            {
                canvas.Clear(SKColors.White);
                foreach (var layer in doc.Layers)
                {
                    if (!layer.Visible || layer.Opacity <= 0f) continue;
                    var paint = new SKPaint { IsAntialias = true };
                    paint.Color = paint.Color.WithAlpha((byte)(layer.Opacity * 255));
                    paint.BlendMode = MapBlend(layer.BlendMode);
                    using (paint)
                    {
                        var info = new SKImageInfo(layer.Pixels.Width, layer.Pixels.Height,
                                                   SKColorType.Bgra8888, SKAlphaType.Premul);
                        var handle = GCHandle.Alloc(layer.Pixels.Data, GCHandleType.Pinned);
                        try
                        {
                            using var img = SKImage.FromPixels(info, handle.AddrOfPinnedObject(), layer.Pixels.Stride);
                            canvas.DrawImage(img, layer.OffsetX, layer.OffsetY, paint);
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                }
            }
            return bmp;
        }

        private static SKBlendMode MapBlend(Osiris.Core.Imaging.BlendMode mode)
        {
            switch (mode)
            {
                case Osiris.Core.Imaging.BlendMode.Multiply: return SKBlendMode.Multiply;
                case Osiris.Core.Imaging.BlendMode.Screen: return SKBlendMode.Screen;
                case Osiris.Core.Imaging.BlendMode.Overlay: return SKBlendMode.Overlay;
                case Osiris.Core.Imaging.BlendMode.Darken: return SKBlendMode.Darken;
                case Osiris.Core.Imaging.BlendMode.Lighten: return SKBlendMode.Lighten;
                case Osiris.Core.Imaging.BlendMode.ColorDodge: return SKBlendMode.ColorDodge;
                case Osiris.Core.Imaging.BlendMode.ColorBurn: return SKBlendMode.ColorBurn;
                case Osiris.Core.Imaging.BlendMode.HardLight: return SKBlendMode.HardLight;
                case Osiris.Core.Imaging.BlendMode.SoftLight: return SKBlendMode.SoftLight;
                case Osiris.Core.Imaging.BlendMode.Difference: return SKBlendMode.Difference;
                case Osiris.Core.Imaging.BlendMode.Exclusion: return SKBlendMode.Exclusion;
                case Osiris.Core.Imaging.BlendMode.Hue: return SKBlendMode.Hue;
                case Osiris.Core.Imaging.BlendMode.Saturation: return SKBlendMode.Saturation;
                case Osiris.Core.Imaging.BlendMode.Color: return SKBlendMode.Color;
                case Osiris.Core.Imaging.BlendMode.Luminosity: return SKBlendMode.Luminosity;
                default: return SKBlendMode.SrcOver;
            }
        }
    }
}
