using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests;

public class InterlevelTests : DungeonFixture
{
    private static void Run(InterlevelMode mode)
    {
        Interlevel.Mode = mode;
        Interlevel.Run();
    }

    [Fact]
    public void Descend_WithoutAHero_StartsANewGameOnDepthOne()
    {
        Dungeon.Hero = null;
        Run(InterlevelMode.Descend);

        Assert.NotNull(Dungeon.Hero);
        Assert.Equal(1, Dungeon.Depth);
        Assert.IsType<SewerLevel>(Dungeon.Level);
        Assert.Equal(Dungeon.Level.Entrance, Dungeon.Hero.Pos);
        Assert.Equal(InterlevelMode.Descend, Interlevel.Mode); // view clears it after arrival
    }

    [Fact]
    public void Descend_ThenAscend_RestoresTheLevelYouLeft()
    {
        Dungeon.Hero = null;
        Run(InterlevelMode.Descend);
        var first = Dungeon.Level;
        var fistMap = (int[])first.Map.Clone();
        var firstVisited = (bool[])first.Visited.Clone();

        Run(InterlevelMode.Descend);
        Assert.Equal(2, Dungeon.Depth);
        Assert.NotSame(first, Dungeon.Level);
        var second = Dungeon.Level;
        var secondMap = (int[])second.Map.Clone();

        Run(InterlevelMode.Ascend);
        Assert.Equal(1, Dungeon.Depth);
        Assert.Equal(fistMap, Dungeon.Level.Map);
        for (var i = 0; i < Level.Length; i++)
        {
            if (firstVisited[i])
            {
                Assert.True(Dungeon.Level.Visited[i]); // arriving at the exit observes more cells, never fewer
            }
        }

        Assert.NotNull(Dungeon.Hero);
        Assert.Equal(first.Exit, Dungeon.Hero.Pos);
        Assert.NotNull(((SewerLevel)Dungeon.Level).RoomAt(first.Entrance));

        Run(InterlevelMode.Descend);
        Assert.Equal(2, Dungeon.Depth);
        Assert.Equal(secondMap, Dungeon.Level.Map); // loaded not generated
        Assert.Equal(second.Entrance, Dungeon.Hero.Pos);
    }

    [Fact]
    public void Fall_LandsOnAPassableCellOfTheNextDepth()
    {
        Dungeon.Hero = null;
        Run(InterlevelMode.Descend);
        Interlevel.FallIntoPit = false;
        Run(InterlevelMode.Fall);

        Assert.NotNull(Dungeon.Hero);
        Assert.Equal(2, Dungeon.Depth);
        Assert.True(Level.Passable[Dungeon.Hero.Pos] || Level.Avoid[Dungeon.Hero.Pos]);
    }

    [Fact]
    public void HeroDescend_EndToEnd()
    {
        Dungeon.Hero = null;
        Run(InterlevelMode.Descend);
        Assert.NotNull(Dungeon.Hero);

        Dungeon.Hero.Pos = Dungeon.Level.Exit;
        Dungeon.Hero.Handle(Dungeon.Level.Exit);
        Assert.Equal(new[] { InterlevelMode.Descend }, View.SwitchedModes);

        Interlevel.Run();
        Assert.Equal(2, Dungeon.Depth);
        Assert.Equal(Dungeon.Level.Entrance, Dungeon.Hero.Pos);
    }

    [Fact]
    public void FixTime_RunsOnEveryTransition()
    {
        Dungeon.Hero = null;
        Run(InterlevelMode.Descend);
        Assert.NotNull(Dungeon.Hero);
        Dungeon.Hero.Spend(5);

        Actor.Process(); // respawner acts at 0, then hero at 5, clock reads 5
        Run(InterlevelMode.Descend);
        Assert.Equal(5f, Statistics.Duration);
    }
}