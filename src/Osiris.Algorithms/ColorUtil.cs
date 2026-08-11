namespace Osiris.Algorithms;

/// <summary>
/// 像素颜色工具：BGRA 预乘像素的打包/解包与预乘转换（模块开发者通用）。
/// 颜色打包约定（PackBgra）：int = A&lt;&lt;24 | R&lt;&lt;16 | G&lt;&lt;8 | B。
/// </summary>
public static class ColorUtil
{
    /// <summary>打包 BGRA 颜色（R/G/B/A 各 8bit → int）。</summary>
    public static int PackBgra(byte r, byte g, byte b, byte a = 255)
        => (a << 24) | (r << 16) | (g << 8) | b;

    /// <summary>取红色通道（Bgra 打包值）。</summary>
    public static byte R(int bgra) => (byte)(bgra >> 16);

    /// <summary>取绿色通道。</summary>
    public static byte G(int bgra) => (byte)(bgra >> 8);

    /// <summary>取蓝色通道。</summary>
    public static byte B(int bgra) => (byte)bgra;

    /// <summary>取 Alpha 通道。</summary>
    public static byte A(int bgra) => (byte)(bgra >> 24);

    /// <summary>预乘：把直通 RGBA 的 RGB 按 Alpha 预乘（结果 PackBgra 布局，Alpha 保持；全透明归零）。</summary>
    public static int Premultiply(int bgra)
    {
        byte a = A(bgra);
        if (a == 255) return bgra;
        if (a == 0) return 0; // 全透明：RGB 归零（预乘语义下 alpha=0 则 RGB 必须为 0）
        return PackBgra((byte)(R(bgra) * a / 255), (byte)(G(bgra) * a / 255), (byte)(B(bgra) * a / 255), a);
    }

    /// <summary>反预乘：还原直通 RGB（Alpha 保持；全透明返回 0）。</summary>
    public static int Unpremultiply(int bgra)
    {
        byte a = A(bgra);
        if (a == 0) return 0;
        if (a == 255) return bgra;
        byte r = (byte)Math.Min(255, R(bgra) * 255 / a);
        byte g = (byte)Math.Min(255, G(bgra) * 255 / a);
        byte b = (byte)Math.Min(255, B(bgra) * 255 / a);
        return PackBgra(r, g, b, a);
    }
}
