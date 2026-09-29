namespace PixelDungeon.Core.Levels.Painters;

public class EntrancePainter : Painter
{
    public static void Paint(Level level, Room room)
    {
        Fill(level, room, Terrain.Wall);
        Fill(level, room, 1, Terrain.Empty);

        foreach (var door in room.Connected.Values)
        {
            door.Set(DoorType.Regular);
        }

        level.Entrance = room.RandomCell(1);
        Set(level, level.Entrance, Terrain.Entrance);
    }
}