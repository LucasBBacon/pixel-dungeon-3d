using PixelDungeon.Core.Levels.Painters;

namespace PixelDungeon.Core.Levels;

public enum LevelFeeling
{
    None,
    Chasm,
    Water,
    Grass
}

public abstract class Level
{
    public const int Width = 32;
    public const int Height = 32;
    public const int Length = Width * Height;

    public static readonly int[] Neighbours4 =
    [
        -Width,
        +1,
        +Width,
        -1
    ];

    public static readonly int[] Neighbours8 =
    {
        +1,
        -1,
        +Width,
        -Width,
        +1 + Width,
        +1 - Width,
        -1 + Width,
        -1 - Width
    };

    public static readonly int[] Neighbours9 =
    [
        0,
        +1,
        -1,
        +Width,
        -Width,
        +1 + Width,
        +1 - Width,
        -1 + Width,
        -1 - Width
    ];

    protected const float TimeToRespawn = 50;

    private const string TxtHiddenPlateClicks = "A hidden pressure plate clicks!";

    public static bool ResizingNeeded;
    public static int LoadedMapSize;

    public int[] Map;
    public bool[] Visited;
    public bool[] Mapped;

    public int ViewDistance = Dungeon.IsChallenged(Challenges.Darkness) ? 3 : 8;

    public static bool[] FieldOfView = new bool[Length];

    public static bool[] Passable = new bool[Length];
    public static bool[] LosBlocking = new bool[Length];
    public static bool[] Flammable = new bool[Length];
    public static bool[] Secret = new bool[Length];
    public static bool[] Solid = new bool[Length];
    public static bool[] Avoid = new bool[Length];
    public static bool[] Water = new bool[Length];
    public static bool[] Pit = new bool[Length];

    public static bool[] Discoverable = new bool[Length];

    public LevelFeeling Feeling = LevelFeeling.None;

    public int Entrance;
    public int Exit;

    public int Color1 = 0x004400;
    public int Color2 = 0x88CC44;

    protected static bool PitRoomNeeded = false;
    protected static bool WeakFloorCreated = false;

    public int TunnelTile() => Feeling == LevelFeeling.Chasm ? Terrain.EmptySp : Terrain.Empty;

    protected abstract bool Build();
    protected abstract void Decorate();
    protected abstract void CreateMobs();
    protected abstract void CreateItems();

    protected void BuildFlagMaps()
    {
        for (var i = 0; i < Length; i++)
        {
            var flags = Terrain.Flags[Map[i]];
            Passable[i] = (flags & Terrain.Passable) != 0;
            LosBlocking[i] = (flags & Terrain.LosBlocking) != 0;
            Flammable[i] = (flags & Terrain.Flammable) != 0;
            Secret[i] = (flags & Terrain.Secret) != 0;
            Solid[i] = (flags & Terrain.Solid) != 0;
            Avoid[i] = (flags & Terrain.Avoid) != 0;
            Water[i] = (flags & Terrain.Liquid) != 0;
            Pit[i] = (flags & Terrain.Pit) != 0;
        }

        const int lastRow = Length - Width;
        for (var i = 0; i < Width; i++)
        {
            Passable[i] = Avoid[i] = false;
            Passable[lastRow + i] = Avoid[lastRow + i] = false;
        }

        for (var i = Width; i < lastRow; i += Width)
        {
            Passable[i] = Avoid[i] = false;
            Passable[i + Width - 1] = Avoid[i + Width - 1] = false;
        }

        for (var i = Width; i < Length - Width; i++)
        {
            if (Water[i])
            {
                Map[i] = GetWaterTile(i);
            }

            if (!Pit[i]) continue;

            if (Pit[i - Width]) continue;

            var c = Map[i - Width];
            if (c is Terrain.EmptySp or Terrain.StatueSp)
            {
                Map[i] = Terrain.ChasmFloorSp;
            }
            else if (Water[i - Width])
            {
                Map[i] = Terrain.ChasmWater;
            }
            else if ((Terrain.Flags[c] & Terrain.Unstitchable) != 0)
            {
                Map[i] = Terrain.ChasmWall;
            }
            else
            {
                Map[i] = Terrain.ChasmFloor;
            }
        }
    }

    private int GetWaterTile(int pos)
    {
        var t = Terrain.WaterTiles;
        for (var j = 0; j < Neighbours4.Length; j++)
        {
            if ((Terrain.Flags[Map[pos + Neighbours4[j]]] & Terrain.Unstitchable) != 0)
            {
                t += 1 << j;
            }
        }

        return t;
    }

    public void Destroy(int pos)
    {
        if ((Terrain.Flags[Map[pos]] & Terrain.Unstitchable) == 0)
        {
            Set(pos, Terrain.Embers);
        }
        else
        {
            var flood = Neighbours4.Any(t => Water[pos + t]);
            Set(pos, flood ? GetWaterTile(pos) : Terrain.Embers);
        }
    }

    protected void CleanWalls()
    {
        for (var i = 0; i < Length; i++)
        {
            var d = Neighbours9
                .Select(t => i + t)
                .Any(n => n is >= 0 and < Length &&
                          Map[n] != Terrain.Wall && Map[n] != Terrain.WallDeco);

            if (d)
            {
                d = Neighbours9
                    .Select(t => i + t)
                    .Any(n => n is >= 0 and < Length && !Pit[n]);
            }

            Discoverable[i] = d;
        }
    }

