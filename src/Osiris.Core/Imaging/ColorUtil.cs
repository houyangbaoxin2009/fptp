namespace Osiris.Core.Imaging;

/// <summary>
/// 颜色工具（2.0 Assalg 颜色部分的新架构重写）：
/// BGRA 预乘像素值（int，低位=蓝）的打包/解包与预乘换算。
/// Core 自用类型，纯算术零依赖（不依赖 System.Drawing / SkiaSharp）。
/// </summary>
public static class ColorUtil
{
    /// <summary>RGBA 字节 → BGRA 预乘像素值（int 低位=蓝：0xAA RR GG BB）。</summary>
    public static int PackBgra(byte r, byte g, byte b, byte a = 255)
        => b | (g << 8) | (r << 16) | (a << 24);

    /// <summary>从 BGRA 像素值取红。</summary>
    public static byte R(int bgra) => (byte)((bgra >> 16) & 0xFF);

    /// <summary>从 BGRA 像素值取绿。</summary>
    public static byte G(int bgra) => (byte)((bgra >> 8) & 0xFF);

    /// <summary>从 BGRA 像素值取蓝。</summary>
    public static byte B(int bgra) => (byte)(bgra & 0xFF);

    /// <summary>从 BGRA 像素值取透明。</summary>
    public static byte A(int bgra) => (byte)((bgra >> 24) & 0xFF);

    /// <summary>
    /// 直通色 → 预乘色：RGB 通道按 alpha 加权（channel * a / 255，四舍五入）。
    /// alpha=255 直接返回原值；alpha=0 返回全零（透明黑）。
    /// </summary>
    public static int Premultiply(int bgra)
    {
        byte a = A(bgra);
        if (a == 255)
            return bgra;
        if (a == 0)
            return 0;

        byte r = (byte)((R(bgra) * a + 127) / 255);
        byte g = (byte)((G(bgra) * a + 127) / 255);
        byte b = (byte)((B(bgra) * a + 127) / 255);
        return PackBgra(r, g, b, a);
    }

    /// <summary>
    /// 预乘色 → 直通色：RGB 通道按 alpha 反加权（channel * 255 / a，四舍五入并钳制 255）。
    /// alpha=255 直接返回原值；alpha=0 无有效颜色信息，返回全零。
    /// </summary>
    public static int Unpremultiply(int bgra)
    {
        byte a = A(bgra);
        if (a == 255)
            return bgra;
        if (a == 0)
            return 0;

        byte r = (byte)Math.Min(255, (R(bgra) * 255 + a / 2) / a);
        byte g = (byte)Math.Min(255, (G(bgra) * 255 + a / 2) / a);
        byte b = (byte)Math.Min(255, (B(bgra) * 255 + a / 2) / a);
        return PackBgra(r, g, b, a);
    }
}
