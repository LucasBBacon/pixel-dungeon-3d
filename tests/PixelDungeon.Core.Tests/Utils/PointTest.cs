using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class PointTest
{
    [Fact]
    public void Equals_SameCoordinates_IsTrue()
    {
        Assert.Equal(new Point(1, 2), new Point(1, 2));
        Assert.True(new Point(1, 2) == new Point(1, 2));
    }

    [Fact]
    public void Equals_DifferentCoordinates_IsFalse()
    {
        Assert.NotEqual(new Point(1, 2), new Point(2, 1));
        Assert.True(new Point(1, 2) != new Point(2, 1));
        Assert.False(new Point(4, 6).Equals(null));
    }

    [Fact]
    public void GetHashCode_SameCoordinates_Matches()
    {
        Assert.Equal(new Point(5, 10).GetHashCode(), new Point(5, 10).GetHashCode());
    }

    [Fact]
    public void Offset_MutatesAndReturnsSelf()
    {
        var p = new Point(1, 1);
        var returned = p.Offset(2, 3);
        Assert.Same(p, returned);
        Assert.Equal(new Point(3, 4), p);
        Assert.Equal(new Point(4, 6), p.Offset(new Point(1, 2)));
    }

    [Fact]
    public void Scale_TruncatesTowardZero()
    {
        Assert.Equal(new Point(1, -1), new Point(3, -3).Scale(0.5f));
    }

    [Fact]
    public void Clone_IsIndependentCopy()
    {
        var p = new Point(5, 6);
        var c = p.Clone();
        c.Set(0, 0);
        Assert.Equal(new Point(5, 6), p);
        Assert.Equal(new Point(0, 0), c);
    }

    [Fact]
    public void Set_FromPoint_CopiesCoordinates()
    {
        Assert.Equal(new Point(7, 8), new Point().Set(new Point(7, 8)));
    }
}