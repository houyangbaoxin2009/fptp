using System;
using Osiris.Core.Document;
using Osiris.Core.Imaging;

namespace Osiris.Core.History
{
    /// <summary>
    /// 像素编辑命令：记录受影响区域 before/after 快照，撤销恢复、重做重放。
    /// 区域快照（非整图）——历史栈内存上限 + 每命令仅存区域，控内存。
    /// </summary>
    public sealed class PixelEditCommand : IUndoableCommand
    {
        private readonly Layer _layer;
        private readonly int _x, _y, _width, _height;
        private readonly byte[] _before;
        private readonly byte[] _after;
        private readonly int _rowBytes;

        public string Name { get; }

        /// <summary>
        /// 构造前：执行方已把新像素写入 layer（或经 after 提供），本命令构造时快照 before。
        /// </summary>
        public PixelEditCommand(string name, Layer layer, int x, int y, int width, int height, byte[] after)
        {
            Name = name;
            _layer = layer;
            _x = x; _y = y; _width = width; _height = height;
            _after = after;
            _rowBytes = width * 4;

            _before = new byte[height * _rowBytes];
            var src = layer.Pixels.Data;
            var srcStride = layer.Pixels.Stride;
            var srcOffset = (y * srcStride) + (x * 4);
            for (int r = 0; r < height; r++)
                Buffer.BlockCopy(src, srcOffset + r * srcStride, _before, r * _rowBytes, _rowBytes);
        }

        public void Execute(OsirisDocument doc) => Write(_after);

        public void Undo(OsirisDocument doc) => Write(_before);

        public void Redo(OsirisDocument doc) => Write(_after);

        /// <summary>把快照写回图层像素（同名区域）。</summary>
        private void Write(byte[] data)
        {
            var dst = _layer.Pixels.Data;
            var dstStride = _layer.Pixels.Stride;
            var dstOffset = (_y * dstStride) + (_x * 4);
            for (int r = 0; r < _height; r++)
                Buffer.BlockCopy(data, r * _rowBytes, dst, dstOffset + r * dstStride, _rowBytes);
        }
    }
}
