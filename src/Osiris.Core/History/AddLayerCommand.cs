using System;
using Osiris.Core.Document;

namespace Osiris.Core.History
{
    /// <summary>
    /// 添加图层命令：把新图层加入文档（如排版结果图层）。
    /// 撤销移除、重做重新加入。
    /// </summary>
    public sealed class AddLayerCommand : IUndoableCommand
    {
        private readonly Layer _layer;

        public string Name { get; }

        public AddLayerCommand(string name, Layer layer)
        {
            Name = name;
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));
        }

        public void Execute(OsirisDocument doc)
        {
            if (!doc.Layers.Contains(_layer))
                doc.Layers.Add(_layer);
        }

        public void Undo(OsirisDocument doc) => doc.Layers.Remove(_layer);

        public void Redo(OsirisDocument doc)
        {
            if (!doc.Layers.Contains(_layer))
                doc.Layers.Add(_layer);
        }
    }
}
