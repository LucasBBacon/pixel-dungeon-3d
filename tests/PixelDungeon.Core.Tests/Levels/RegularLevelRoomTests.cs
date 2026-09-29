using PixelDungeon.Core.Levels;
using Xunit.Abstractions;

namespace PixelDungeon.Core.Tests.Levels;

public sealed class BareRegularLevel : RegularLevel
{
    public bool TryInitRooms() => InitRooms();

    public bool TryBuild()
    {
        Map = new int[Length];
        Array.Fill(Map, Terrain.Wall);
        return Build();
    }

    public HashSet<Room> RoomSet => Rooms;
    public Room EntranceRoom => RoomEntrance;
    public Room ExitRoom => RoomExit;

    protected override void Decorate()
    {
    }

    protected override bool[] WaterMap() => new bool[Length];

    protected override bool[] GrassMap() => new bool[Length];
}

public class RegularLevelRoomTests : DungeonFixture
{
    [Fact]
    public void InitRooms_SplitsIntoAtLeastEightRoomsInsideTheMap()
    {
        var successes = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            var level = new BareRegularLevel();
            if (!level.TryInitRooms()) continue;
            successes++;
            Assert.True(level.RoomSet.Count >= 8);
            foreach (var room in level.RoomSet)
            {
                Assert.True(room.Left >= 0 &&
                            room.Top >= 0 &&
                            room.Right <= Level.Width - 1 &&
                            room.Bottom <= Level.Height - 1);
                Assert.True(room.Width() >= 2 && room.Height() >= 2);
            }
        }

        Assert.True(successes > 50);
    }

    [Fact]
    public void Build_AssignsEntranceAndExitRooms_AndAtLeastFourStandardRooms()
    {
        Dungeon.Depth = 1;
        var successes = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            var level = new BareRegularLevel();
            if (!level.TryBuild()) continue;
            successes++;
            Assert.Equal(RoomType.Entrance, level.EntranceRoom.Type);
            Assert.Equal(RoomType.Exit, level.ExitRoom.Type);
            Assert.NotSame(level.EntranceRoom, level.ExitRoom);
            Assert.True(level.RoomSet.Count(r => r.Type == RoomType.Standard) >= 4);
            Assert.Equal(Terrain.Entrance, level.Map[level.Entrance]);
            Assert.Equal(Terrain.Exit, level.Map[level.Exit]);
            Assert.Equal(0, level.SecretDoors); // depth 1 never hides doors
            foreach (var d in level.RoomSet
                         .Where(r => r.Type != RoomType.Null)
                         .SelectMany(r => r.Connected.Values))
            {
                Assert.NotNull(d);
            }
        }

        Assert.True(successes > 50);
    }

    [Fact]
    public void Build_AtDepthTwo_HidesSomeDoorsAndPlacesTraps()
    {
        Dungeon.Depth = 2;
        var secret = 0;
        var traps = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            var level = new BareRegularLevel();
            if (!level.TryBuild()) continue;
            secret += level.SecretDoors;
            traps += level.Map
                .Count(t => (Terrain.Flags[t] & Terrain.Secret) != 0 &&
                            t != Terrain.SecretDoor);
        }

        Assert.True(secret > 0);
        Assert.True(traps > 0);
    }

    [Fact]
    public void Build_OnAShopDepth_MakesAShopRoomNextToTheEntrance()
    {
        Dungeon.Depth = 6;
        var successes = 0;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            var level = new BareRegularLevel();
            if (!level.TryBuild()) continue;
            successes++;
            var shop = level.RoomSet.Single(r => r.Type == RoomType.Shop);
            Assert.True(level.EntranceRoom.Connected.ContainsKey(shop));
        }

        Assert.True(successes > 10);
    }
}