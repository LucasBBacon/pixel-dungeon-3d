using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels;

public enum LevelFeeling
{
    None,
    Chasm,
    Water,
    Grass
}

public abstract class Level : IBundlable
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

    public void Create()
    {
        ResizingNeeded = false;

        Map = new int[Length];
        Visited = new bool[Length];
        Mapped = new bool[Length];

        Mobs = new HashSet<Mob>();
        Heaps = new Dictionary<int, Heap>();
        Blobs = new Dictionary<Type, Blob>();
        Plants = new Dictionary<int, Plant>();

        if (!Dungeon.BossLevel())
        {
            AddItemToSpawn(Generator.Random(Generator.Category.Food));
            if (Dungeon.PosNeeded())
            {
                // TODO: addItemToSpawn(new PotionOfStrength())
                Dungeon.PotionOfStrength++;
            }

            if (Dungeon.SouNeeded())
            {
                // TODO: addItemToSpawn(new ScrollOfUpgrade())
                Dungeon.ScrollsOfUpgrade++;
            }

            if (Dungeon.SoeNeeded())
            {
                // TODO: addItemToSpawn(new ScrollOfEnchantment())
                Dungeon.ScrollsOfEnchantment++;
            }

            if (Dungeon.Depth > 1)
            {
                switch (Random.Int(10))
                {
                    case 0:
                        if (!Dungeon.BossLevel(Dungeon.Depth + 1))
                        {
                            Feeling = LevelFeeling.Chasm;
                        }

                        break;
                    case 1:
                        Feeling = LevelFeeling.Water;
                        break;
                    case 2:
                        Feeling = LevelFeeling.Grass;
                        break;
                }
            }
        }

        var pitNeeded = Dungeon.Depth > 1 && WeakFloorCreated;

        do
        {
            Array.Fill(Map, Feeling == LevelFeeling.Chasm ? Terrain.Chasm : Terrain.Wall);

            PitRoomNeeded = pitNeeded;
            WeakFloorCreated = false;
        } while (!Build());

        Decorate();

        BuildFlagMaps();
        CleanWalls();

        CreateMobs();
        CreateItems();
    }

    public void Reset()
    {
        foreach (var mob in Mobs.ToArray())
        {
            if (!mob.Reset())
            {
                Mobs.Remove(mob);
            }
        }

        CreateMobs();
    }

    private const string TagMap = "map";
    private const string TagVisited = "visited";
    private const string TagMapped = "mapped";
    private const string TagEntrance = "entrance";
    private const string TagExit = "exit";
    private const string TagHeaps = "heaps";
    private const string TagPlants = "plants";
    private const string TagMobs = "mobs";
    private const string TagBlobs = "blobs";

    public virtual void RestoreFromBundle(Bundle bundle)
    {
        Mobs = new HashSet<Mob>();
        Heaps = new Dictionary<int, Heap>();
        Blobs = new Dictionary<Type, Blob>();
        Plants = new Dictionary<int, Plant>();

        Map = bundle.GetIntArray(TagMap);
        Visited = bundle.GetBooleanArray(TagVisited);
        Mapped = bundle.GetBooleanArray(TagMapped);

        Entrance = bundle.GetInt(TagEntrance);
        Exit = bundle.GetInt(TagExit);

        WeakFloorCreated = false;

        AdjustMapSize();

        foreach (var heap in bundle.GetCollection(TagHeaps).Cast<Heap>())
        {
            if (ResizingNeeded)
            {
                heap.Pos = AdjustPos(heap.Pos);
            }

            Heaps[heap.Pos] = heap;
        }

        foreach (var plant in bundle.GetCollection(TagPlants).Cast<Plant>())
        {
            if (ResizingNeeded)
            {
                plant.Pos = AdjustPos(plant.Pos);
            }

            Plants[plant.Pos] = plant;
        }

        foreach (var mob in bundle.GetCollection(TagMobs).Cast<Mob>().Where(mob => mob != null))
        {
            if (ResizingNeeded)
            {
                mob.Pos = AdjustPos(mob.Pos);
            }

            Mobs.Add(mob);
        }

        foreach (var blob in bundle.GetCollection(TagBlobs).Cast<Blob>())
        {
            Blobs[blob.GetType()] = blob;
        }

        BuildFlagMaps();
        CleanWalls();
    }

    public virtual void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagMap, Map);
        bundle.Put(TagVisited, Visited);
        bundle.Put(TagMapped, Mapped);
        bundle.Put(TagEntrance, Entrance);
        bundle.Put(TagExit, Exit);
        bundle.Put(TagHeaps, Heaps.Values);
        bundle.Put(TagPlants, Plants.Values);
        bundle.Put(TagMobs, Mobs);
        bundle.Put(TagBlobs, Blobs.Values);
    }

    private void AdjustMapSize()
    {
        if (Map.Length < Length)
        {
            ResizingNeeded = true;
            LoadedMapSize = (int)Math.Sqrt(Map.Length);

            var map = new int[Length];
            Array.Fill(map, Terrain.Wall);

            var visited = new bool[Length];
            var mapped = new bool[Length];

            for (var i = 0; i < LoadedMapSize; i++)
            {
                Array.Copy(Map,
                    i * LoadedMapSize,
                    map,
                    i * Width,
                    LoadedMapSize);
                Array.Copy(Visited,
                    i * LoadedMapSize,
                    visited,
                    i * Width,
                    LoadedMapSize);
                Array.Copy(Mapped,
                    i * LoadedMapSize,
                    mapped,
                    i * Width,
                    LoadedMapSize);
            }

            Map = map;
            Visited = visited;
            Mapped = mapped;

            Entrance = AdjustPos(Entrance);
            Exit = AdjustPos(Exit);
        }
        else
        {
            ResizingNeeded = false;
        }
    }

    public int AdjustPos(int pos) => pos / LoadedMapSize * Width + pos % LoadedMapSize;

    public virtual Actor Respawner()
    {
        return new RespawnerActor(this);
    }

    // an anonymous subclass
    private sealed class RespawnerActor : Actor
    {
        private readonly Level _level;

        public RespawnerActor(Level level)
        {
            _level = level;
        }

        protected override bool Act()
        {
            if (_level.Mobs.Count < _level.NMobs())
            {
                var mob = Bestiary.Mutable(Dungeon.Depth);
                if (mob != null) // bestiary returns null at depths whose mobs are not ported yet
                {
                    // TODO: mob.state = mob.Wandering
                    mob.Pos = _level.RandomRespawnCell();
                    if (Dungeon.Hero.IsAlive() && mob.Pos != -1)
                    {
                        // TODO: GameScene.Add(mob); mob.Beckon(Dungeon.Hero.Pos) when amulet obtained
                    }
                }
            }

            Spend(Dungeon.NightMode || Statistics.AmuletObtained ? TimeToRespawn / 2 : TimeToRespawn);
            return true;
        }
    }

    public void AddItemToSpawn(Item item)
    {
        if (item != null)
        {
            ItemsToSpawn.Add(item);
        }
    }

    public Item ItemToSpawnAsPrize()
    {
        if (Random.Int(ItemsToSpawn.Count + 1) <= 0) return null;

        var item = Random.Element(ItemsToSpawn);
        ItemsToSpawn.Remove(item);
        return item;
    }

    public Heap Drop(Item item, int cell)
    {
        // TODO: NoFood, NoArmor, NoHealing, NoHerbalism, NoScrolls challenges replace item with gold
        if (Map[cell] == Terrain.Alchemy && item is not Plant.Seed)
        {
            int n;
            do
            {
                n = cell + Neighbours8[Random.Int(8)];
            } while (Map[n] != Terrain.EmptySp);

            cell = n;
        }

        var heap = Heaps.GetValueOrDefault(cell);
        if (heap == null)
        {
            heap = new Heap
            {
                Pos = cell
            };
            if (Map[cell] == Terrain.Chasm || (Dungeon.Level != null && Pit[cell]))
            {
                Dungeon.DropToChasm(item);
                GameScene.Discard(heap);
            }
            else
            {
                Heaps[cell] = heap;
                GameScene.Add(heap);
            }
        }
        else if (heap.Type is HeapType.LockedChest or HeapType.CrystalChest)
        {
            int n;
            do
            {
                n = cell + Neighbours8[Random.Int(8)];
            } while (!Passable[n] && !Avoid[n]);

            return Drop(item, n);
        }

        heap.Drop(item);

        if (Dungeon.Level != null)
        {
            Press(cell, null);
        }

        return heap;
    }

    public Plant PlantSeed(Plant.Seed seed, int pos)
    {
        var plant = Plants.GetValueOrDefault(pos);
        plant?.Wither();

        plant = seed.Couch(pos);
        Plants[pos] = plant;

        // TODO: GameScene.Add(plant)

        return plant;
    }

    public void Uproot(int pos) => Plants.Remove(pos);

    public void Press(int cell, Char ch)
    {
        if (Pit[cell] && ch == Dungeon.Hero)
        {
            Chasm.HeroFall(cell);
            return;
        }

        var trap = false;

        switch (Map[cell])
        {
            case Terrain.SecretToxicTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.ToxicTrap;
            case Terrain.ToxicTrap:
                trap = true;
                // TODO: ToxicTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretFireTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.FireTrap;
            case Terrain.FireTrap:
                trap = true;
                // TODO: FireTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretParalyticTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.ParalyticTrap;
            case Terrain.ParalyticTrap:
                trap = true;
                // TODO: ParalyticTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretPoisonTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.PoisonTrap;
            case Terrain.PoisonTrap:
                trap = true;
                // TODO: PoisonTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretAlarmTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.AlarmTrap;
            case Terrain.AlarmTrap:
                trap = true;
                // TODO: AlarmTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretLightningTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.LightningTrap;
            case Terrain.LightningTrap:
                trap = true;
                // TODO: LightningTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretGrippingTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.GrippingTrap;
            case Terrain.GrippingTrap:
                trap = true;
                // TODO: GrippingTrap.Trigger(cell, ch)
                break;

            case Terrain.SecretSummoningTrap:
                GLog.I(TxtHiddenPlateClicks);
                goto case Terrain.SummoningTrap;
            case Terrain.SummoningTrap:
                trap = true;
                // TODO: SummoningTrap.Trigger(cell, ch)
                break;

            case Terrain.HighGrass:
                HighGrass.Trample(this, cell, ch);
                break;

            case Terrain.Well:
                // TODO: WellWater.AffectCell(cell);
                break;

            case Terrain.Alchemy:
                if (ch == null)
                {
                    // TODO: Alchemy.Transmute(cell)
                }

                break;

            case Terrain.Door:
                Door.Enter(cell);
                break;
        }

        if (trap)
        {
            Sample.Play(Assets.SndTrap);
            if (ch == Dungeon.Hero)
            {
                Dungeon.Hero.Interrupt();
            }

            Set(cell, Terrain.InactiveTrap);
            GameScene.UpdateMap(cell);
        }

        var plant = Plants.GetValueOrDefault(cell);
        plant?.Activate(ch);
    }

    public void MobPress(Mob mob)
    {
        var cell = mob.Pos;

        if (Pit[cell] && !mob.Flying)
        {
            Chasm.MobFall(mob);
            return;
        }

        var trap = true;
        switch (Map[cell])
        {
            case Terrain.ToxicTrap:
                // TODO: trigger trap
                break;
            case Terrain.FireTrap:
                // TODO: trigger trap
                break;
            case Terrain.ParalyticTrap:
                // TODO: trigger trap
                break;
            case Terrain.PoisonTrap:
                // TODO: trigger trap
                break;
            case Terrain.AlarmTrap:
                // TODO: trigger trap
                break;
            case Terrain.LightningTrap:
                // TODO: trigger trap
                break;
            case Terrain.GrippingTrap:
                // TODO: trigger trap
                break;
            case Terrain.SummoningTrap:
                // TODO: trigger trap
                break;
            case Terrain.Door:
                Door.Enter(cell);
                trap = false; // java falls through into default here
                break;
            default:
                trap = false;
                break;
        }

        if (trap)
        {
            if (Dungeon.Visible[cell])
            {
                Sample.Play(Assets.SndTrap);
            }

            Set(cell, Terrain.InactiveTrap);
            GameScene.UpdateMap(cell);
        }

        var plant = Plants.GetValueOrDefault(cell);
        plant?.Activate(mob);
    }

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

        const int sense = 1;

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

        if (!c.IsAlive() ||
            c != Dungeon.Hero ||
            ((Hero)c).HeroClass != HeroClass.Huntress)
            return FieldOfView;

        foreach (var p in Mobs
                     .Select(mob => mob.Pos)
                     .Where(p => Distance(c.Pos, p) == 2))
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