using Osiris.Core.Imaging;

namespace Osiris.Core.Document
{
    /// <summary>单个图层：像素真相源为 PixelSurface（非 SKBitmap）。</summary>
    public sealed class Layer
    {
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        public float Opacity { get; set; } = 1f;
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public PixelSurface Pixels { get; }

        public Layer(string name, int width, int height)
        {
            Name = name;
            Pixels = new PixelSurface(width, height);
        }
    }
}
