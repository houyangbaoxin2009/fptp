using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Osiris.Core.Document;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Osiris.App.Workbench
{
    /// <summary>壳：只负责渲染模组注册的 UI 资源，不含任何业务逻辑。</summary>
    public sealed class WorkbenchForm : Form
    {
        private readonly WorkbenchUiService _ui;
        private OsirisDocument _document;
        private IEditorTool _activeTool;
        private string _currentTitle = "";
        /// <summary>文档级导航栈：裁切/排版生成新文档替换后，可撤销回到原文档（原文档+标题+保存路径）。</summary>
        private readonly Stack<(OsirisDocument Doc, string Title, string Path)> _docBack =
            new Stack<(OsirisDocument, string, string)>();
        private readonly Stack<(OsirisDocument Doc, string Title, string Path)> _docForward =
            new Stack<(OsirisDocument, string, string)>();
        /// <summary>已登记的面板内容（文档切换后触发重绑定）。</summary>
        private readonly List<Osiris.Core.Ui.ListPanelContent> _listPanels =
            new List<Osiris.Core.Ui.ListPanelContent>();

        // 工作区容器：壳只提供空白区域，内容由面板/画布贡献填充
        private readonly SplitContainer _root;
        private readonly SplitContainer _leftPanelArea;
        private readonly SplitContainer _rightPanelArea;
        private readonly Panel _canvasArea;
        /// <summary>画布滚动容器（放大后超出可视区可滚动查看）。</summary>
        private readonly Panel _scrollPanel;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStrip _toolStrip;
        private readonly MenuStrip _menuStrip;
        private PictureBox _canvas;
        /// <summary>状态栏缩放指示（适应窗口/百分比）。</summary>
        private ToolStripStatusLabel _zoomStatus;
        /// <summary>缩放比例（1.0 = 100%）。</summary>
        private double _zoom = 1.0;
        /// <summary>适应窗口模式（默认：画布随窗口自动缩放居中）。</summary>
        private bool _zoomFit = true;

        internal MenuStrip MenuStrip => _menuStrip;
        internal ToolStrip ToolStrip => _toolStrip;
        internal StatusStrip StatusStrip => _statusStrip;

        /// <summary>当前文档（打开新文档后替换）。</summary>
        public OsirisDocument Document => _document;
        public IUiService Ui => _ui;
        /// <summary>已加载的插件注册表（批量处理等壳命令获取滤镜用；Program.cs 装配后设置）。</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Osiris.Core.Plugins.IPluginRegistry PluginRegistry { get; set; }
        /// <summary>当前文档保存路径（未保存过为 null）。</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string CurrentPath { get; set; }

        /// <summary>文档替换通知（宿主据此更新 ActiveDocument 绑定）。</summary>
        internal event Action DocumentChanged;

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
            _zoomStatus = new ToolStripStatusLabel("适应窗口")
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                Alignment = ToolStripItemAlignment.Right
            };
            _statusStrip.Items.Add(_zoomStatus);

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
            _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = System.Drawing.Color.DimGray };
            _canvasArea.Controls.Add(_scrollPanel);

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
            _ui.RegisterCommand(new WorkbenchCommands.SaveCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.SaveAsCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.PrintCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.BatchCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.UndoCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.RedoCommand(this));
            _ui.RegisterCommand(new WorkbenchCommands.ZoomCommand(this, WorkbenchCommands.ZoomCommand.ZoomAction.In));
            _ui.RegisterCommand(new WorkbenchCommands.ZoomCommand(this, WorkbenchCommands.ZoomCommand.ZoomAction.Out));
            _ui.RegisterCommand(new WorkbenchCommands.ZoomCommand(this, WorkbenchCommands.ZoomCommand.ZoomAction.Fit));
            _ui.RegisterCommand(new WorkbenchCommands.ZoomCommand(this, WorkbenchCommands.ZoomCommand.ZoomAction.Actual));

            // 历史变化 → 自动重绘画布（撤销/重做/滤镜入栈后像素已变）
            _document.History.Changed += OnHistoryChanged;

            // 拖放图片文件直接打开
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;
            new WorkbenchCommands.OpenDocumentCommand(this).OpenFile(files[0]);
        }

        /// <summary>Ctrl+= / Ctrl+- 缩放（WinForms ShortcutKeys 不支持 OEM 键，经命令键处理）。</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) != 0)
            {
                switch (keyData & Keys.KeyCode)
                {
                    case Keys.Oemplus: ZoomIn(); return true;
                    case Keys.OemMinus: ZoomOut(); return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>历史变化统一回调（文档替换时重订阅）。</summary>
        private void OnHistoryChanged(object sender, EventArgs e) => RefreshCanvas();
        /// <summary>供 UiService 使用的内部布局装配（面板按注册顺序加入对应区域）。</summary>
        internal void AddPanelInternal(Osiris.Core.Ui.PanelContribution panel)
        {
            var content = panel.ContentFactory?.Invoke();
            Control hostContent = content as Control;
            if (hostContent == null && content is Osiris.Core.Ui.ListPanelContent lpc)
            {
                hostContent = CreateListPanel(lpc);
                _listPanels.Add(lpc);
            }
            var host = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            if (hostContent != null) host.Controls.Add(hostContent);

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

        /// <summary>ListPanelContent 数据契约 → ListBox（壳映射，模组不碰 WinForms）。</summary>
        private static ListBox CreateListPanel(Osiris.Core.Ui.ListPanelContent content)
        {
            var list = new ListBox { Dock = DockStyle.Fill };
            var updating = false;

            // 刷新：挂起选中事件，避免 Clear() 触发 SelectedIndexChanged 反向递归（面板↔历史）
            void Reload()
            {
                if (updating || list.IsDisposed) return;
                updating = true;
                try
                {
                    list.Items.Clear();
                    foreach (var item in content.Items?.Invoke() ?? Array.Empty<string>())
                        list.Items.Add(item);
                    if (content.SelectedIndex >= 0 && content.SelectedIndex < list.Items.Count)
                        list.SelectedIndex = content.SelectedIndex;
                }
                finally { updating = false; }
            }

            Reload();
            content.Changed += Reload;
            list.SelectedIndexChanged += (s, e) =>
            {
                if (!updating)
                    content.SelectedIndexChanged?.Invoke(list.SelectedIndex);
            };
            return list;
        }

        /// <summary>渲染全部 UI 资源（模组全部加载完后调用一次）。</summary>
        internal void RebuildUi()
        {
            _ui.ApplyTo(this);
        }

        /// <summary>状态栏消息（后台任务跨线程调用时自动切回 UI 线程）。</summary>
        internal void SetStatus(string message)
        {
            if (_statusStrip.InvokeRequired)
            {
                _statusStrip.BeginInvoke(new Action(() => SetStatus(message)));
                return;
            }
            _statusStrip.Items[0].Text = message;
        }

        /// <summary>
        /// 加载文档：替换当前文档并重订阅历史事件（模组生成结果/打开文件共用入口）。
        /// 当前文档有实际内容时压入文档级回退栈（初始空文档不入栈），撤销可回到原文档。
        /// </summary>
        internal void LoadDocument(OsirisDocument doc, string title, string path = null)
        {
            if (_document.Layers.Count > 0)
                _docBack.Push((_document, _currentTitle, CurrentPath));
            _docForward.Clear();
            SetDocument(doc, title, path);
        }

        /// <summary>文档级撤销：回到上一个文档（裁切/排版生成新文档后按 Ctrl+Z 回原图）。</summary>
        internal void UndoDocument()
        {
            if (_docBack.Count == 0) return;
            var prev = _docBack.Pop();
            _docForward.Push((_document, _currentTitle, CurrentPath));
            SetDocument(prev.Doc, prev.Title, prev.Path);
        }

        /// <summary>文档级重做：前进到下一个文档。</summary>
        internal void RedoDocument()
        {
            if (_docForward.Count == 0) return;
            var next = _docForward.Pop();
            _docBack.Push((_document, _currentTitle, CurrentPath));
            SetDocument(next.Doc, next.Title, next.Path);
        }

        internal bool CanUndoDocument => _docBack.Count > 0;
        internal bool CanRedoDocument => _docForward.Count > 0;

        /// <summary>替换当前文档核心：重订阅历史、更新标题/路径/状态栏、通知面板与宿主、渲染。</summary>
        private void SetDocument(OsirisDocument doc, string title, string path)
        {
            _document.History.Changed -= OnHistoryChanged;
            _document = doc;
            _document.History.Changed += OnHistoryChanged;

            _currentTitle = title;
            CurrentPath = path;
            Text = "Osiris 2.0 — " + title;
            _statusStrip.Items[0].Text = title + "  (图层: " + doc.Layers.Count + ")";
            DocumentChanged?.Invoke();
            foreach (var p in _listPanels) p.NotifyActiveDocumentChanged();
            RefreshCanvas();
        }

        /// <summary>重绘画布（撤销/重做/文档变更后调用）。</summary>
        internal void RefreshCanvas() => RenderCanvas();

        /// <summary>放大画布（视图菜单/Ctrl+滚轮）。</summary>
        internal void ZoomIn() => SetZoom(_zoom * 1.25);

        /// <summary>缩小画布（视图菜单/Ctrl+滚轮）。</summary>
        internal void ZoomOut() => SetZoom(_zoom / 1.25);

        /// <summary>适应窗口：画布等比例填满可视区并居中。</summary>
        internal void ZoomFitView() { _zoomFit = true; ApplyCanvasLayout(); }

        /// <summary>实际大小 100%。</summary>
        internal void ZoomActual() { _zoomFit = false; _zoom = 1.0; ApplyCanvasLayout(); }

        private void SetZoom(double zoom)
        {
            _zoom = Math.Max(0.05, Math.Min(32.0, zoom));
            _zoomFit = false;
            ApplyCanvasLayout();
        }

        /// <summary>合成当前文档为 GDI+ 位图（保存/打印共用；调用方负责 Dispose）。</summary>
        internal System.Drawing.Bitmap RenderToGdiBitmap()
        {
            using (var sk = new Osiris.Engine.Skia.CanvasRenderer().Render(_document))
                return ToGdiBitmap(sk);
        }

        /// <summary>激活/取消交互工具：壳只路由鼠标事件与覆盖层，不知工具内部逻辑。</summary>
        internal void SetActiveTool(IEditorTool tool)
        {
            if (ReferenceEquals(_activeTool, tool)) return;
            _activeTool?.Deactivate();
            _activeTool = tool;
            _activeTool?.Activate();
            RefreshCanvas();
        }

        /// <summary>PictureBox 画布坐标 → 文档像素坐标（画布尺寸=图片×缩放，等比例满铺无偏移）。</summary>
        private ToolMouseEvent MapToDocument(int clientX, int clientY, ToolMouseButton button)
        {
            var img = _canvas?.Image;
            if (img == null || _canvas.Width == 0 || _canvas.Height == 0)
                return new ToolMouseEvent { X = 0, Y = 0, Button = button };
            return new ToolMouseEvent
            {
                X = (int)(clientX * (double)img.Width / _canvas.Width),
                Y = (int)(clientY * (double)img.Height / _canvas.Height),
                Button = button,
                Modifiers = ModifierKeysToTool(ModifierKeys)
            };
        }

        private static ToolModifiers ModifierKeysToTool(Keys keys)
        {
            var m = ToolModifiers.None;
            if ((keys & Keys.Shift) != 0) m |= ToolModifiers.Shift;
            if ((keys & Keys.Control) != 0) m |= ToolModifiers.Control;
            if ((keys & Keys.Alt) != 0) m |= ToolModifiers.Alt;
            return m;
        }

        /// <summary>用渲染引擎把当前文档画到画布（含激活工具的覆盖层）。</summary>
        private void RenderCanvas()
        {
            if (_canvas == null)
            {
                _canvas = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = System.Drawing.Color.DimGray
                };
                _canvas.MouseDown += OnCanvasMouseDown;
                _canvas.MouseMove += OnCanvasMouseMove;
                _canvas.MouseUp += OnCanvasMouseUp;
                _canvas.MouseWheel += OnCanvasMouseWheel;
                _scrollPanel.Controls.Add(_canvas);
                _scrollPanel.Resize += (s, e) => { if (_zoomFit) ApplyCanvasLayout(); };
            }
            using (var bmp = new Osiris.Engine.Skia.CanvasRenderer().Render(_document))
            {
                // 覆盖层：激活工具自绘蚂蚁线等（直接画进位图，随重绘刷新）
                if (_activeTool != null)
                {
                    using (var canvas = new SkiaSharp.SKCanvas(bmp))
                        _activeTool.DrawOverlay(new SkiaToolOverlay(canvas));
                }
                SwapCanvasImage(ToGdiBitmap(bmp));
            }
            ApplyCanvasLayout();
        }

        /// <summary>Ctrl+滚轮缩放（不按住 Ctrl 时交回 AutoScroll 滚动）。</summary>
        private void OnCanvasMouseWheel(object sender, MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == 0) return;
            ((HandledMouseEventArgs)e).Handled = true;
            if (e.Delta > 0) ZoomIn();
            else ZoomOut();
        }

        /// <summary>按当前视图模式布置画布：适应窗口等比例居中，或按缩放比例定尺寸并启用滚动。</summary>
        private void ApplyCanvasLayout()
        {
            var img = _canvas?.Image;
            if (img == null) return;
            double scale = _zoomFit
                ? Math.Min((double)_scrollPanel.ClientSize.Width / img.Width,
                           (double)_scrollPanel.ClientSize.Height / img.Height)
                : _zoom;
            int w = Math.Max(1, (int)Math.Round(img.Width * scale));
            int h = Math.Max(1, (int)Math.Round(img.Height * scale));
            if (_zoomFit)
            {
                _canvas.Location = new System.Drawing.Point(
                    Math.Max(0, (_scrollPanel.ClientSize.Width - w) / 2),
                    Math.Max(0, (_scrollPanel.ClientSize.Height - h) / 2));
                _canvas.Size = new System.Drawing.Size(w, h);
                _scrollPanel.AutoScrollMinSize = System.Drawing.Size.Empty;
                _zoomStatus.Text = "适应窗口 " + (int)Math.Round(scale * 100) + "%";
            }
            else
            {
                _canvas.Location = System.Drawing.Point.Empty;
                _canvas.Size = new System.Drawing.Size(w, h);
                _scrollPanel.AutoScrollMinSize = new System.Drawing.Size(w, h);
                _zoomStatus.Text = (int)Math.Round(scale * 100) + "%";
            }
        }

        /// <summary>替换画布 Image（释放旧图）。</summary>
        private void SwapCanvasImage(System.Drawing.Bitmap bmp)
        {
            var old = _canvas.Image;
            _canvas.Image = bmp;
            old?.Dispose();
        }

        private void OnCanvasMouseDown(object sender, MouseEventArgs e)
            => SendMouse(e, down: true);

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
            => _activeTool?.MouseMove(MapToDocument(e.X, e.Y, ButtonOf(e)));

        private void OnCanvasMouseUp(object sender, MouseEventArgs e)
            => SendMouse(e, down: false);

        private static ToolMouseButton ButtonOf(MouseEventArgs e)
            => (e.Button & MouseButtons.Left) != 0 ? ToolMouseButton.Left
             : (e.Button & MouseButtons.Middle) != 0 ? ToolMouseButton.Middle
             : ToolMouseButton.Right;

        /// <summary>按下/抬起转发给激活工具（壳只路由，不知工具语义）。</summary>
        private void SendMouse(MouseEventArgs e, bool down)
        {
            if (_activeTool == null) return;
            if ((e.Button & (MouseButtons.Left | MouseButtons.Middle | MouseButtons.Right)) == 0) return;
            var ev = MapToDocument(e.X, e.Y, ButtonOf(e));
            if (down) _activeTool.MouseDown(ev);
            else _activeTool.MouseUp(ev);
        }

        /// <summary>IToolOverlay 的 Skia 实现：虚线蚂蚁线。</summary>
        private sealed class SkiaToolOverlay : IToolOverlay
        {
            private readonly SkiaSharp.SKCanvas _canvas;

            public SkiaToolOverlay(SkiaSharp.SKCanvas canvas) => _canvas = canvas;

            public void DrawPolyline(IReadOnlyList<Point2> points, bool closed)
            {
                if (points == null || points.Count < 2) return;
                using (var paint = new SkiaSharp.SKPaint
                {
                    Style = SkiaSharp.SKPaintStyle.Stroke,
                    StrokeWidth = 1,
                    IsAntialias = true,
                    Color = SkiaSharp.SKColors.Black,
                    PathEffect = SkiaSharp.SKPathEffect.CreateDash(new float[] { 4, 4 }, 0)
                })
                using (var path = new SkiaSharp.SKPath())
                {
                    path.MoveTo(points[0].X + 0.5f, points[0].Y + 0.5f);
                    for (int i = 1; i < points.Count; i++)
                        path.LineTo(points[i].X + 0.5f, points[i].Y + 0.5f);
                    if (closed) path.Close();
                    _canvas.DrawPath(path, paint);
                }
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
