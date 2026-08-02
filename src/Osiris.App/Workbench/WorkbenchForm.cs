using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Osiris.Core.Document;
using Osiris.Core.Ui;

namespace Osiris.App.Workbench
{
    /// <summary>壳：只负责渲染模组注册的 UI 资源，不含任何业务逻辑。</summary>
    public sealed class WorkbenchForm : Form
    {
        private readonly WorkbenchUiService _ui;
        private readonly OsirisDocument _document;

        // 工作区容器：壳只提供空白区域，内容由面板/画布贡献填充
        private readonly SplitContainer _root;
        private readonly SplitContainer _leftPanelArea;
        private readonly SplitContainer _rightPanelArea;
        private readonly Panel _canvasArea;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStrip _toolStrip;
        private readonly MenuStrip _menuStrip;
        private PictureBox _canvas;

        internal MenuStrip MenuStrip => _menuStrip;
        internal ToolStrip ToolStrip => _toolStrip;
        internal StatusStrip StatusStrip => _statusStrip;

        public OsirisDocument Document => _document;
        public IUiService Ui => _ui;

        public WorkbenchForm(Osiris.Core.Plugins.IPluginRegistry registry, int pluginCount)
        {
            Text = "Osiris 2.0";
            Size = new System.Drawing.Size(1100, 720);

            _document = new OsirisDocument(1, 1);
            _ui = new WorkbenchUiService(this);

            // ---- 壳布局：仅容器，无业务 UI ----
            _menuStrip = new MenuStrip { Dock = DockStyle.Top };
            _toolStrip = new ToolStrip { Dock = DockStyle.Top };
            _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            _statusStrip.Items.Add("已加载模组: " + pluginCount);

            _root = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 0
            };
            _leftPanelArea = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 120
            };
            _rightPanelArea = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel2MinSize = 120
            };
            _canvasArea = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.DimGray };

            _rightPanelArea.Panel1.Controls.Add(_canvasArea);
            _leftPanelArea.Panel2.Controls.Add(_rightPanelArea);
            _root.Panel1.Controls.Add(_leftPanelArea);

            Controls.Add(_root);
            Controls.Add(_statusStrip);
            Controls.Add(_toolStrip);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;

            // 先把内置命令注册进壳，再由模组贡献 UI 资源
            _ui.RegisterCommand(new WorkbenchCommands.OpenDocumentCommand(this));
        }

        /// <summary>供 UiService 使用的内部布局装配（面板按注册顺序加入对应区域）。</summary>
        internal void AddPanelInternal(Osiris.Core.Ui.PanelContribution panel)
        {
            var content = panel.ContentFactory?.Invoke();
            var host = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            if (content is Control c) host.Controls.Add(c);

            switch (panel.Side)
            {
                case PanelSide.Left:
                    if (_leftPanelArea.Panel1.Controls.Count == 0)
                    {
                        _leftPanelArea.Panel1.Controls.Add(host);
                        _root.SplitterDistance = 220;
                    }
                    break;
                case PanelSide.Right:
                    if (_rightPanelArea.Panel2.Controls.Count == 0)
                    {
                        _rightPanelArea.Panel2.Controls.Add(host);
                        _rightPanelArea.SplitterDistance = _rightPanelArea.Width - 220;
                    }
                    break;
                case PanelSide.Bottom:
                    _canvasArea.Controls.Add(host);
                    break;
            }
        }

        /// <summary>渲染全部 UI 资源（模组全部加载完后调用一次）。</summary>
        internal void RebuildUi()
        {
            _ui.ApplyTo(this);
        }

        /// <summary>状态栏消息。</summary>
        internal void SetStatus(string message)
        {
            _statusStrip.Items[0].Text = message;
        }

        /// <summary>加载文档并渲染画布。</summary>
        internal void LoadDocument(OsirisDocument doc, string title)
        {
            // 画布区域已由渲染模组贡献时，由模组自行接管；壳只维护标题
            Text = "Osiris 2.0 — " + title;
            _statusStrip.Items[0].Text = title + "  (图层: " + doc.Layers.Count + ")";
            RenderCanvas();
        }

        /// <summary>用渲染引擎把当前文档画到画布。</summary>
        private void RenderCanvas()
        {
            if (_canvas == null)
            {
                _canvas = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = System.Drawing.Color.DimGray
                };
                _canvasArea.Controls.Add(_canvas);
            }
            using (var bmp = new Osiris.Engine.Skia.CanvasRenderer().Render(_document))
            {
                var old = _canvas.Image;
                _canvas.Image = ToGdiBitmap(bmp);
                old?.Dispose();
            }
        }

        /// <summary>SKBitmap → GDI+ Bitmap（BGRA 与 Format32bppArgb 字节序一致，整行拷贝）。</summary>
        private static System.Drawing.Bitmap ToGdiBitmap(SkiaSharp.SKBitmap sk)
        {
            var bmp = new System.Drawing.Bitmap(sk.Width, sk.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var rect = new System.Drawing.Rectangle(0, 0, sk.Width, sk.Height);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                var srcPtr = sk.GetPixels();
                var srcStride = sk.RowBytes;
                var dstStride = data.Stride;
                var bytesPerRow = sk.Width * 4;
                var rowBuf = new byte[bytesPerRow];
                for (int y = 0; y < sk.Height; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(
                        IntPtr.Add(srcPtr, y * srcStride), rowBuf, 0, bytesPerRow);
                    System.Runtime.InteropServices.Marshal.Copy(
                        rowBuf, 0, IntPtr.Add(data.Scan0, y * dstStride), bytesPerRow);
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
