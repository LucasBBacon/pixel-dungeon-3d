using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Tests.Levels;

namespace PixelDungeon.Core.Tests.Mechanics;

public class BallisticaTests : DungeonFixture
{
    private sealed class Blocker : Char
    {
        protected override bool Act() => false;
    }

    private static readonly string[] Hall =
    [
        "##########",
        "#........#",
        "#........#",
        "#........#",
        "#...#....#",
        "#........#",
        "##########"
    ];

    [Fact]
    public void Cast_StraightLine_ListsEveryCellAndReturnsTheTaget()
    {
        LoadLevel(Hall);
        var from = TestLevel.At(1, 1);
        var to = TestLevel.At(5, 1);
        Assert.Equal(to, Ballistica.Cast(from, to, false, false));
        Assert.Equal(6, Ballistica.Distance);
        Assert.Equal(new[]
            {
                from,
                TestLevel.At(2, 1),
                TestLevel.At(3, 1),
                TestLevel.At(4, 1),
                to
            },
            Ballistica.Trace[..5]);
    }

    [Fact]
    public void Cast_Diagonal_TracesBresenhamCells()
    {
        LoadLevel(Hall);
        var from = TestLevel.At(1, 1);
        var to = TestLevel.At(4, 3);
        Assert.Equal(to, Ballistica.Cast(from, to, false, false));
        Assert.Equal(new[]
            {
                from,
                TestLevel.At(2, 2),
                TestLevel.At(3, 2),
                to
            },
            Ballistica.Trace[..4]);
    }

    [Fact]
    public void Cast_StopsAtAWall_ReturningTheLastOpenCell()
    {
        LoadLevel(Hall);
        var from = TestLevel.At(1, 4);
        var to = TestLevel.At(7, 4);
        Assert.Equal(TestLevel.At(3, 4), Ballistica.Cast(from, to, false, false));
    }

    [Fact]
    public void Cast_HitChars_ShopsAnOccupiedCell()
    {
        LoadLevel(Hall);
        var blocker = new Blocker { Pos = TestLevel.At(3, 2) };
        Actor.OccupyCell(blocker);

        Assert.Equal(TestLevel.At(3, 2),
            Ballistica.Cast(TestLevel.At(1, 2),
                TestLevel.At(6, 2),
                false,
                true));
        Assert.Equal(TestLevel.At(6, 2),
            Ballistica.Cast(TestLevel.At(1, 2),
                TestLevel.At(6, 2),
                false,
                false));
    }

    [Fact]
    public void Cast_Magic_ContinuesPastTargetToTheWall()
    {
        LoadLevel(Hall);
        Assert.Equal(TestLevel.At(8, 2),
            Ballistica.Cast(TestLevel.At(1, 2),
                TestLevel.At(3, 2),
                true,
                false));
    }
}