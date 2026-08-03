using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Osiris.Core.Document;
using Osiris.Core.Plugins;
using Osiris.Core.Ui;

namespace Osiris.App.Workbench
{
    /// <summary>壳：只负责渲染模组注册的 UI 资源，不含任何业务逻辑。</summary>
    public sealed class WorkbenchForm : Form, IMessageFilter
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

        // 工作区容器：四边停靠区（Tab 合并）+ 中央画布
        private readonly SplitContainer _root;
        private readonly SplitContainer _mid;
        private readonly SplitContainer _centerColumn;
        private readonly SplitContainer _canvasColumn;
        private readonly Panel _canvasArea;
        private readonly DockHost _dockTop;
        private readonly DockHost _dockLeft;
        private readonly DockHost _dockBottom;
        private readonly DockHost _dockRight;
        private readonly DockManager _dockManager;
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
            Size = new System.Drawing.Size(1400, 900);

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
                Alignment = ToolStripItemAlignment.Right,
                IsLink = true
            };
            _zoomStatus.Click += (s, e) => ToggleZoomFit();
            _statusStrip.Items.Add(_zoomStatus);

            // 四边停靠区 + 中央画布（嵌套 SplitContainer：上 | 左 | 画布列 | 右）
            // 注意：Orientation.Vertical = 左右排列，Orientation.Horizontal = 上下排列
            _root = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 120,
                Panel1Collapsed = true // 无上面板时画布占满
            };
            _mid = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 120,
                Panel1Collapsed = true // 无左面板时画布占满
            };
            _centerColumn = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel2MinSize = 120,
                Panel2Collapsed = true // 无右面板时画布占满
            };
            _canvasColumn = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2MinSize = 120,
                Panel2Collapsed = true // 无下面板时画布占满
            };
            _canvasArea = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.DimGray };
            _scrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = System.Drawing.Color.DimGray };
            EnableDoubleBuffered(_canvasArea);
            EnableDoubleBuffered(_scrollPanel);
            _canvasArea.Controls.Add(_scrollPanel);

            _dockTop = new DockHost(DockZone.Top, _root, true, 160);
            _dockLeft = new DockHost(DockZone.Left, _mid, true, 200);
            _dockBottom = new DockHost(DockZone.Bottom, _canvasColumn, false, 160);
            _dockRight = new DockHost(DockZone.Right, _centerColumn, false, 200);
            _dockManager = new DockManager(this, _dockTop, _dockLeft, _dockBottom, _dockRight);

            _root.Panel1.Controls.Add(_dockTop.Tabs);
            _root.Panel2.Controls.Add(_mid);
            _mid.Panel1.Controls.Add(_dockLeft.Tabs);
            _mid.Panel2.Controls.Add(_centerColumn);
            _centerColumn.Panel1.Controls.Add(_canvasColumn);
            _centerColumn.Panel2.Controls.Add(_dockRight.Tabs);
            _canvasColumn.Panel1.Controls.Add(_canvasArea);
            _canvasColumn.Panel2.Controls.Add(_dockBottom.Tabs);

            Controls.Add(_root);
            Controls.Add(_statusStrip);
            Controls.Add(_toolStrip);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;

            // 先把内置命令注册进壳，再由模组贡献 UI 资源
            _ui.RegisterCommand(new WorkbenchCommands.OpenDocumentCommand(this));            _ui.RegisterCommand(new WorkbenchCommands.SaveCommand(this));
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

            // Ctrl+滚轮缩放：IMessageFilter 全局拦截（PictureBox 无焦点时 MouseWheel 不触发）
            Application.AddMessageFilter(this);
            FormClosed += (s, e) => Application.RemoveMessageFilter(this);
        }

        /// <summary>
        /// 全局消息过滤：Ctrl+滚轮缩放（以光标为中心）。PictureBox 的 MouseWheel 事件仅在
        /// 控件有焦点时触发，不可靠；此处在消息层拦截，光标在画布区域内即缩放。
        /// </summary>
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL) return false;
            if ((ModifierKeys & Keys.Control) == 0) return false;
            var pos = _scrollPanel.PointToClient(Cursor.Position);
            if (!_scrollPanel.ClientRectangle.Contains(pos)) return false;
            int delta = (short)((long)m.WParam >> 16);
            ZoomAt(pos.X, pos.Y, delta > 0 ? 1.25 : 0.8);
            return true;
        }

        private const int WM_MOUSEWHEEL = 0x020A;

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
        /// <summary>供 UiService 使用的内部布局装配（面板按注册顺序加入对应停靠区，同一区合并为 Tab）。</summary>
        internal void AddPanelInternal(Osiris.Core.Ui.PanelContribution panel)
        {
            var content = panel.ContentFactory?.Invoke();
            Control hostContent = content as Control;
            if (hostContent == null && content is Osiris.Core.Ui.ListPanelContent lpc)
            {
                hostContent = CreateListPanel(lpc);
                _listPanels.Add(lpc);
            }
            if (hostContent == null) return;

            var page = new TabPage(panel.Title ?? panel.Id) { Dock = DockStyle.Fill };
            page.Controls.Add(hostContent);

            var host = DockHostFor(panel.Side);
            _dockManager.AddTab(host, page);
        }

        /// <summary>面板方位 → 停靠区。</summary>
        private DockHost DockHostFor(Osiris.Core.Ui.PanelSide side)
        {
            switch (side)
            {
                case Osiris.Core.Ui.PanelSide.Left: return _dockLeft;
                case Osiris.Core.Ui.PanelSide.Right: return _dockRight;
                default: return _dockBottom;
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

        /// <summary>放大画布（视图菜单/工具栏：以视口中心缩放）。</summary>
        internal void ZoomIn() => ZoomAt(_scrollPanel.ClientSize.Width / 2, _scrollPanel.ClientSize.Height / 2, 1.25);

        /// <summary>缩小画布（视图菜单/工具栏：以视口中心缩放）。</summary>
        internal void ZoomOut() => ZoomAt(_scrollPanel.ClientSize.Width / 2, _scrollPanel.ClientSize.Height / 2, 0.8);

        /// <summary>适应窗口：画布等比例填满可视区并居中。</summary>
        internal void ZoomFitView() { _zoomFit = true; ApplyCanvasLayout(); }

        /// <summary>实际大小 100%。</summary>
        internal void ZoomActual() { _zoomFit = false; _zoom = 1.0; ApplyCanvasLayout(); }

        /// <summary>在 适应窗口 与 实际大小 之间切换（状态栏点击/画布双击）。</summary>
        internal void ToggleZoomFit()
        {
            if (_zoomFit) ZoomActual();
            else ZoomFitView();
        }

        /// <summary>
        /// 以光标(clientX, clientY，相对 _scrollPanel 客户区)为中心缩放 factor 倍。
        /// 缩放前后光标下的文档像素保持不动（缩放锚定光标 + 视口跟随）。
        /// </summary>
        private void ZoomAt(int clientX, int clientY, double factor)
        {
            var img = _canvas?.Image;
            if (img == null) return;
            double scale = _zoomFit
                ? Math.Min((double)_scrollPanel.ClientSize.Width / img.Width,
                           (double)_scrollPanel.ClientSize.Height / img.Height)
                : _zoom;
            var sp = _scrollPanel.AutoScrollPosition; // 负值：内容被滚动的偏移
            double offX = -sp.X, offY = -sp.Y;
            // 光标下的文档坐标（画布内容原点 = 画布位置 + 滚动偏移）
            double docX = (clientX + offX - _canvas.Location.X) / scale;
            double docY = (clientY + offY - _canvas.Location.Y) / scale;

            _zoom = Math.Max(0.05, Math.Min(32.0, scale * factor));
            _zoomFit = false;
            // 关闭重绘：缩放全程（改尺寸+滚动跟随）不绘制，完成后一次性显示最终画面，
            // 杜绝 AutoScrollMinSize 变化触发的滚动条重算导致的中间帧跳动（鬼畜）。
            SetRedraw(_scrollPanel, false);
            try
            {
                ApplyCanvasLayout(); // 新布局：画布居中，AutoScrollMinSize=图片×新缩放
                _scrollPanel.PerformLayout(); // 布局+滚动条先重算完毕，滚动位置才不会被后续布局覆盖

                // 视口跟随：让 docX 仍落在光标处。
                // 滚动偏移 = 画布居中偏移 + docX×新缩放 - 光标（AutoScrollPosition setter 取正值）
                double newOffX = _canvas.Location.X + docX * _zoom - clientX;
                double newOffY = _canvas.Location.Y + docY * _zoom - clientY;
                _scrollPanel.AutoScrollPosition = new System.Drawing.Point(
                    (int)Math.Round(Math.Max(0, newOffX)), (int)Math.Round(Math.Max(0, newOffY)));
                _scrollPanel.PerformLayout(); // 应用滚动位置
            }
            finally
            {
                SetRedraw(_scrollPanel, true);
            }
            _scrollPanel.Invalidate(); // 重绘最终画面
        }

        /// <summary>WM_SETREDRAW：开/关控件重绘（缩放全程锁绘，完成一次性刷新防中间帧）。</summary>
        private static void SetRedraw(System.Windows.Forms.Control control, bool on)
        {
            const int WM_SETREDRAW = 0x000B;
            if (control.IsHandleCreated)
                SendMessage(control.Handle, WM_SETREDRAW, (IntPtr)(on ? 1 : 0), IntPtr.Zero);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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

        /// <summary>用渲染引擎把当前文档画到画布（覆盖层由 Paint 事件绘制，不烧进位图）。</summary>
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
                _canvas.Paint += OnCanvasPaint;
                _canvas.DoubleClick += (s, e) => ToggleZoomFit();
                EnableDoubleBuffered(_canvas);
                _scrollPanel.Controls.Add(_canvas);
                _scrollPanel.Resize += (s, e) => { if (_zoomFit) ApplyCanvasLayout(); };
            }
            using (var bmp = new Osiris.Engine.Skia.CanvasRenderer().Render(_document))
                SwapCanvasImage(ToGdiBitmap(bmp));
            ApplyCanvasLayout();
        }

        /// <summary>控件双缓冲（受保护属性经反射开启），消除缩放/滚动时的重绘闪烁。</summary>
        private static void EnableDoubleBuffered(System.Windows.Forms.Control control)
        {
            typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        /// <summary>画布表面覆盖层：激活工具自绘蚂蚁线等（GDI+ 缩放坐标，随 Invalidate 轻量重绘）。</summary>
        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            if (_activeTool == null) return;
            var img = _canvas?.Image;
            if (img == null || _canvas.Width == 0 || _canvas.Height == 0) return;
            float scaleX = (float)_canvas.Width / img.Width;
            float scaleY = (float)_canvas.Height / img.Height;
            _activeTool.DrawOverlay(new GdiToolOverlay(e.Graphics, scaleX, scaleY));
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
            // 注意：不能用 SuspendLayout 包裹 AutoScrollMinSize——挂起期间改 MinSize 会在
            // ResumeLayout 时被滚动条重算把 AutoScrollPosition 重置为 (0,0)，导致缩放后画布跳回原点（鬼畜）。
            // 双缓冲已消除重绘闪烁，这里顺序设置属性即可。
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
                // 非 fit 模式：画布居中；超出视口时钳到 (0,0) 由滚动查看
                _canvas.Location = new System.Drawing.Point(
                    Math.Max(0, (_scrollPanel.ClientSize.Width - w) / 2),
                    Math.Max(0, (_scrollPanel.ClientSize.Height - h) / 2));
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
        {
            if (_activeTool == null) return;
            _activeTool.MouseMove(MapToDocument(e.X, e.Y, ButtonOf(e)));
            _canvas.Invalidate(); // 拖拽轨迹实时刷新蚂蚁线（仅重绘表面，不重渲染文档）
        }

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
            if (down)
            {
                _canvas.Capture = true; // 捕获鼠标：拖出画布仍持续收集轨迹
                _activeTool.MouseDown(ev);
            }
            else
            {
                _activeTool.MouseUp(ev);
                _canvas.Capture = false;
            }
            _canvas.Invalidate();
        }

        /// <summary>IToolOverlay 的 GDI+ 实现：虚线蚂蚁线（坐标=文档像素×缩放）。</summary>
        private sealed class GdiToolOverlay : IToolOverlay
        {
            private readonly System.Drawing.Graphics _graphics;
            private readonly float _scaleX;
            private readonly float _scaleY;

            public GdiToolOverlay(System.Drawing.Graphics graphics, float scaleX, float scaleY)
            {
                _graphics = graphics;
                _scaleX = scaleX;
                _scaleY = scaleY;
            }

            public void DrawPolyline(IReadOnlyList<Point2> points, bool closed)
            {
                if (points == null || points.Count < 2) return;
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                })
                {
                    var pts = new System.Drawing.PointF[points.Count + (closed ? 1 : 0)];
                    for (int i = 0; i < points.Count; i++)
                        pts[i] = new System.Drawing.PointF(
                            (points[i].X + 0.5f) * _scaleX, (points[i].Y + 0.5f) * _scaleY);
                    if (closed) pts[points.Count] = pts[0];
                    _graphics.DrawLines(pen, pts);
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
