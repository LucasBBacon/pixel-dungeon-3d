using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Painters;

public class StandardPainter : Painter
{
    public static void Paint(Level level, Room room)
    {
        Fill(level, room, Terrain.Wall);
        foreach (var door in room.Connected.Values)
        {
            door.Set(DoorType.Regular);
        }

        if (!Dungeon.BossLevel() && Random.Int(5) == 0)
        {
            switch (Random.Int(6))
            {
                case 0:
                    if (level.Feeling != LevelFeeling.Grass)
                    {
                        if (Math.Min(room.Width(), room.Height()) >= 4 &&
                            Math.Max(room.Width(), room.Height()) >= 6)
                        {
                            PaintGraveyard(level, room);
                            return;
                        }

                        break;
                    }
                    else
                    {
                        // Burned room
                        goto case 1;
                    }
                case 1:
                    if (Dungeon.Depth > 1)
                    {
                        PaintBurned(level, room);
                        return;
                    }

                    break;
                case 2:
                    if (Math.Max(room.Width(), room.Height()) >= 4)
                    {
                        PaintStriped(level, room);
                        return;
                    }

                    break;
                case 3:
                    if (room.Width() >= 6 && room.Height() >= 6)
                    {
                        PaintStudy(level, room);
                        return;
                    }

                    break;
                case 4:
                    if (level.Feeling != LevelFeeling.Water)
                    {
                        if (room.Connected.Count == 2 && room.Width() >= 4 && room.Height() >= 4)
                        {
                            PaintBridge(level, room);
                            return;
                        }

                        break;
                    }
                    else
                    {
                        // Fissure
                        goto case 5;
                    }
                case 5:
                    if (!Dungeon.BossLevel() && !Dungeon.BossLevel(Dungeon.Depth + 1) &&
                        Math.Min(room.Width(), room.Height()) >= 5)
                    {
                        PaintFissure(level, room);
                        return;
                    }

                    break;
            }
        }

        Fill(level, room, 1, Terrain.Empty);
    }

    private static void PaintBurned(Level level, Room room)
    {
        for (var i = room.Top + 1; i < room.Bottom; i++)
        {
            for (var j = room.Left + 1; j < room.Right; j++)
            {
                var t = Random.Int(5) switch
                {
                    0 => Terrain.Empty,
                    1 => Terrain.FireTrap,
                    2 => Terrain.SecretFireTrap,
                    3 => Terrain.InactiveTrap,
                    _ => Terrain.Embers
                };

                level.Map[i * Level.Width + j] = t;
            }
        }
    }

    private static void PaintGraveyard(Level level, Room room)
    {
        Fill(level,
            room.Left + 1,
            room.Top + 1,
            room.Width() - 1,
            room.Height() - 1,
            Terrain.Grass);
    }

    private static void PaintStriped(Level level, Room room)
    {
        Fill(level,
            room.Left + 1,
            room.Top + 1,
            room.Width() - 1,
            room.Height() - 1,
            Terrain.EmptySp);

        if (room.Width() > room.Height())
        {
            for (var i = room.Left + 2; i < room.Right; i += 2)
            {
                Fill(level,
                    i,
                    room.Top + 1,
                    1,
                    room.Height() - 1,
                    Terrain.HighGrass);
            }
        }
        else
        {
            for (var i = room.Top + 2; i < room.Bottom; i += 2)
            {
                Fill(level,
                    room.Left + 1,
                    i,
                    room.Width() - 1,
                    1,
                    Terrain.HighGrass);
            }
        }
    }

    private static void PaintStudy(Level level, Room room)
    {
        Fill(level,
            room.Left + 1,
            room.Top + 1,
            room.Width() - 1,
            room.Height() - 1,
            Terrain.Bookshelf);
        Fill(level,
            room.Left + 2,
            room.Top + 2,
            room.Width() - 3,
            room.Height() - 3,
            Terrain.EmptySp);

        foreach (var door in room.Connected.Values)
        {
            if (door.X == room.Left)
            {
                Set(level, door.X + 1, door.Y, Terrain.Empty);
            }
            else if (door.X == room.Right)
            {
                Set(level, door.X - 1, door.Y, Terrain.Empty);
            }
            else if (door.Y == room.Top)
            {
                Set(level, door.X, door.Y + 1, Terrain.Empty);
            }
            else if (door.Y == room.Bottom)
            {
                Set(level, door.X, door.Y - 1, Terrain.Empty);
            }
        }

        Set(level, room.Center(), Terrain.Pedestal);
    }

