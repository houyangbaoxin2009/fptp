using Osiris.Core.Imaging;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// ColorUtil 颜色算术测试：BGRA 打包/通道提取往返、预乘/反预乘已知值。
/// </summary>
public class ColorUtilTests
{
    [Fact]
    public void PackBgra_KnownValue_And_ChannelExtraction()
    {
        // 意图：PackBgra(255,128,64,32) 的已知位布局——低位=蓝，最高字节=alpha。
        int pixel = ColorUtil.PackBgra(255, 128, 64, 32);

        Assert.Equal(32, ColorUtil.A(pixel));
        Assert.Equal(255, ColorUtil.R(pixel));
        Assert.Equal(128, ColorUtil.G(pixel));
        Assert.Equal(64, ColorUtil.B(pixel));
        // 直接按位验证：0x40 | (0x80<<8) | (0xFF<<16) | (0x20<<24)
        Assert.Equal(0x20FF8040, pixel);
    }

    [Fact]
    public void PackBgra_RoundTrip_AllChannels()
    {
        // 意图：任意通道组合打包后逐通道提取应往返一致。
        int pixel = ColorUtil.PackBgra(200, 100, 50, 128);
        Assert.Equal(200, ColorUtil.R(pixel));
        Assert.Equal(100, ColorUtil.G(pixel));
        Assert.Equal(50, ColorUtil.B(pixel));
        Assert.Equal(128, ColorUtil.A(pixel));
    }

    [Fact]
    public void Premultiply_SemiTransparentRed_KnownValue()
    {
        // 意图：不透明红 255 半透明 alpha=128 → 预乘后 R 通道 = (255*128+127)/255 = 128。
        int premul = ColorUtil.Premultiply(ColorUtil.PackBgra(255, 0, 0, 128));

        Assert.Equal(128, ColorUtil.R(premul));
        Assert.Equal(0, ColorUtil.G(premul));
        Assert.Equal(0, ColorUtil.B(premul));
        Assert.Equal(128, ColorUtil.A(premul));
    }

    [Fact]
    public void Unpremultiply_RecoversOriginalStraightColor()
    {
        // 意图：预乘红(128) 反预乘回直通色 → R 恢复 255（钳制上限）。
        int straight = ColorUtil.Unpremultiply(ColorUtil.PackBgra(128, 0, 0, 128));

        Assert.Equal(255, ColorUtil.R(straight));
        Assert.Equal(0, ColorUtil.G(straight));
        Assert.Equal(128, ColorUtil.A(straight));
    }

    [Fact]
    public void Premultiply_AlphaZero_ReturnsZero()
    {
        // 意图：alpha=0 时无颜色信息，预乘/反预乘均返回全零（透明黑）。
        Assert.Equal(0, ColorUtil.Premultiply(ColorUtil.PackBgra(255, 128, 64, 0)));
        Assert.Equal(0, ColorUtil.Unpremultiply(ColorUtil.PackBgra(255, 128, 64, 0)));
    }

    [Fact]
    public void Premultiply_Alpha255_ReturnsSame()
    {
        // 意图：不透明像素（alpha=255）预乘/反预乘均原样返回。
        int pixel = ColorUtil.PackBgra(200, 100, 50, 255);
        Assert.Equal(pixel, ColorUtil.Premultiply(pixel));
        Assert.Equal(pixel, ColorUtil.Unpremultiply(pixel));
    }
}
