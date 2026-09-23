using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Tests.Levels;

namespace PixelDungeon.Core.Tests;

public class DungeonPathTests : DungeonFixture
{
    private sealed class Walker : Char
    {
        protected override bool Act() => false;
    }

    private static readonly string[] Corridor =
    [
        "########",
        "#......#",
        "########"
    ];

    [Fact]
    public void FindPath_AdjacentTarget_StepsDirectlyWhenFree()
    {
        LoadLevel(Corridor);
        var ch = new Walker { Pos = TestLevel.At(1, 1) };
        Assert.Equal(TestLevel.At(2, 1),
            Dungeon.FindPath(ch,
                ch.Pos,
                TestLevel.At(2, 1),
                Level.Passable,
                Level.FieldOfView));
        Assert.Equal(-1,
            Dungeon.FindPath(ch,
                ch.Pos,
                TestLevel.At(1, 0),
                Level.Passable,
                Level.FieldOfView));
    }

    [Fact]
    public void FindPath_TreatsVisibleCharsAsBlocked_AndUnseenOnesAsOpen()
    {
        LoadLevel(Corridor);
        PlaceHero(TestLevel.At(6, 1));
        var walker = new Walker { Pos = TestLevel.At(1, 1) };
        var blocker = new Walker { Pos = TestLevel.At(3, 1) };
        Actor.Add(walker);
        Actor.Add(blocker);

        var visible = new bool[Level.Length];
        Assert.Equal(TestLevel.At(2, 1),
            Dungeon.FindPath(walker,
                walker.Pos,
                TestLevel.At(5, 1),
                Level.Passable,
                visible));

        visible[TestLevel.At(3, 1)] = true;
        Assert.Equal(-1, Dungeon.FindPath(walker,
            walker.Pos,
            TestLevel.At(4, 1),
            Level.Passable,
            visible));
    }

    [Fact]
    public void Flee_StepsAwayFromTheThreat()
    {
        LoadLevel(Corridor);
        var ch = new Walker { Pos = TestLevel.At(3, 1) };
        var step = Dungeon.Flee(ch,
            ch.Pos,
            TestLevel.At(1, 1),
            Level.Passable,
            new bool[Level.Length]);
        Assert.Equal(TestLevel.At(4, 1), step);
    }
}