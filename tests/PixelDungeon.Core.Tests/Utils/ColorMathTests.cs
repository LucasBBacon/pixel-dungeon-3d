using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class ColorMathTests
{
    [Theory]
    [InlineData(0x004400, 0x88CC44, 0xFF448822u)]
    [InlineData(0x48763C, 0x59994A, 0xFF508743u)]
    [InlineData(0x444444, 0x777766, 0xFF5D5D55u)]
    public void Midpoint_AveragesEachChannelAndAddsOpaqueAlpha(int a, int b, uint expected) => 
        Assert.Equal(expected, ColorMath.Midpoint(a, b));
}