    public static void Set(int cell, int terrain)
    {
        Painter.Set(Dungeon.Level, cell, terrain);

        var flags = Terrain.Flags[terrain];
        Passable[cell] = (flags & Terrain.Passable) != 0;
        LosBlocking[cell] = (flags & Terrain.LosBlocking) != 0;
        Flammable[cell] = (flags & Terrain.Flammable) != 0;
        Secret[cell] = (flags & Terrain.Secret) != 0;
        Solid[cell] = (flags & Terrain.Solid) != 0;
        Avoid[cell] = (flags & Terrain.Avoid) != 0;
        Pit[cell] = (flags & Terrain.Pit) != 0;
        Water[cell] = terrain is Terrain.Water or >= Terrain.WaterTiles;
    }

    public static int Distance(int a, int b)
    {
        var ax = a % Width;
        var ay = a / Width;
        var bx = b % Width;
        var by = b / Width;
        return Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));
    }

    public static bool Adjacent(int a, int b)
    {
        var diff = Math.Abs(a - b);
        return diff is 1 or Width or Width + 1 or Width - 1;
    }


    public virtual string TileName(int tile)
    {
        if (tile >= Terrain.WaterTiles && tile != Terrain.Water)
        {
            return TileName(Terrain.Water);
        }

        if (tile != Terrain.Chasm && (Terrain.Flags[tile] & Terrain.Pit) != 0)
        {
            return TileName(Terrain.Chasm);
        }

        return tile switch
        {
            Terrain.Chasm => "Chasm",
            Terrain.Empty or Terrain.EmptySp or Terrain.EmptyDeco or Terrain.SecretToxicTrap or Terrain.SecretFireTrap
                or Terrain.SecretParalyticTrap or Terrain.SecretPoisonTrap or Terrain.SecretAlarmTrap
                or Terrain.SecretLightningTrap => "Floor",
            Terrain.Grass => "Grass",
            Terrain.Water => "Water",
            Terrain.Wall or Terrain.WallDeco or Terrain.SecretDoor => "Wall",
            Terrain.Door => "Closed door",
            Terrain.OpenDoor => "Open door",
            Terrain.Entrance => "Depth entrance",
            Terrain.Exit => "Depth exit",
            Terrain.Embers => "Embers",
            Terrain.LockedDoor => "Locked door",
            Terrain.Pedestal => "Pedestal",
            Terrain.Barricade => "Barricade",
            Terrain.HighGrass => "High grass",
            Terrain.LockedExit => "Locked depth exit",
            Terrain.UnlockedExit => "Unlocked depth exit",
            Terrain.Sign => "Sign",
            Terrain.Well => "Well",
            Terrain.EmptyWell => "Empty well",
            Terrain.Statue or Terrain.StatueSp => "Statue",
            Terrain.ToxicTrap => "Toxic gas trap",
            Terrain.FireTrap => "Fire trap",
            Terrain.ParalyticTrap => "Paralytic gas trap",
            Terrain.PoisonTrap => "Poison dart trap",
            Terrain.AlarmTrap => "Alarm trap",
            Terrain.LightningTrap => "Lightning trap",
            Terrain.GrippingTrap => "Gripping trap",
            Terrain.SummoningTrap => "Summoning trap",
            Terrain.InactiveTrap => "Triggered trap",
            Terrain.Bookshelf => "Bookshelf",
            Terrain.Alchemy => "Alchemy pot",
            _ => "???"
        };
    }


    public virtual string TileDesc(int tile)
    {
        switch (tile)
        {
            case Terrain.Chasm:
                return "You can't see the bottom.";
            case Terrain.Water:
                return "In case of burning step into the water to extinguish the fire.";
            case Terrain.Entrance:
                return "Stairs lead up to the upper depth.";
            case Terrain.Exit:
            case Terrain.UnlockedExit:
                return "Stairs lead down to the lower depth.";
            case Terrain.Embers:
                return "Embers cover the floor.";
            case Terrain.HighGrass:
                return "Dense vegetation blocks the view.";
            case Terrain.LockedDoor:
                return "This door is locked, you need a matching key to unlock it.";
            case Terrain.LockedExit:
                return "Heavy bars block the stairs leading down.";
            case Terrain.Barricade:
                return "The wooden barricade is firmly set but has dried over the years. Might it burn?";
            case Terrain.Sign:
                return "You can't read the text from here.";
            case Terrain.ToxicTrap:
            case Terrain.FireTrap:
            case Terrain.ParalyticTrap:
            case Terrain.PoisonTrap:
            case Terrain.AlarmTrap:
            case Terrain.LightningTrap:
            case Terrain.GrippingTrap:
            case Terrain.SummoningTrap:
                return "Stepping onto a hidden pressure plate will activate the trap.";
            case Terrain.InactiveTrap:
                return "The trap has been triggered before and it's not dangerous anymore.";
            case Terrain.Statue:
            case Terrain.StatueSp:
                return "Someone wanted to adorn this place, but failed, obviously.";
            case Terrain.Alchemy:
                return "Drop some seeds here to cook a potion.";
            case Terrain.EmptyWell:
                return "The well has run dry.";
            default:
                if (tile >= Terrain.WaterTiles)
                {
                    return TileDesc(Terrain.Water);
                }

                return (Terrain.Flags[tile] & Terrain.Pit) != 0 ? TileDesc(Terrain.Chasm) : "";
        }
    }
}