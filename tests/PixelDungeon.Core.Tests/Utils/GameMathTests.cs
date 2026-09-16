using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class GameMathTests
{
    [Fact]
    public void gate_BelowMin_ReturnsMin()
    {
        Assert.Equal(0f, GameMath.Gate(0, -1, 6));
    }

    [Fact]
    public void Gate_InsideRange_ReturnsValue()
    {
        Assert.Equal(3f, GameMath.Gate(0, 3, 6));
    }

    [Fact]
    public void Gate_AboveMax_ReturnsMax()
    {
        Assert.Equal(6f, GameMath.Gate(0, 9, 6));
    }
}