    private static void PaintBridge(Level level, Room room)
    {
        Fill(level,
            room.Left + 1,
            room.Top + 1,
            room.Width() - 1,
            room.Height() - 1,
            !Dungeon.BossLevel() &&
            !Dungeon.BossLevel(Dungeon.Depth + 1) &&
            Random.Int(3) == 0
                ? Terrain.Chasm
                : Terrain.Water);

        Point door1 = null;
        Point door2 = null;

        foreach (var p in room.Connected.Values)
        {
            if (door1 == null)
            {
                door1 = p;
            }
            else
            {
                door2 = p;
            }
        }

        if ((door1.X == room.Left && door2.X == room.Right) ||
            (door1.X == room.Right && door2.X == room.Left))
        {
            var s = room.Width() / 2;

            DrawInside(level, room, door1, s, Terrain.EmptySp);
            DrawInside(level, room, door2, s, Terrain.EmptySp);

            Fill(level,
                room.Center().X,
                Math.Min(door1.Y, door2.Y),
                1,
                Math.Abs(door1.Y - door2.Y) + 1,
                Terrain.EmptySp);
        }
        else if ((door1.Y == room.Top && door2.Y == room.Bottom) ||
                 (door1.Y == room.Bottom && door2.Y == room.Top))
        {
            var s = room.Height() / 2;

            DrawInside(level, room, door1, s, Terrain.EmptySp);
            DrawInside(level, room, door2, s, Terrain.EmptySp);

            Fill(level,
                Math.Min(door1.X, door2.X),
                room.Center().Y,
                Math.Abs(door1.X - door2.X) + 1,
                1,
                Terrain.EmptySp);
        }
        else if (door1.X == door2.X)
        {
            Fill(level,
                door1.X == room.Left
                    ? room.Left + 1
                    : room.Right - 1,
                Math.Min(door1.Y, door2.Y),
                1,
                Math.Abs(door1.Y - door2.Y) + 1,
                Terrain.EmptySp);
        }
        else if (door1.Y == door2.Y)
        {
            Fill(level,
                Math.Min(door1.X, door2.X),
                door1.Y == room.Top
                    ? room.Top + 1
                    : room.Bottom - 1,
                Math.Abs(door1.X - door2.X) + 1,
                1,
                Terrain.EmptySp);
        }
        else if (door1.Y == room.Top || door1.Y == room.Bottom)
        {
            DrawInside(level, room, door1, Math.Abs(door1.Y - door2.Y), Terrain.EmptySp);
            DrawInside(level, room, door2, Math.Abs(door1.X - door2.X), Terrain.EmptySp);
        }
        else if (door1.X == room.Left || door1.X == room.Right)
        {
            DrawInside(level, room, door1, Math.Abs(door1.X - door2.X), Terrain.EmptySp);
            DrawInside(level, room, door2, Math.Abs(door1.Y - door2.Y), Terrain.EmptySp);
        }
    }

    private static void PaintFissure(Level level, Room room)
    {
        Fill(level, room.Left + 1, room.Top + 1, room.Width() - 1, room.Height() - 1, Terrain.Empty);

        for (var i = room.Top + 2; i < room.Bottom - 1; i++)
        {
            for (var j = room.Left + 2; j < room.Right - 1; j++)
            {
                var v = Math.Min(i - room.Top, room.Bottom - i);
                var h = Math.Min(j - room.Left, room.Right - j);
                if (Math.Min(v, h) > 2 || Random.Int(2) == 0)
                {
                    Set(level, j, i, Terrain.Chasm);
                }
            }
        }
    }
}