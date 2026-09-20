using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Levels.Painters;

public class PainterTests
{
    [Fact]
    public void Fill_Rect_includesRightAndBottomEdges()
    {
        var level = TestLevel.FromRows("#");
        Painter.Fill(level, new Rect(2, 2, 5, 4), Terrain.Empty);

        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(2, 2)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(5, 4)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(6, 4)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(5, 5)]);
    }

    [Fact]
    public void Fill_WithMargin_ShrinksEachSide()
    {
        var level = TestLevel.FromRows("#");
        Painter.Fill(level, new Rect(2, 2, 6, 6), 1, Terrain.Empty);

        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(2, 2)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(3, 3)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(5, 5)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(6, 6)]);
    }

    [Fact]
    public void DrawInside_WalksAwayFromTheRoomEdge()
    {
        var level = TestLevel.FromRows("#");
        var room = new Rect(2, 2, 8, 8);
        var end = Painter.DrawInside(level, room, new Point(2, 5), 3, Terrain.EmptySp);

        Assert.Equal(Terrain.EmptySp, level.Map[TestLevel.At(3, 5)]);
        Assert.Equal(Terrain.EmptySp, level.Map[TestLevel.At(5, 5)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(6, 5)]);
        Assert.Equal(new Point(6, 5), end);
    }
}