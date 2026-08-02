using System;
using System.Collections.Generic;
using Osiris.Core.Document;
using Osiris.Core.History;
using Osiris.Core.Plugins;

namespace Fptp.Plugins.Builtin
{
    /// <summary>
    /// 套索选框工具（一笔画选区）：首个 IEditorTool 落地实现。
    /// 拖拽收集轨迹点 → 闭合多边形 → 栅格化写入选区（经历史栈入栈，可撤销）。
    /// 零 UI 依赖：只接收纯数据事件，壳负责路由与覆盖层绘制。
    /// </summary>
    public sealed class LassoTool : IEditorTool
    {
        private readonly List<Point2> _points = new List<Point2>();
        private IHostContext _host;
        private bool _dragging;

        public string Id => "fptp.builtin.lasso";
        public string Name => "套索选框";
        public string Version => "2.0.2.0";
        public string MinHostVersion => "2.0.2.0";

        /// <summary>是否已被壳激活为当前工具（命令据此切换/灰显）。</summary>
        public bool Active { get; private set; }

        public void Initialize(IHostContext host) => _host = host;

        public void Activate()
        {
            Active = true;
            _points.Clear();
            _dragging = false;
        }

        public void Deactivate()
        {
            Active = false;
            _points.Clear();
            _dragging = false;
        }

        public void MouseDown(ToolMouseEvent e)
        {
            _dragging = true;
            _points.Clear();
            _points.Add(new Point2(e.X, e.Y));
        }

        public void MouseMove(ToolMouseEvent e)
        {
            if (!_dragging) return;
            var last = _points[_points.Count - 1];
            // 去重相邻同点，防轨迹冗余
            if (last.X == e.X && last.Y == e.Y) return;
            _points.Add(new Point2(e.X, e.Y));
        }

        public void MouseUp(ToolMouseEvent e)
        {
            if (!_dragging) return;
            _dragging = false;
            CommitSelection();
        }

        /// <summary>闭合多边形 → 栅格化 → 以选区编辑命令入栈（构造快照 before，Execute 写入 after）。</summary>
        private void CommitSelection()
        {
            var doc = _host?.ActiveDocument;
            if (doc == null || _points.Count < 3) { _points.Clear(); return; }

            // 复制轨迹（防止后续 MouseMove 修改列表）；补一个闭合点使终点=起点
            var polygon = new List<Point2>(_points);
            var first = polygon[0];
            polygon.Add(first);
            _points.Clear();

            // 先栅格化到临时选区计算包围盒（选区编辑命令需区域快照）
            var temp = new Selection(doc.Width, doc.Height);
            temp.SetPolygon(polygon, replace: true);
            var (minX, minY, maxX, maxY) = Bounds(polygon);
            if (minX > maxX || minY > maxY) return;

            // 区域快照：把临时蒙版对应区域作为 after 交给命令
            var w = maxX - minX + 1;
            var h = maxY - minY + 1;
            var after = new byte[w * h];
            for (int r = 0; r < h; r++)
                Array.Copy(temp.Data, (minY + r) * doc.Width + minX, after, r * w, w);

            // 构造命令（此时选区仍为旧状态 → before 快照正确）→ Push 执行写入
            var cmd = new SelectionEditCommand("套索选区", doc, minX, minY, w, h, after);
            doc.History.Push(cmd, doc);
        }

        /// <summary>多边形包围盒（钳制到文档范围内）。</summary>
        private (int minX, int minY, int maxX, int maxY) Bounds(IReadOnlyList<Point2> pts)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            var doc = _host?.ActiveDocument;
            int w = doc?.Width ?? 0, h = doc?.Height ?? 0;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            minX = Math.Max(0, minX); minY = Math.Max(0, minY);
            maxX = Math.Min(w - 1, maxX); maxY = Math.Min(h - 1, maxY);
            return (minX, minY, maxX, maxY);
        }

        public void DrawOverlay(IToolOverlay overlay)
        {
            if (_points.Count < 2) return;
            overlay.DrawPolyline(_points, closed: !_dragging);
        }
    }
}
