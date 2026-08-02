using System;

namespace Osiris.Core.Imaging
{
    /// <summary>像素缓冲：BGRA 预乘 8bit，Span 访问，独立于渲染后端。</summary>
    public sealed class PixelSurface
    {
        private readonly byte[] _data;

        public int Width { get; }
        public int Height { get; }
        public int Stride => Width * 4;

        public PixelSurface(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "尺寸必须为正");
            Width = width;
            Height = height;
            _data = new byte[width * height * 4];
        }

        /// <summary>整幅像素（BGRA 预乘）。</summary>
        public Span<byte> Pixels => _data;

        /// <summary>底层数组（渲染零拷贝视图用）。</summary>
        public byte[] Data => _data;

        /// <summary>第 row 行的像素视图。</summary>
        public Span<byte> Row(int row)
        {
            if (row < 0 || row >= Height) throw new ArgumentOutOfRangeException(nameof(row));
            return _data.AsSpan(row * Stride, Stride);
        }
    }
}
