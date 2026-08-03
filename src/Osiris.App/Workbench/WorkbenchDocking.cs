using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Osiris.App.Workbench
{
    /// <summary>停靠方位。</summary>
    internal enum DockZone
    {
        Top,
        Left,
        Bottom,
        Right
    }

    /// <summary>停靠宿主：一个 TabControl 承载多个面板，Tab 头支持拖拽迁移/合并。</summary>
    internal sealed class DockHost
    {
        public DockZone Zone { get; }
        public TabControl Tabs { get; }
        /// <summary>所属 SplitContainer 面板（对应上/左/下/右四区）。</summary>
        private readonly SplitContainer _owner;
        private readonly bool _isPanel1;
        /// <summary>停靠时该区默认尺寸（宽或高）。</summary>
        public int DefaultSize { get; }

        public DockHost(DockZone zone, SplitContainer owner, bool isPanel1, int defaultSize)
        {
            Zone = zone;
            _owner = owner;
            _isPanel1 = isPanel1;
            DefaultSize = defaultSize;
            Tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(90, 22)
            };
        }

        /// <summary>无任何面板页时折叠该区。</summary>
        public bool IsEmpty => Tabs.TabPages.Count == 0;

        private bool IsCollapsed => _isPanel1 ? _owner.Panel1Collapsed : _owner.Panel2Collapsed;

        /// <summary>该区是否展开（有可见区域可合并）。</summary>
        public bool IsExpanded => _isPanel1 ? !_owner.Panel1Collapsed : !_owner.Panel2Collapsed;

        /// <summary>空则折叠、非空则展开并按默认尺寸校准分隔距离（面板增减后调用）。</summary>
        public void ApplyVisibility()
        {
            if (_isPanel1)
            {
                if (_owner.Panel1Collapsed != IsEmpty) _owner.Panel1Collapsed = IsEmpty;
            }
            else if (_owner.Panel2Collapsed != IsEmpty)
            {
                _owner.Panel2Collapsed = IsEmpty;
            }
            // 展开后 SplitterDistance 可能漂移（SplitContainer 折叠时距离无意义），按默认尺寸校准
            if (IsExpanded) EnsureExpanded();
        }

        /// <summary>展开该区并设默认分隔距离（停靠落点后调用）。</summary>
        public void EnsureExpanded()
        {
            if (_isPanel1) _owner.Panel1Collapsed = false;
            else _owner.Panel2Collapsed = false;

            // Vertical=左右分割（距离沿宽），Horizontal=上下分割（距离沿高）
            int total = _owner.Orientation == Orientation.Vertical ? _owner.Width : _owner.Height;
            int dist = _isPanel1 ? DefaultSize : total - DefaultSize;
            if (total > _owner.Panel1MinSize + _owner.Panel2MinSize)
            {
                dist = Math.Max(_owner.Panel1MinSize,
                    Math.Min(dist, total - _owner.Panel2MinSize));
                _owner.SplitterDistance = dist;
            }
        }

        /// <summary>该区的目标矩形（拖拽高亮用，相对 relativeTo 客户区）。展开用实际边界，折叠用默认尺寸预估。</summary>
        public Rectangle GetTargetRect(Control relativeTo)
        {
            Rectangle rect;
            if (!IsCollapsed)
            {
                var panel = _isPanel1 ? _owner.Panel1 : _owner.Panel2;
                rect = panel.RectangleToScreen(panel.ClientRectangle);
                return relativeTo.RectangleToClient(rect);
            }

            rect = relativeTo.RectangleToClient(_owner.RectangleToScreen(_owner.ClientRectangle));
            if (_owner.Orientation == Orientation.Vertical)
                return _isPanel1
                    ? new Rectangle(rect.X, rect.Y, DefaultSize, rect.Height)
                    : new Rectangle(rect.Right - DefaultSize, rect.Y, DefaultSize, rect.Height);
            return _isPanel1
                ? new Rectangle(rect.X, rect.Y, rect.Width, DefaultSize)
                : new Rectangle(rect.X, rect.Bottom - DefaultSize, rect.Width, DefaultSize);
        }
    }

    /// <summary>轻量停靠管理：Tab 头拖拽跨区迁移/合并。鼠标捕获设在源 TabControl（不遮挡界面），
    /// 目标高亮用独立小面板只盖住目标区，其余工作区保持可见。</summary>
    internal sealed class DockManager
    {
        private readonly Form _form;
        /// <summary>目标高亮面板：仅覆盖目标区，其余界面保持可见。</summary>
        private readonly Panel _glow;
        private readonly List<DockHost> _hosts = new List<DockHost>();
        private readonly Dictionary<DockZone, DockHost> _zoneMap = new Dictionary<DockZone, DockHost>();

        // 按下阶段（未超拖拽阈值）
        private DockHost _pressHost;
        private TabPage _pressPage;
        private Point _pressScreen;

        // 拖拽阶段
        private bool _dragging;
        private DockHost _dragSource;
        private TabPage _dragPage;
        private DockHost _lastTarget;

        public DockManager(Form form, params DockHost[] hosts)
        {
            _form = form;
            foreach (var h in hosts)
            {
                _hosts.Add(h);
                _zoneMap[h.Zone] = h;
                var tabs = h.Tabs;
                tabs.MouseDown += (s, e) => TabsMouseDown(h, e);
                tabs.MouseMove += (s, e) => TabsMouseMove(h, e);
                tabs.MouseUp += (s, e) => TabsMouseUp(h, e);
            }

            // 高亮面板：加到窗体最上层，平时隐藏；只盖目标区
            _glow = new Panel
            {
                BackColor = Color.FromArgb(64, 0, 120, 215),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            _form.Controls.Add(_glow);
        }

        /// <summary>往指定停靠区追加面板页（空区自动展开）。</summary>
        public void AddTab(DockHost host, TabPage page)
        {
            host.Tabs.TabPages.Add(page);
            host.ApplyVisibility();
        }

        /// <summary>按方位取停靠区。</summary>
        public DockHost HostOf(DockZone zone) => _zoneMap[zone];

        private void TabsMouseDown(DockHost host, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            for (int i = 0; i < host.Tabs.TabPages.Count; i++)
            {
                if (host.Tabs.GetTabRect(i).Contains(e.Location))
                {
                    _pressHost = host;
                    _pressPage = host.Tabs.TabPages[i];
                    _pressScreen = host.Tabs.PointToScreen(e.Location);
                    // 捕获鼠标：即使移出 TabControl 仍持续收到 MouseMove/MouseUp
                    host.Tabs.Capture = true;
                    return;
                }
            }
            _pressPage = null;
            _pressHost = null;
        }

        private void TabsMouseMove(DockHost host, MouseEventArgs e)
        {
            if (_dragging)
            {
                UpdateHighlight();
                return;
            }
            if (_pressPage == null) return;
            var cur = host.Tabs.PointToScreen(e.Location);
            if (Math.Abs(cur.X - _pressScreen.X) < SystemInformation.DragSize.Width &&
                Math.Abs(cur.Y - _pressScreen.Y) < SystemInformation.DragSize.Height) return;

            // 超过阈值：进入拖拽（鼠标已在源 TabControl 上捕获，事件持续到达）
            _dragging = true;
            _dragSource = _pressHost;
            _dragPage = _pressPage;
            _pressPage = null;
            UpdateHighlight();
        }

        private void TabsMouseUp(DockHost host, MouseEventArgs e)
        {
            // 无论是否进入拖拽都要收尾：清空按下状态并释放捕获。
            // 否则点击 tab 后未拖动就释放，_pressHost/_pressPage 残留且 Capture 未释放，
            // 之后仅鼠标悬停移动超过阈值便会误触发拖拽（面板意外迁移）。
            _pressHost = null;
            _pressPage = null;
            if (host.Tabs.Capture) host.Tabs.Capture = false;
            if (!_dragging) return;
            var target = _lastTarget;
            _dragging = false;
            _glow.Visible = false;
            if (target != null && target != _dragSource && _dragPage != null)
            {
                _dragSource.Tabs.TabPages.Remove(_dragPage);
                target.EnsureExpanded();
                target.Tabs.TabPages.Add(_dragPage);
                target.Tabs.SelectedTab = _dragPage;
                _dragSource.ApplyVisibility();
            }
            _dragPage = null;
            _dragSource = null;
            _lastTarget = null;
        }

        /// <summary>落点判定：鼠标到四边的距离，停靠到最近的一边（四角归最近边，不会发生
        /// “想停下面却停到右面”）；若落在某已展开区的 TabControl 上则优先合并进该区。</summary>
        private DockHost ResolveTarget()
        {
            // 优先：鼠标悬停在某个已展开区的 TabControl 上 → 合并到该区
            foreach (var h in _hosts)
            {
                if (h == _dragSource || !h.IsExpanded) continue;
                var tp = h.Tabs.PointToClient(Cursor.Position);
                if (h.Tabs.ClientRectangle.Contains(tp)) return h;
            }

            // 距离最近边判定：左/右带宽 1/3 宽，上/下带 1/3 高
            var pt = _form.PointToClient(Cursor.Position);
            var r = _form.ClientRectangle;
            int dLeft = pt.X;
            int dRight = r.Right - pt.X;
            int dTop = pt.Y;
            int dBottom = r.Bottom - pt.Y;
            int min = Math.Min(Math.Min(dLeft, dRight), Math.Min(dTop, dBottom));

            if (min == dLeft && dLeft <= r.Width / 3) return _zoneMap[DockZone.Left];
            if (min == dRight && dRight <= r.Width / 3) return _zoneMap[DockZone.Right];
            if (min == dTop && dTop <= r.Height / 3) return _zoneMap[DockZone.Top];
            if (min == dBottom && dBottom <= r.Height / 3) return _zoneMap[DockZone.Bottom];
            return null;
        }

        private void UpdateHighlight()
        {
            _lastTarget = ResolveTarget();
            if (_lastTarget == null)
            {
                _glow.Visible = false;
                return;
            }
            var rect = _lastTarget.GetTargetRect(_glow.Parent);
            _glow.Location = rect.Location;
            _glow.Size = rect.Size;
            _glow.Visible = true;
            _glow.BringToFront();
        }
    }
}
