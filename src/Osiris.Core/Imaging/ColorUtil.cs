using System;

namespace Osiris.Core.Imaging
{
    /// <summary>
    /// 颜色工具（2.0 替代 1.x Assalg 的颜色部分）：差异计算、BGRA 打包。
    /// Core 自用类型，不依赖 System.Drawing。
    /// </summary>
    public static class ColorUtil
    {
        /// <summary>RGBA 字节 → BGRA 预乘像素值（int 低位=蓝）。</summary>
        public static int PackBgra(byte r, byte g, byte b, byte a = 255)
            => (b) | (g << 8) | (r << 16) | (a << 24);

        /// <summary>从 BGRA 像素值取红。</summary>
        public static int R(int bgra) => (bgra >> 16) & 0xFF;
        /// <summary>从 BGRA 像素值取绿。</summary>
        public static int G(int bgra) => (bgra >> 8) & 0xFF;
        /// <summary>从 BGRA 像素值取蓝。</summary>
        public static int B(int bgra) => bgra & 0xFF;
        /// <summary>从 BGRA 像素值取透明。</summary>
        public static int A(int bgra) => (bgra >> 24) & 0xFF;

        /// <summary>两个 BGRA 像素的曼哈顿距离（只比 RGB，忽略 alpha）。</summary>
        public static int Difference(int bgra1, int bgra2)
            => Math.Abs(R(bgra1) - R(bgra2)) + Math.Abs(G(bgra1) - G(bgra2)) + Math.Abs(B(bgra1) - B(bgra2));

        /// <summary>
        /// 四角取"最接近其他角"的采样色：各角到其余三角距离之和最小者，
        /// 避免单一角被主体占据导致误采样。
        /// </summary>
        public static int MostCommonCorner(int c0, int c1, int c2, int c3)
        {
            int d0 = Difference(c0, c1) + Difference(c0, c2) + Difference(c0, c3);
            int d1 = Difference(c1, c0) + Difference(c1, c2) + Difference(c1, c3);
            int d2 = Difference(c2, c0) + Difference(c2, c1) + Difference(c2, c3);
            int d3 = Difference(c3, c0) + Difference(c3, c1) + Difference(c3, c2);

            int min = Math.Min(Math.Min(d0, d1), Math.Min(d2, d3));
            if (min == d0) return c0;
            if (min == d1) return c1;
            if (min == d2) return c2;
            return c3;
        }
    }
}
