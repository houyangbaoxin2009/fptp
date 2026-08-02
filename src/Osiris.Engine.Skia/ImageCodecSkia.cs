using System;
using System.IO;
using Osiris.Core.Imaging;
using Osiris.Core.IO;
using SkiaSharp;

namespace Osiris.Engine.Skia
{
    /// <summary>基于 SkiaSharp 的图片编解码器（PNG/JPEG/BMP/WebP 等）。</summary>
    public sealed class ImageCodecSkia : IDocumentImporter, IDocumentExporter
    {
        public string Id => "osiris.codec.skia";
        public string[] Extensions { get; } = { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };

        public bool CanRead(string extension) => Matches(extension);
        public bool CanWrite(string extension) => Matches(extension);

        public PixelSurface Read(Stream stream, string extension)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var src = SKBitmap.Decode(stream);
            if (src == null) throw new InvalidDataException("无法解码图片: " + extension);

            var surface = new PixelSurface(src.Width, src.Height);
            CopyToPixelSurface(src, surface);
            return surface;
        }

        public void Write(PixelSurface surface, Stream stream, string extension)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var format = ToFormat(extension);
            using var bmp = CreateBitmap(surface);
            using var data = bmp.Encode(format, 90);
            if (data == null) throw new InvalidOperationException("图片编码失败: " + extension);
            data.SaveTo(stream);
        }

        /// <summary>SKBitmap → PixelSurface（BGRA 逐行拷贝）。</summary>
        private static void CopyToPixelSurface(SKBitmap src, PixelSurface dst)
        {
            var source = src;
            try
            {
                // 颜色格式不一致时一次性转换到 BGRA8888 预乘
                if (src.Info.ColorType != SKColorType.Bgra8888 || src.Info.AlphaType != SKAlphaType.Premul)
                {
                    source = src.Copy(SKColorType.Bgra8888);
                    if (source == null) throw new InvalidDataException("像素格式转换失败");
                }

                using (var pixmap = source.PeekPixels())
                {
                    var dstStride = dst.Stride;
                    var rowBytes = Math.Min(pixmap.RowBytes, dstStride);
                    for (int y = 0; y < source.Height; y++)
                    {
                        var srcPtr = pixmap.GetPixels() + y * pixmap.RowBytes;
                        System.Runtime.InteropServices.Marshal.Copy(srcPtr, dst.Data, y * dstStride, rowBytes);
                    }
                }
            }
            finally
            {
                if (!ReferenceEquals(source, src)) source?.Dispose();
            }
        }

        /// <summary>PixelSurface → SKBitmap（零拷贝视图，由调用方负责存活期）。</summary>
        private static SKBitmap CreateBitmap(PixelSurface surface)
        {
            var info = new SKImageInfo(surface.Width, surface.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var bmp = new SKBitmap(info);
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(surface.Data, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                bmp.InstallPixels(info, handle.AddrOfPinnedObject(), surface.Stride, null, null);
            }
            finally
            {
                handle.Free();
            }
            return bmp;
        }

        /// <summary>把合成位图（含全部图层）按扩展名编码保存到文件。</summary>
        public static void SaveComposite(SkiaSharp.SKBitmap bmp, string filePath)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            using var data = bmp.Encode(ToFormat(Path.GetExtension(filePath)), 90);
            if (data == null) throw new InvalidOperationException("图片编码失败: " + filePath);
            using var fs = File.Create(filePath);
            data.SaveTo(fs);
        }

        private bool Matches(string extension)
        {
            foreach (var ext in Extensions)
                if (string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static SKEncodedImageFormat ToFormat(string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg": return SKEncodedImageFormat.Jpeg;
                case ".bmp": return SKEncodedImageFormat.Bmp;
                case ".webp": return SKEncodedImageFormat.Webp;
                default: return SKEncodedImageFormat.Png;
            }
        }
    }
}
