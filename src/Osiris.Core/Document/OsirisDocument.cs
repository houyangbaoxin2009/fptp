using System.Collections.Generic;

namespace Osiris.Core.Document
{
    /// <summary>文档：图层集合 + 撤销历史。</summary>
    public sealed class OsirisDocument
    {
        public int Width { get; }
        public int Height { get; }
        public List<Layer> Layers { get; } = new List<Layer>();
        public History.HistoryStack History { get; } = new History.HistoryStack();

        public OsirisDocument(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
