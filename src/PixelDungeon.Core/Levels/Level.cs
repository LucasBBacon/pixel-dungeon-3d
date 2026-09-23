using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Plants;

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
    [
        +1,
        -1,
        +Width,
        -Width,
        +1 + Width,
        +1 - Width,
        -1 + Width,
        -1 - Width
    ];

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

    public HashSet<Mob> Mobs = [];
    public Dictionary<int, Heap> Heaps = new();
    public Dictionary<Type, Blob> Blobs = new();
    public Dictionary<int, Plant> Plants = new();

    protected List<Item> ItemsToSpawn = [];

    public int Color1 = 0x004400;
    public int Color2 = 0x88CC44;

    protected static bool PitRoomNeeded = false;
    protected static bool WeakFloorCreated = false;

    public int TunnelTile() => Feeling == LevelFeeling.Chasm ? Terrain.EmptySp : Terrain.Empty;

    public static void ResetStatics()
    {
        Array.Fill(FieldOfView, false);
        Array.Fill(Passable, false);
        Array.Fill(LosBlocking, false);
        Array.Fill(Flammable, false);
        Array.Fill(Secret, false);
        Array.Fill(Solid, false);
        Array.Fill(Avoid, false);
        Array.Fill(Water, false);
        Array.Fill(Pit, false);
        Array.Fill(Discoverable, false);
        PitRoomNeeded = false;
        WeakFloorCreated = false;
        ResizingNeeded = false;
        LoadedMapSize = 0;
    }

    public virtual int RandomRespawnCell()
    {
        int cell;
        do
        {
            cell = Random.Int(Length);
        } while (!Passable[cell] || Dungeon.Visible[cell] || Actor.FindChar(cell) != null);

        return cell;
    }

    public virtual int RandomDestination()
    {
        int cell;
        do
        {
            cell = Random.Int(Length);
        } while (!Passable[cell]);

        return cell;
    }

    public virtual int PitCell() => RandomRespawnCell();

    public virtual int NMobs() => 0;

    public bool[] UpdateFieldOfView(Char c)
    {
        var cx = c.Pos % Width;
        var cy = c.Pos / Width;

        var sighted = c.IsAlive();
        if (sighted)
        {
            ShadowCaster.CastShadow(cx, cy, FieldOfView, c.ViewDistance);
        }
        else
        {
            Array.Fill(FieldOfView, false);
        }

        var sense = 1;

        if ((sighted && sense > 1) || !sighted)
        {
            var ax = Math.Max(0, cx - sense);
            var bx = Math.Min(cx + sense, Width - 1);
            var ay = Math.Max(0, cy - sense);
            var by = Math.Min(cy + sense, Height - 1);

            var len = bx - ax + 1;
            var pos = ax + ay * Width;
            for (var y = ay; y <= by; y++, pos += Width)
            {
                Array.Fill(FieldOfView, true, pos, len);
            }

            for (var i = 0; i < Length; i++)
            {
                FieldOfView[i] &= Discoverable[i];
            }
        }

        if (c.IsAlive())
        {
            // DEFERRED(sp2): MindVision reveals the 3x3 block around every mob and takes precedence over the Huntress branch
            if (c == Dungeon.Hero && ((Hero)c).HeroClass == HeroClass.Huntress)
            {
                foreach (var mob in Mobs)
                {
                    var p = mob.Pos;
                    if (Distance(c.Pos, p) == 2)
                    {
                        FieldOfView[p] = true;
                        FieldOfView[p + 1] = true;
                        FieldOfView[p - 1] = true;
                        FieldOfView[p + Width + 1] = true;
                        FieldOfView[p + Width - 1] = true;
                        FieldOfView[p - Width + 1] = true;
                        FieldOfView[p - Width - 1] = true;
                        FieldOfView[p + Width] = true;
                        FieldOfView[p - Width] = true;
                    }
                }
            }
            // DEFERRED(sp4): Awareness (from the well of awareness) reveals the 3x3 block around every heap
        }

        return FieldOfView;
    }

    protected abstract bool Build();
    protected abstract void Decorate();
    protected abstract void CreateMobs();
    protected abstract void CreateItems();

    // Private in the Java; protected so Create() and test levels can call it.
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

        switch (tile)
        {
            case Terrain.Chasm:
                return "Chasm";
            case Terrain.Empty:
            case Terrain.EmptySp:
            case Terrain.EmptyDeco:
            case Terrain.SecretToxicTrap:
            case Terrain.SecretFireTrap:
            case Terrain.SecretParalyticTrap:
            case Terrain.SecretPoisonTrap:
            case Terrain.SecretAlarmTrap:
            case Terrain.SecretLightningTrap:
                return "Floor";
            case Terrain.Grass:
                return "Grass";
            case Terrain.Water:
                return "Water";
            case Terrain.Wall:
            case Terrain.WallDeco:
            case Terrain.SecretDoor:
                return "Wall";
            case Terrain.Door:
                return "Closed door";
            case Terrain.OpenDoor:
                return "Open door";
            case Terrain.Entrance:
                return "Depth entrance";
            case Terrain.Exit:
                return "Depth exit";
            case Terrain.Embers:
                return "Embers";
            case Terrain.LockedDoor:
                return "Locked door";
            case Terrain.Pedestal:
                return "Pedestal";
            case Terrain.Barricade:
                return "Barricade";
            case Terrain.HighGrass:
                return "High grass";
            case Terrain.LockedExit:
                return "Locked depth exit";
            case Terrain.UnlockedExit:
                return "Unlocked depth exit";
            case Terrain.Sign:
                return "Sign";
            case Terrain.Well:
                return "Well";
            case Terrain.EmptyWell:
                return "Empty well";
            case Terrain.Statue:
            case Terrain.StatueSp:
                return "Statue";
            case Terrain.ToxicTrap:
                return "Toxic gas trap";
            case Terrain.FireTrap:
                return "Fire trap";
            case Terrain.ParalyticTrap:
                return "Paralytic gas trap";
            case Terrain.PoisonTrap:
                return "Poison dart trap";
            case Terrain.AlarmTrap:
                return "Alarm trap";
            case Terrain.LightningTrap:
                return "Lightning trap";
            case Terrain.GrippingTrap:
                return "Gripping trap";
            case Terrain.SummoningTrap:
                return "Summoning trap";
            case Terrain.InactiveTrap:
                return "Triggered trap";
            case Terrain.Bookshelf:
                return "Bookshelf";
            case Terrain.Alchemy:
                return "Alchemy pot";
            default:
                return "???";
        }
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