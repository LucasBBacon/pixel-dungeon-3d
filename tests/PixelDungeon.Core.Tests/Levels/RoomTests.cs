using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Levels;

public class RoomTests : DungeonFixture
{
    [Fact]
    public void AddNeighbour_RequiresASharedEdgeOfAtLeastThree()
    {
        var a = (Room)new Room().Set(0, 0, 8, 8);
        var b = (Room)new Room().Set(8, 0, 16, 8); // shares x = 8 for 8 cells
        var c = (Room)new Room().Set(8, 7, 16, 12); // shares x = 8 for 1 cell

        a.AddNeighbour(b);
        b.AddNeighbour(c);

        Assert.Contains(b, a.Neighbours);
        Assert.Contains(a, b.Neighbours);
        Assert.DoesNotContain(c, a.Neighbours);
    }

    [Fact]
    public void Connect_IsSymmetric_AndStartsWithoutADoor()
    {
        var a = new Room();
        var b = new Room();

        a.Connect(b);

        Assert.True(a.Connected.ContainsKey(b));
        Assert.True(b.Connected.ContainsKey(a));
        Assert.Null(a.Connected[b]);
    }

    [Fact]
    public void Door_Set_OnlyUpgrades()
    {
        var door = new Room.Door(3, 4);
        door.Set(DoorType.Regular);
        door.Set(DoorType.Tunnel);
        Assert.Equal(DoorType.Regular, door.Type);
        door.Set(DoorType.Locked);
        Assert.Equal(DoorType.Locked, door.Type);
    }

    [Fact]
    public void Inside_ExcludesTheWalls_AndRandomCellStaysInside()
    {
        var room = (Room)new Room().Set(2, 2, 8, 6);
        Assert.True(room.Inside(TestLevel.At(3, 3)));
        Assert.False(room.Inside(TestLevel.At(2, 3)));
        Assert.False(room.Inside(TestLevel.At(8, 3)));
        for (var i = 0; i < 200; i++)
        {
            Assert.True(room.Inside(room.RandomCell()));
            var inner = room.RandomCell(1);
            Assert.True(inner % Level.Width >= 4 && inner % Level.Width <= 6);
            Assert.True(inner / Level.Width >= 4 && inner / Level.Width <= 4);
        }
    }

    [Fact]
    public void Center_ListInsideTheRoom()
    {
        var room = (Room)new Room().Set(2, 2, 9, 7);
        for (var i = 0; i < 50; i++)
        {
            var c = room.Center();
            Assert.True(c.X is >= 5 and <= 6); // odd span, java adds Random.Int(2)
            Assert.True(c.Y is >= 4 and <= 5);
        }
    }

    [Fact]
    public void ShuffleTypes_PermutesSpecials_AndUseTypeMovesToTheEnd()
    {
        var before = new List<RoomType>(Room.Specials);
        Room.ShuffleTypes();
        Assert.Equal(before.Count, Room.Specials.Count);
        Assert.True(before.TrueForAll(Room.Specials.Contains));

        Room.UseType(RoomType.Crypt);
        Assert.Equal(RoomType.Crypt, Room.Specials[^1]);

        Room.ResetSpecials();
        Assert.Equal(RoomType.Armory, Room.Specials[0]);
    }

    [Fact]
    public void SpecialsBundle_RoundTrips_AndMissingKeyShuffles()
    {
        Room.UseType(RoomType.Armory);
        var bundle = new Bundle();
        Room.StoreRoomsInBundle(bundle);
        Room.ResetSpecials();
        Room.RestoreRoomsFromBundle(bundle);
        Assert.Equal(RoomType.Armory, Room.Specials[^1]);
        Room.RestoreRoomsFromBundle(new Bundle());
        Assert.Equal(14, Room.Specials.Count);
    }

    [Fact]
    public void Bundle_RoundTripsRectAndType()
    {
        var room = (Room)new Room().Set(1, 2, 3, 4);
        room.Type = RoomType.Standard;
        var bundle = new Bundle();
        bundle.Put("room", room);
        var restored = (Room)bundle.Get("room");

        Assert.Equal(1, restored.Left);
        Assert.Equal(4, restored.Bottom);
        Assert.Equal(RoomType.Standard, restored.Type);
    }

    [Fact]
    public void RoomPaintersUnportedTypes_PaintAsStandard()
    {
        var level = TestLevel.FromRows("#");
        var room = (Room)new Room().Set(2, 2, 8, 8);
        RoomPainters.Paint(RoomType.Armory, level, room);
        Assert.Equal(Terrain.Wall, level.Map[TestLevel.At(2, 2)]);
        Assert.NotEqual(Terrain.Wall, level.Map[TestLevel.At(5, 5)]);
    }
}