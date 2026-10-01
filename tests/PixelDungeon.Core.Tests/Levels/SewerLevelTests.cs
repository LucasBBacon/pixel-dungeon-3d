using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Levels;

public class SewerLevelTests : DungeonFixture
{
    private static SewerLevel Generate(int seed, int depth)
    {
        Random.Seed(seed);
        Dungeon.Depth = depth;
        var level = new SewerLevel();
        level.Create();
        Dungeon.Level = level;
        return level;
    }

    private static bool Connected(Level level, int from, int to)
    {
        var seen = new bool[Level.Length];
        var queue = new Queue<int>();
        queue.Enqueue(from);
        seen[from] = true;
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (cell == to) return true;
            foreach (var offset in Level.Neighbours8)
            {
                var neighbour = cell + offset;

                if (neighbour < 0 || neighbour >= Level.Length || seen[neighbour]) continue;
                if (!Level.Passable[neighbour] && !Level.Avoid[neighbour] && !Level.Secret[neighbour]) continue;

                seen[neighbour] = true;
                queue.Enqueue(neighbour);
            }
        }

        return false;
    }

    [Fact]
    public void DepthOne_EverySeed_IsWalkableFromEntranceToExit()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var level = Generate(seed, 1);
            Assert.Equal(Terrain.Entrance, level.Map[level.Entrance]);
            Assert.Equal(Terrain.Exit, level.Map[level.Exit]);
            Assert.NotEqual(level.Entrance, level.Exit);
            Assert.True(Level.Passable[level.Entrance]);
            Assert.True(Level.Passable[level.Exit]);
            Assert.True(Connected(level, level.Entrance, level.Exit), $"seed {seed} has no path");
        }
    }

    [Fact]
    public void DepthOne_HasNoSecretsAndNoFeeling()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var level = Generate(seed, 1);
            Assert.Equal(LevelFeeling.None, level.Feeling);
            Assert.Equal(0, level.SecretDoors);
            Assert.DoesNotContain(level.Map, t => (Terrain.Flags[t] & Terrain.Secret) != 0);
        }
    }

    [Fact]
    public void ExactlyOneSign_InsideThEntranceRoom()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var level = Generate(seed, 1);
            var signs = Enumerable
                .Range(0, Level.Length)
                .Where(i => level.Map[i] == Terrain.Sign)
                .ToList();
            Assert.Single(signs);
            var room = level.RoomAt(signs[0]);
            Assert.NotNull(room);
            Assert.Equal(RoomType.Entrance, room.Type);
            Assert.NotEqual(level.Entrance, signs[0]);
        }
    }

    [Fact]
    public void DepthThree_RollsEveryFeeling_AndChasmLevelsHavePits_AndSecretsAppear()
    {
        var seen = new HashSet<LevelFeeling>();
        var secrets = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            var level = Generate(seed, 3);
            seen.Add(level.Feeling);
            secrets += level.SecretDoors;
            if (level.Feeling == LevelFeeling.Chasm)
            {
                Assert.Contains(true, Level.Pit);
            }

            Assert.True(Connected(level, level.Entrance, level.Exit), $"seed {seed} has no path");
        }

        Assert.Contains(LevelFeeling.Chasm, seen);
        Assert.Contains(LevelFeeling.Water, seen);
        Assert.Contains(LevelFeeling.Grass, seen);
        Assert.True(secrets > 0);
    }

    [Fact]
    public void Decorate_AddsWallDecoAndMossSomewhere()
    {
        var deco = false;
        var moss = false;
        for (var seed = 1; seed <= 30 && !(deco && moss); seed++)
        {
            var level = Generate(seed, 2);
            deco |= level.Map.Contains(Terrain.WallDeco);
            moss |= level.Map.Contains(Terrain.EmptyDeco);
        }

        Assert.True(deco);
        Assert.True(moss);
    }

    [Fact]
    public void TileNames_AreSewerFlavoured()
    {
        var level = new SewerLevel();
        Assert.Equal("Murky water", level.TileName(Terrain.Water));
        Assert.Equal("Murky water", level.TileName(Terrain.WaterTiles));
        Assert.Equal("Wet yellowish moss covers the floor.", level.TileDesc(Terrain.EmptyDeco));
        Assert.Equal("Depth exit", level.TileName(Terrain.Exit));
    }

    [Fact]
    public void Bundle_RoundTripsAGeneratedLeve()
    {
        var level = Generate(5, 2);
        level.Visited[level.Entrance] = true;
        var bundle = new Bundle();
        bundle.Put("level", level);
        var restored = (SewerLevel)bundle.Get("level");
        Assert.Equal(level.Map, restored.Map);
        Assert.Equal(level.Entrance, restored.Entrance);
        Assert.Equal(level.Exit, restored.Exit);
        Assert.True(restored.Visited[level.Entrance]);
        Assert.NotNull(restored.RoomAt(level.Entrance));

    }
}