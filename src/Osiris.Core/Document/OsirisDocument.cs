using System.Collections.Generic;

namespace Osiris.Core.Document
{
    /// <summary>文档：图层集合 + 撤销历史 + 选区。</summary>
    public sealed class OsirisDocument
    {
        public int Width { get; }
        public int Height { get; }
        public List<Layer> Layers { get; } = new List<Layer>();
        public History.HistoryStack History { get; } = new History.HistoryStack();
        /// <summary>当前选区（全不选初始；工具写入、滤镜读取）。</summary>
        public Selection Selection { get; }

        public OsirisDocument(int width, int height)
        {
            Width = width;
            Height = height;
            Selection = new Selection(width, height);
        }
    }
}
