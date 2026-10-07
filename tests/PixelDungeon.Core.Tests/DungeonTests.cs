using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests;

public class DungeonTests : DungeonFixture
{
    [Fact]
    public void Init_CreatesAHero_AndResetsRunState()
    {
        Dungeon.Depth = 7;
        Dungeon.Gold = 9;
        Dungeon.Init();
        Assert.NotNull(Dungeon.Hero);
        Assert.Equal(20, Dungeon.Hero.HP);
        Assert.Equal(0, Dungeon.Depth);
        Assert.Equal(0, Dungeon.Gold);
        Assert.True(Dungeon.DewVial);
        // Init() calls hero.Live(), which now attaches Regeneration
        // that buff schedules itself as an actor even though the hero isn't placed
        // on a level yet
        Assert.Single(Actor.All());
    }

    [Fact]
    public void NewLevel_IncrementsDepth_TracksDeepestFloor_AndBuildsASewer()
    {
        Dungeon.Init();
        var first = Dungeon.NewLevel();
        Assert.IsType<SewerLevel>(first);
        Assert.Equal(1, Dungeon.Depth);
        Assert.Equal(1, Statistics.DeepestFloor);
        Assert.True(Statistics.QualifiedForNoKilling);
        Assert.Equal(Terrain.Entrance, first.Map[first.Entrance]);

        Dungeon.NewLevel();
        Assert.Equal(2, Dungeon.Depth);
        Assert.Equal(2, Statistics.DeepestFloor);
    }

    [Fact]
    public void SwitchLevel_PlacesTheHero_SchedulesActors_AndObserves()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Entrance);

        Assert.Same(level, Dungeon.Level);
        Assert.Equal(level.Entrance, Dungeon.Hero.Pos);
        Assert.True(Dungeon.Visible[level.Entrance]);
        Assert.True(level.Visited[level.Entrance]);
        Assert.Contains(Dungeon.Hero, Actor.All());
        // hero, hero's regen buff, respawner, and every mob
        Assert.Equal(3 + level.Mobs.Count, Actor.All().Count);
        Assert.Equal(8, Dungeon.Hero.ViewDistance);
        Assert.Equal(1, View.ObserveCount);

        Dungeon.SwitchLevel(level, -1);
        Assert.Equal(level.Exit, Dungeon.Hero.Pos);
    }

    [Fact]
    public void SaveLevel_ThenLoadLevel_RoundTrips()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Entrance);
        Dungeon.SaveLevel();

        var loaded = Dungeon.LoadLevel();
        Assert.Null(Dungeon.Level);
        Assert.NotSame(level, loaded);
        Assert.Equal(level.Map, loaded.Map);
        Assert.Equal(level.Visited, loaded.Visited);
        Assert.Equal(level.Entrance, loaded.Entrance);
        Assert.Empty(Actor.All());
    }

    [Fact]
    public void LoadLevel_ForAnUnvisitedDepth_Throws()
    {
        Dungeon.Init();
        Dungeon.Depth = 9;
        Assert.Throws<KeyNotFoundException>(() => Dungeon.LoadLevel());
    }

    [Fact]
    public void Reset_ClearsTheLevelStore()
    {
        Dungeon.Init();
        Dungeon.NewLevel();
        Dungeon.SaveLevel();
        Dungeon.Reset();
        Dungeon.Depth = 1;

        Assert.Throws<KeyNotFoundException>(() => Dungeon.LoadLevel());
    }

    [Fact]
    public void ResetLevel_KeepsTheMapAndReturnsToTheEntrance()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Exit);
        var map = (int[])level.Map.Clone();
        Dungeon.ResetLevel();

        Assert.Equal(map, Dungeon.Level.Map);
        Assert.Equal(level.Entrance, Dungeon.Hero.Pos);
    }
}