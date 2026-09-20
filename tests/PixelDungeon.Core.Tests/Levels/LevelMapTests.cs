using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests.Levels;

public class LevelMapTests
{
    [Fact]
    public void Distance_IsChebyshev_AndAdjacentIsEightWay()
    {
        Assert.Equal(3, Level.Distance(TestLevel.At(1, 1), TestLevel.At(4, 3)));
        Assert.True(Level.Adjacent(TestLevel.At(5, 5), TestLevel.At(6, 6)));
        Assert.True(Level.Adjacent(TestLevel.At(5, 5), TestLevel.At(5, 4)));
        Assert.False(Level.Adjacent(TestLevel.At(5, 5), TestLevel.At(7, 5)));
        Assert.False(Level.Adjacent(TestLevel.At(5, 5), TestLevel.At(5, 5)));
    }

    [Fact]
    public void BuildFlagMaps_SettsFlagsAndForcesTheBorderImpassable()
    {
        var level = TestLevel.FromRows(
            "....",
            ".+S.",
            "...."
        );

        Assert.False(Level.Passable[TestLevel.At(0, 0)]); // border row
        Assert.False(Level.Passable[TestLevel.At(0, 1)]); // border column
        Assert.True(Level.Passable[TestLevel.At(1, 1)]);
        Assert.True(Level.LosBlocking[TestLevel.At(1, 1)]); // closed door
        Assert.True(Level.Solid[TestLevel.At(1, 1)]);
        Assert.False(Level.Passable[TestLevel.At(2, 1)]); // secret door is a wall
        Assert.True(Level.Secret[TestLevel.At(2, 1)]);
        Assert.NotNull(level.Map);
    }

    [Fact]
    public void BuildFlagMaps_StitchesWaterTilesByUnstitchableNeighbours()
    {
        var level = TestLevel.FromRows(
            "#####",
            "#.~.#",
            "######"
        );

        // Neighbours4 order is up, right, down, left - unstitchable neighbors add bits 1, 2, 4, 8
        Assert.Equal(Terrain.WaterTiles + 1 + 4, level.Map[TestLevel.At(2, 1)]);
        Assert.True(Level.Water[TestLevel.At(2, 1)]);
        Assert.True(Level.Passable[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void BuildFlagMaps_StitchesChasmEdgesByTheCellAbove()
    {
        var level = TestLevel.FromRows(
            "#######",
            "#.~#Px#",
            "#xxxxx#",
            "#######"
        );

        Assert.Equal(Terrain.ChasmFloor, level.Map[TestLevel.At(1, 2)]);
        Assert.Equal(Terrain.ChasmWater, level.Map[TestLevel.At(2, 2)]);
        Assert.Equal(Terrain.ChasmWall, level.Map[TestLevel.At(3, 2)]);
        Assert.Equal(Terrain.ChasmFloorSp, level.Map[TestLevel.At(4, 2)]);
        Assert.Equal(Terrain.Chasm, level.Map[TestLevel.At(5, 2)]); // cell above is also a pit
        Assert.True(Level.Pit[TestLevel.At(1, 2)]);
        Assert.True(Level.Avoid[TestLevel.At(1, 2)]);
        Assert.False(Level.Passable[TestLevel.At(1, 2)]);
    }

    [Fact]
    public void CleanWalls_MarksOnlyWallNextToNonWallsDiscoverable()
    {
        TestLevel.FromRows(
            "#####",
            "#.###",
            "#####"
        );

        Assert.True(Level.Discoverable[TestLevel.At(1, 1)]);
        Assert.True(Level.Discoverable[TestLevel.At(2, 2)]); // diagonal neighbor of the floor
        Assert.False(Level.Discoverable[TestLevel.At(4, 0)]); // walls all around
    }

    [Fact]
    public void Set_UpdatesMapsAndFlags()
    {
        var level = TestLevel.FromRows(
            "###",
            "#.#",
            "###"
        );
        Dungeon.Level = level;

        Level.Set(TestLevel.At(1, 1), Terrain.Water);
        Assert.Equal(Terrain.Water, level.Map[TestLevel.At(1, 1)]);
        Assert.True(Level.Water[TestLevel.At(1, 1)]);

        Level.Set(TestLevel.At(1, 1), Terrain.Wall);
        Assert.False(Level.Passable[TestLevel.At(1, 1)]);
        Assert.False(Level.Water[TestLevel.At(1, 1)]);

        Dungeon.Level = null;
    }

    [Fact]
    public void Destroy_LeavesEmbers_OrFloodsNextToWater()
    {
        var level = TestLevel.FromRows(
            "#####",
            "#.,~#",
            "#####"
        );
        Dungeon.Level = level;

        level.Destroy(TestLevel.At(1, 1));
        Assert.Equal(Terrain.Embers, level.Map[TestLevel.At(1, 1)]);

        Level.Set(TestLevel.At(2, 1), Terrain.Wall);
        level.Destroy(TestLevel.At(2, 1));
        Assert.True(level.Map[TestLevel.At(2, 1)] >= Terrain.WaterTiles);

        Dungeon.Level = null;
    }

    [Fact]
    public void TileName_AndTileDesc_FollowTables()
    {
        var level = TestLevel.FromRows("#");

        Assert.Equal("Wall", level.TileName(Terrain.SecretDoor));
        Assert.Equal("Water", level.TileName(Terrain.WaterTiles + 5));
        Assert.Equal("Chasm", level.TileName(Terrain.ChasmWall));
        Assert.Equal("Floor", level.TileName(Terrain.SecretFireTrap));
        Assert.Equal("Depth exit", level.TileName(Terrain.Exit));
        Assert.Equal("Stairs lead down to the lower depth.", level.TileDesc(Terrain.Exit));
        Assert.Equal("You can't see the bottom.", level.TileDesc(Terrain.ChasmFloor));
        Assert.Equal("", level.TileDesc(Terrain.Empty));
    }

    [Fact]
    public void TunnelTile_DependsOnFeeling()
    {
        var level = TestLevel.FromRows();

        Assert.Equal(Terrain.Empty, level.TunnelTile());
        level.Feeling = LevelFeeling.Chasm;
        Assert.Equal(Terrain.EmptySp, level.TunnelTile());
    }

    [Fact]
    public void Dungeon_BossAndShopDepths()
    {
        Assert.True(Dungeon.BossLevel(5));
        Assert.True(Dungeon.BossLevel(25));
        Assert.False(Dungeon.BossLevel(6));

        Dungeon.Depth = 11;
        Assert.True(Dungeon.ShopOnLevel());

        Dungeon.Depth = 12;
        Assert.False(Dungeon.ShopOnLevel());

        Dungeon.Depth = 0;
        Assert.False(Dungeon.IsChallenged(Challenges.Darkness));
    }
}