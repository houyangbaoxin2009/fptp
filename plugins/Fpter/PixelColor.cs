namespace Fpter;

/// <summary>
/// 滤镜自用颜色工具（ABI 红线：插件不得引用 Osiris.Core，故此处复刻 Core ColorUtil 的
/// BGRA 预乘像素值打包/解包与预乘换算）。纯算术实现，零依赖、零分配。
/// 像素值约定：uint 低位=蓝，0xAARRGGBB（与 Abstractions 的 FilterParameterKind.Color 一致）。
/// </summary>
internal static class PixelColor
{
    /// <summary>RGBA 字节 → BGRA 预乘像素值（uint 低位=蓝：0xAARRGGBB）。</summary>
    public static uint PackBgra(byte r, byte g, byte b, byte a = 255)
        => (uint)(b | (g << 8) | (r << 16) | (a << 24));

    /// <summary>从 BGRA 像素值取红。</summary>
    public static byte R(uint bgra) => (byte)((bgra >> 16) & 0xFF);

    /// <summary>从 BGRA 像素值取绿。</summary>
    public static byte G(uint bgra) => (byte)((bgra >> 8) & 0xFF);

    /// <summary>从 BGRA 像素值取蓝。</summary>
    public static byte B(uint bgra) => (byte)(bgra & 0xFF);

    /// <summary>从 BGRA 像素值取透明。</summary>
    public static byte A(uint bgra) => (byte)((bgra >> 24) & 0xFF);

    /// <summary>
    /// 直通色 → 预乘色：RGB 通道按 alpha 加权（channel * a / 255，四舍五入）。
    /// alpha=255 直接返回原值；alpha=0 返回全零（透明黑）。
    /// </summary>
    public static uint Premultiply(uint bgra)
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
    public static uint Unpremultiply(uint bgra)
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

    /// <summary>两个 BGRA 像素的曼哈顿距离（只比 RGB，忽略 alpha，用于颜色近似判定）。</summary>
    public static int ManhattanDistance(uint bgra1, uint bgra2)
        => Math.Abs(R(bgra1) - R(bgra2)) + Math.Abs(G(bgra1) - G(bgra2)) + Math.Abs(B(bgra1) - B(bgra2));
}