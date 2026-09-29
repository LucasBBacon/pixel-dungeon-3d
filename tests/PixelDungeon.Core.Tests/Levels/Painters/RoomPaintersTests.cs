using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Painters;

namespace PixelDungeon.Core.Tests.Levels.Painters;

public class RoomPaintersTests : DungeonFixture
{
    private static Room RoomWithDoors(params (int X, int Y)[] doors)
    {
        var room = (Room)new Room().Set(2, 2, 10, 8);
        foreach (var (x, y) in doors)
        {
            var other = new Room();
            room.Connected[other] = new Room.Door(x, y);
        }

        return room;
    }

    [Fact]
    public void TunnelPainter_ConnectsEveryDoorToTheCentreLine()
    {
        var level = TestLevel.FromRows("#");
        var room = RoomWithDoors((2, 5), (10, 4), (6, 2), (7, 8));
        TunnelPainter.Paint(level, room);

        foreach (var door in room.Connected.Values)
        {
            Assert.Equal(DoorType.Tunnel, door.Type);
        }

        // the room is wider than tall, so the spine is the center row (y = 5)
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(3, 5)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(9, 5)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(6, 3)]);
        Assert.Equal(Terrain.Empty, level.Map[TestLevel.At(7, 7)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(2, 5)]); // doors painted by RegularLevel
    }

    [Fact]
    public void EntranceAndExitPainters_PlaceStairsStrictlyInside()
    {
        var level = TestLevel.FromRows("#");
        var room = RoomWithDoors((2, 5));
        EntrancePainter.Paint(level, room);
        Assert.Equal(Terrain.Entrance, level.Map[level.Entrance]);
        Assert.True(level.Entrance % Level.Width >= 4 && level.Entrance % Level.Width <= 8);
        Assert.True(level.Entrance / Level.Width >= 4 && level.Entrance / Level.Width <= 6);
        Assert.Equal(DoorType.Regular, room.Connected.Values.First().Type);

        ExitPainter.Paint(level, room);
        Assert.Equal(Terrain.Exit, level.Map[level.Exit]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(2, 2)]);
    }

    [Fact]
    public void StandardPainter_KeepsWallsAndSetsRegularDoors()
    {
        var level = TestLevel.FromRows("#");
        var room = RoomWithDoors((2, 5), (10, 5));
        StandardPainter.Paint(level, room);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(2, 2)]);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(10, 8)]);
        Assert.NotEqual(Terrain.Wall, level.Map[TestLevel.At(6, 5)]);
        foreach (var door in room.Connected.Values)
        {
            Assert.Equal(DoorType.Regular, door.Type);
        }
    }
}