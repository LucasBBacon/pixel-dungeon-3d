namespace PixelDungeon.Core.Levels.Painters;

public class ExitPainter : Painter
{
    public static void Paint(Level level, Room room)
    {
        Fill(level, room, Terrain.Wall);
        Fill(level, room, 1, Terrain.Empty);

        foreach (var door in room.Connected.Values)
        {
            door.Set(DoorType.Regular);
        }

        level.Exit = room.RandomCell(1);
        Set(level, level.Exit, Terrain.Exit);
    }
}