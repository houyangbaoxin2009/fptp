using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Osiris.Core.Document;
using Osiris.Core.Imaging;
using Osiris.Engine.Skia;

namespace Osiris.App
{
    /// <summary>主窗体：画布 + 占位菜单（M0 目标：加载图片、显示画布）。</summary>
    public sealed class MainForm : Form
    {
        private readonly OsirisDocument _doc;
        private readonly PictureBox _canvas;

        public MainForm()
        {
            Text = "Osiris 2.0";
            Size = new System.Drawing.Size(1000, 700);

            _doc = new OsirisDocument(640, 480);
            var layer = new Layer("背景", 640, 480);
            // 填充示例渐变，验证渲染管线
            for (int y = 0; y < 480; y++)
            {
                var row = layer.Pixels.Row(y);
                for (int x = 0; x < 640; x++)
                {
                    var i = x * 4;
                    row[i] = (byte)(x * 255 / 640);       // B
                    row[i + 1] = (byte)(y * 255 / 480);   // G
                    row[i + 2] = 128;                     // R
                    row[i + 3] = 255;                     // A
                }
            }
            _doc.Layers.Add(layer);

            _canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.DimGray
            };
            Controls.Add(_canvas);

            RefreshCanvas();
        }

        private void RefreshCanvas()
        {
            using (var bmp = new CanvasRenderer().Render(_doc))
            {
                var old = _canvas.Image;
                _canvas.Image = ToGdiBitmap(bmp);
                old?.Dispose();
            }
        }

        /// <summary>SKBitmap → GDI+ Bitmap（BGRA 与 Format32bppArgb 字节序一致，整行拷贝）。</summary>
        private static Bitmap ToGdiBitmap(SkiaSharp.SKBitmap sk)
        {
            var bmp = new Bitmap(sk.Width, sk.Height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, sk.Width, sk.Height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                var srcPtr = sk.GetPixels();
                var srcStride = sk.RowBytes;
                var dstStride = data.Stride;
                var bytesPerRow = sk.Width * 4;
                var rowBuf = new byte[bytesPerRow];
                for (int y = 0; y < sk.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(srcPtr, y * srcStride), rowBuf, 0, bytesPerRow);
                    Marshal.Copy(rowBuf, 0, IntPtr.Add(data.Scan0, y * dstStride), bytesPerRow);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }
    }
}
