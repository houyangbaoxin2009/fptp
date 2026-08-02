using System;
using System.Collections.Generic;

namespace Osiris.Core.Document
{
    /// <summary>整数坐标点（Core 自有点，避免依赖 System.Drawing）。</summary>
    public struct Point2
    {
        public int X;
        public int Y;

        public Point2(int x, int y) { X = x; Y = y; }
    }

    /// <summary>
    /// 选区：像素级 8bit 蒙版（1=选中 / 0=未选）+ 矩形包围盒。
    /// 自有缓冲（byte[]），独立于渲染后端；工具写入、滤镜读取、壳渲染蚂蚁线。
    /// </summary>
    public sealed class Selection
    {
        private readonly byte[] _mask;

        public int Width { get; }
        public int Height { get; }
        /// <summary>选中像素数（每次更新时重算）。</summary>
        public int SelectedCount { get; private set; }
        /// <summary>是否为空选区（无选中像素）。</summary>
        public bool IsEmpty => SelectedCount == 0;

        /// <summary>构造全选/全不选选区。</summary>
        public Selection(int width, int height, bool selectAll = false)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "尺寸必须为正");
            Width = width;
            Height = height;
            _mask = new byte[width * height];
            if (selectAll)
            {
                for (int i = 0; i < _mask.Length; i++) _mask[i] = 1;
                SelectedCount = _mask.Length;
            }
        }

        /// <summary>蒙版像素访问（0..W*H，1=选中）。</summary>
        public Span<byte> Mask => _mask;

        /// <summary>底层数组（渲染/快照用）。</summary>
        public byte[] Data => _mask;

        /// <summary>查询指定像素是否选中。</summary>
        public bool IsSelected(int x, int y)
            => x >= 0 && y >= 0 && x < Width && y < Height && _mask[y * Width + x] != 0;

        /// <summary>整幅清除（全不选）。</summary>
        public void Clear()
        {
            Array.Clear(_mask, 0, _mask.Length);
            SelectedCount = 0;
        }

        /// <summary>整幅全选。</summary>
        public void SelectAll()
        {
            for (int i = 0; i < _mask.Length; i++) _mask[i] = 1;
            SelectedCount = _mask.Length;
        }

        /// <summary>
        /// 用闭合多边形栅格化选区：逐扫描线求交点（偶数-奇数规则）。
        /// 点坐标为文档像素坐标；替换式（不清空则叠加）。
        /// </summary>
        public void SetPolygon(IReadOnlyList<Point2> points, bool replace = true)
        {
            if (points == null || points.Count < 3) return;
            if (replace) Clear();

            // 计算多边形包围盒
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var p in points)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            minX = Math.Max(0, minX); minY = Math.Max(0, minY);
            maxX = Math.Min(Width - 1, maxX); maxY = Math.Min(Height - 1, maxY);
            if (minX > maxX || minY > maxY) return;

            var n = points.Count;
            for (int y = minY; y <= maxY; y++)
            {
                // 收集本行与多边形边的交点 x
                // 半开区间规则（严格大于）：顶点只算一次，水平边跳过，避免重叠顶点双计
                var xs = new List<int>();
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    var pi = points[i];
                    var pj = points[j];
                    bool yi = pi.Y > y, yj = pj.Y > y;
                    if (yi != yj)
                    {
                        // 边 (pj→pi) 与扫描线 y 的交点
                        int x = pi.X + (y - pi.Y) * (pj.X - pi.X) / (pj.Y - pi.Y);
                        xs.Add(x);
                    }
                }
                xs.Sort();
                // 偶数-奇数填充：成对填充
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int x0 = Math.Max(minX, xs[k]);
                    int x1 = Math.Min(maxX, xs[k + 1]);
                    for (int x = x0; x <= x1; x++)
                    {
                        if (_mask[y * Width + x] == 0)
                        {
                            _mask[y * Width + x] = 1;
                            SelectedCount++;
                        }
                    }
                }
            }
        }

        /// <summary>与另一选区按位与（交集，用于"从选区减去"等）。</summary>
        public void Intersect(Selection other)
        {
            if (other == null || other._mask.Length != _mask.Length) return;
            SelectedCount = 0;
            for (int i = 0; i < _mask.Length; i++)
            {
                if (_mask[i] != 0 && other._mask[i] != 0) { _mask[i] = 1; SelectedCount++; }
                else _mask[i] = 0;
            }
        }
    }
}
