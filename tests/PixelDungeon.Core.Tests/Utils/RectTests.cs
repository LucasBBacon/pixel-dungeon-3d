using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class RectTests
{
    private static (int, int, int, int) Edges(Rect r) => (r.Left, r.Top, r.Right, r.Bottom);

    [Fact]
    public void WidthHeightSquare_UseHalfOpenEdges()
    {
        var r = new Rect(1, 1, 4, 3);
        Assert.Equal(3, r.Width());
        Assert.Equal(2, r.Height());
        Assert.Equal(6, r.Square());
    }

    [Fact]
    public void IsEmpty_WhenRightNotGreaterThanLeft()
    {
        Assert.True(new Rect(2, 2, 2, 5).IsEmpty());
        Assert.True(new Rect().IsEmpty());
        Assert.False(new Rect(0, 0, 1, 1).IsEmpty());
    }

    [Fact]
    public void SetEmpty_ZeroesAllEdges()
    {
        Assert.Equal((0, 0, 0, 0), Edges(new Rect(1, 2, 3, 4).SetEmpty()));
    }

    [Fact]
    public void Intersect_Disjoint_IsEmpty()
    {
        Assert.True(new Rect(0, 0, 2, 2).Intersect(new Rect(3, 3, 5, 5)).IsEmpty());
    }

    [Fact]
    public void Intersect_Overlapping_ReturnsNewRectAndLeavesOriginal()
    {
        var a = new Rect(0, 0, 4, 4);
        var i = a.Intersect(new Rect(2, 2, 6, 6));
        Assert.Equal((2, 2, 4, 4), Edges(i));
        Assert.Equal((0, 0, 4, 4), Edges(a));
        Assert.NotSame(a, i);
    }

    [Fact]
    public void Union_GrowsToIncludePoint()
    {
        var r = new Rect(0, 0, 2, 2);
        Assert.Same(r, r.Union(5, 5));
        Assert.Equal((0, 0, 6, 6), Edges(r));
        r.Union(new Point(-1, -2));
        Assert.Equal((-1, -2, 6, 6), Edges(r));
    }

    [Fact]
    public void Union_OnEmptyRect_BecomesUnitRectAtPoint()
    {
        Assert.Equal((3, 4, 4, 5), Edges(new Rect().Union(new Point(3, 4))));
    }

    [Fact]
    public void Inside_IsInconclusiveOfLeftTopExclusiveOfRightBottom()
    {
        var r = new Rect(0, 0, 2, 2);
        Assert.True(r.Inside(new Point(0, 0)));
        Assert.True(r.Inside(new Point(1, 1)));
        Assert.False(r.Inside(new Point(2, 2)));
        Assert.False(r.Inside(new Point(2, 0)));
        Assert.False(r.Inside(new Point(-1, 0)));
    }

    [Fact]
    public void Shrink_ReturnsNewRectAndLeavesOriginal()
    {
        var r = new Rect(0, 0, 4, 4);
        var s = r.Shrink();
        Assert.Equal((1, 1, 3, 3), Edges(s));
        Assert.Equal((0, 0, 4, 4), Edges(r));
        Assert.NotSame(s, r);
        Assert.Equal((2, 2, 2, 2), Edges(r.Shrink(2)));
    }

    [Fact]
    public void CopyConstructor_CopiesEdges()
    {
        Assert.Equal((1, 2, 3, 4), Edges(new Rect(new Rect(1, 2, 3, 4))));
    }
}