using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels;

public abstract class RegularLevel : Level
{
    protected HashSet<Room> Rooms;

    protected Room RoomEntrance;
    protected Room RoomExit;

    protected List<RoomType> Specials;

    public int SecretDoors;

    protected override bool Build()
    {
        if (!InitRooms())
        {
            return false;
        }

        int distance;
        var retry = 0;
        var minDistance = (int)Math.Sqrt(Rooms.Count);
        do
        {
            do
            {
                RoomEntrance = Random.Element(Rooms);
            } while (RoomEntrance.Width() < 4 || RoomEntrance.Height() < 4);

            do
            {
                RoomExit = Random.Element(Rooms);
            } while (RoomExit == RoomEntrance ||
                     RoomExit.Width() < 4 ||
                     RoomExit.Height() < 4);

            Graph.BuildDistanceMap(Rooms, RoomExit);
            distance = RoomEntrance.Distance;

            if (retry++ > 10)
            {
                return false;
            }
        } while (distance < minDistance);

        RoomEntrance.Type = RoomType.Entrance;
        RoomExit.Type = RoomType.Exit;

        var connected = new HashSet<Room> { RoomEntrance };

        Graph.BuildDistanceMap(Rooms, RoomExit);
        var path = Graph.BuildPath(Rooms, RoomEntrance, RoomExit);

        var room = RoomEntrance;
        foreach (var next in path)
        {
            room.Connect(next);
            room = next;
            connected.Add(room);
        }

        Graph.SetPrice(path, RoomEntrance.Distance);

        Graph.BuildDistanceMap(Rooms, RoomExit);
        path = Graph.BuildPath(Rooms, RoomEntrance, RoomExit);

        room = RoomEntrance;
        foreach (var next in path)
        {
            room.Connect(next);
            room = next;
            connected.Add(room);
        }

        var nConnected = (int)(Rooms.Count * Random.Float(0.5f, 0.7f));
        while (connected.Count < nConnected)
        {
            var currentRoom = Random.Element(connected);
            var otherRoom = Random.Element(currentRoom.Neighbours);

            if (connected.Contains(otherRoom)) continue;

            currentRoom.Connect(otherRoom);
            connected.Add(otherRoom);
        }

        if (Dungeon.ShopOnLevel())
        {
            var shop = RoomEntrance
                .Connected
                .Keys
                .FirstOrDefault(shopRoom => shopRoom.Connected.Count == 1 &&
                                            shopRoom.Width() >= 5 &&
                                            shopRoom.Height() >= 5);

            if (shop == null) return false;

            shop.Type = RoomType.Shop;
        }

        Specials = [.. Room.Specials];
        if (Dungeon.BossLevel(Dungeon.Depth + 1))
        {
            Specials.Remove(RoomType.WeakFloor);
        }

        AssignRoomType();

        Paint();
        PaintWater();
        PaintGrass();

        PlaceTraps();

        return true;
    }

    protected bool InitRooms()
    {
        Rooms = [];
        Split(new Rect(0, 0, Width - 1, Height - 1));

        if (Rooms.Count < 8)
        {
            return false;
        }

        var ra = Rooms.ToArray();
        for (var i = 0; i < ra.Length - 1; i++)
        for (var j = i + 1; j < ra.Length; j++)
            ra[i].AddNeighbour(ra[j]);

        return true;
    }

    protected void AssignRoomType()
    {
        var specialRooms = 0;

        foreach (var room in
                 Rooms.Where(r => r.Type == RoomType.Null &&
                                  r.Connected.Count == 1))
        {
            if (Specials.Count > 0 &&
                room.Width() > 3 && room.Height() > 3 &&
                Random.Int(specialRooms * specialRooms + 2) == 0)
            {
                if (PitRoomNeeded)
                {
                    room.Type = RoomType.Pit;
                    PitRoomNeeded = false;

                    Specials.Remove(RoomType.Armory);
                    Specials.Remove(RoomType.Crypt);
                    Specials.Remove(RoomType.Laboratory);
                    Specials.Remove(RoomType.Library);
                    Specials.Remove(RoomType.Statue);
                    Specials.Remove(RoomType.Treasury);
                    Specials.Remove(RoomType.Vault);
                    Specials.Remove(RoomType.WeakFloor);
                }
                else if (Dungeon.Depth % 5 == 2 && Specials.Contains(RoomType.Laboratory))
                {
                    room.Type = RoomType.Laboratory;
                }
                else
                {
                    var n = Specials.Count;
                    room.Type = Specials[Math.Min(Random.Int(n), Random.Int(n))];
                    if (room.Type == RoomType.WeakFloor)
                    {
                        WeakFloorCreated = true;
                    }
                }

                Room.UseType(room.Type);
                Specials.Remove(room.Type);
                specialRooms++;
            }
            else if (Random.Int(2) == 0)
            {
                var neighbours = new HashSet<Room>();
                foreach (var neighbour in
                         room.Neighbours.Where(neighbour =>
                             !room.Connected.ContainsKey(neighbour) &&
                             !Room.Specials.Contains(neighbour.Type) &&
                             neighbour.Type != RoomType.Pit))
                    neighbours.Add(neighbour);

                if (neighbours.Count > 1)
                    room.Connect(Random.Element(neighbours));
            }
        }

        var count = 0;
        foreach (var room in Rooms)
        {
            if (room.Type != RoomType.Null) continue;

            var connections = room.Connected.Count;
            if (connections == 0)
            {
            }
            else if (Random.Int(connections * connections) == 0)
            {
                room.Type = RoomType.Standard;
                count++;
            }
            else
            {
                room.Type = RoomType.Tunnel;
            }
        }

        while (count < 4)
        {
            var room = RandomRoom(RoomType.Tunnel, 1);

            if (room == null) continue;

            room.Type = RoomType.Standard;
            count++;
        }
    }

    protected void PaintWater()
    {
        bool[] lake = WaterMap();
        for (int i = 0; i < Length; i++)
        {
            if (Map[i] == Terrain.Empty && lake[i])
            {
                Map[i] = Terrain.Water;
            }
        }
    }

    protected void PaintGrass()
    {
        var grass = GrassMap();

        if (Feeling == LevelFeeling.Grass)
        {
            foreach (var room in Rooms.Where(room => room.Type is not (
                         RoomType.Null or
                         RoomType.Passage or
                         RoomType.Tunnel)))
            {
                grass[(room.Left + 1) + (room.Top + 1) * Width] = true;
                grass[(room.Right - 1) + (room.Top + 1) * Width] = true;
                grass[(room.Left + 1) + (room.Bottom - 1) * Width] = true;
                grass[(room.Right - 1) + (room.Bottom - 1) * Width] = true;
            }
        }

        for (var i = Width + 1; i < Length - Width - 1; i++)
        {
            if (Map[i] != Terrain.Empty || !grass[i]) continue;

            var count = 1 + Neighbours8.Count(neighbour => grass[i + neighbour]);
            Map[i] = (Random.Float() < count / 12f) ? Terrain.HighGrass : Terrain.Grass;
        }
    }

    // water() and grass() in the Java; renamed because Level already has a static field named Water.
    protected abstract bool[] WaterMap();
    protected abstract bool[] GrassMap();

    protected void PlaceTraps()
    {
        var nTraps = NTraps();
        var trapChances = TrapChances();

        for (var i = 0; i < nTraps; i++)
        {
            var trapPos = Random.Int(Length);

            if (Map[trapPos] == Terrain.Empty)
            {
                Map[trapPos] = Random.Chances(trapChances) switch
                {
                    0 => Terrain.SecretToxicTrap,
                    1 => Terrain.SecretFireTrap,
                    2 => Terrain.SecretParalyticTrap,
                    3 => Terrain.SecretPoisonTrap,
                    4 => Terrain.SecretAlarmTrap,
                    5 => Terrain.SecretLightningTrap,
                    6 => Terrain.SecretGrippingTrap,
                    7 => Terrain.SecretSummoningTrap,
                    _ => Map[trapPos]
                };
            }
        }
    }

    protected virtual int NTraps()
    {
        return Dungeon.Depth <= 1 ? 0 : Random.Int(1, Rooms.Count + Dungeon.Depth);
    }

    protected virtual float[] TrapChances()
    {
        float[] chances = { 1, 1, 1, 1, 1, 1, 1, 1 };
        return chances;
    }

    protected const int MinRoomSize = 7;
    protected const int MaxRoomSize = 9;

    protected void Split(Rect rect)
    {
        var w = rect.Width();
        var h = rect.Height();

        if (w > MaxRoomSize && h < MinRoomSize)
        {
            var vw = Random.Int(rect.Left + 3, rect.Right - 3);
            Split(new Rect(rect.Left, rect.Top, vw, rect.Bottom));
            Split(new Rect(vw, rect.Top, rect.Right, rect.Bottom));
        }
        else if (h > MaxRoomSize && w < MinRoomSize)
        {
            var vh = Random.Int(rect.Top + 3, rect.Bottom - 3);
            Split(new Rect(rect.Left, rect.Top, rect.Right, vh));
            Split(new Rect(rect.Left, vh, rect.Right, rect.Bottom));
        }
        // The Java calls Math.random() here; Random.Float() is used so that a seed controls the whole generator.
        else if ((Random.Float() <= (MinRoomSize * MinRoomSize / (float)rect.Square()) && w <= MaxRoomSize &&
                  h <= MaxRoomSize) || w < MinRoomSize || h < MinRoomSize)
        {
            Rooms.Add((Room)new Room().Set(rect));
        }
        else
        {
            if (Random.Float() < (float)(w - 2) / (w + h - 4))
            {
                var vw = Random.Int(rect.Left + 3, rect.Right - 3);
                Split(new Rect(rect.Left, rect.Top, vw, rect.Bottom));
                Split(new Rect(vw, rect.Top, rect.Right, rect.Bottom));
            }
            else
            {
                var vh = Random.Int(rect.Top + 3, rect.Bottom - 3);
                Split(new Rect(rect.Left, rect.Top, rect.Right, vh));
                Split(new Rect(rect.Left, vh, rect.Right, rect.Bottom));
            }
        }
    }

    protected void Paint()
    {
        foreach (var room in Rooms)
        {
            if (room.Type != RoomType.Null)
            {
                PlaceDoors(room);
                RoomPainters.Paint(room.Type, this, room);
            }
            else
            {
                if (Feeling == LevelFeeling.Chasm && Random.Int(2) == 0)
                {
                    Painter.Fill(this, room, Terrain.Wall);
                }
            }
        }

        foreach (var room in Rooms)
        {
            PaintDoors(room);
        }
    }

    private void PlaceDoors(Room r)
    {
        foreach (var neighbour in r.Connected.Keys.ToList())
        {
            var door = r.Connected[neighbour];

            if (door != null) continue;

            var intersect = r.Intersect(neighbour);
            if (intersect.Width() == 0)
            {
                door = new Room.Door(
                    intersect.Left,
                    Random.Int(intersect.Top + 1, intersect.Bottom));
            }
            else
            {
                door = new Room.Door(
                    Random.Int(intersect.Left + 1, intersect.Right),
                    intersect.Top);
            }

            r.Connected[neighbour] = door;
            neighbour.Connected[r] = door;
        }
    }

    protected void PaintDoors(Room room)
    {
        foreach (var neighbour in room.Connected.Keys)
        {
            if (JoinRooms(room, neighbour))
            {
                continue;
            }

            var d = room.Connected[neighbour];
            var door = d.X + d.Y * Width;

            switch (d.Type)
            {
                case DoorType.Empty:
                    Map[door] = Terrain.Empty;
                    break;
                case DoorType.Tunnel:
                    Map[door] = TunnelTile();
                    break;
                case DoorType.Regular:
                    if (Dungeon.Depth <= 1)
                    {
                        Map[door] = Terrain.Door;
                    }
                    else
                    {
                        var secret = (Dungeon.Depth < 6 ? Random.Int(12 - Dungeon.Depth) : Random.Int(6)) == 0;
                        Map[door] = secret ? Terrain.SecretDoor : Terrain.Door;
                        if (secret)
                        {
                            SecretDoors++;
                        }
                    }

                    break;
                case DoorType.Unlocked:
                    Map[door] = Terrain.Door;
                    break;
                case DoorType.Hidden:
                    Map[door] = Terrain.SecretDoor;
                    SecretDoors++;
                    break;
                case DoorType.Barricade:
                    Map[door] = Random.Int(3) == 0 ? Terrain.Bookshelf : Terrain.Barricade;
                    break;
                case DoorType.Locked:
                    Map[door] = Terrain.LockedDoor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected bool JoinRooms(Room room, Room neighbour)
    {
        if (room.Type != RoomType.Standard || neighbour.Type != RoomType.Standard)
        {
            return false;
        }

        var w = room.Intersect(neighbour);
        if (w.Left == w.Right)
        {
            if (w.Bottom - w.Top < 3)
            {
                return false;
            }

            if (w.Height() == Math.Max(room.Height(), neighbour.Height()))
            {
                return false;
            }

            if (room.Width() + neighbour.Width() > MaxRoomSize)
            {
                return false;
            }

            w.Top += 1;
            w.Bottom -= 0;

            w.Right++;

            Painter.Fill(this, w.Left, w.Top, 1, w.Height(), Terrain.Empty);
        }
        else
        {
            if (w.Right - w.Left < 3)
            {
                return false;
            }

            if (w.Width() == Math.Max(room.Width(), neighbour.Width()))
            {
                return false;
            }

            if (room.Height() + neighbour.Height() > MaxRoomSize)
            {
                return false;
            }

            w.Left += 1;
            w.Right -= 0;

            w.Bottom++;

            Painter.Fill(this, w.Left, w.Top, w.Width(), 1, Terrain.Empty);
        }

        return true;
    }

    public override int NMobs()
    {
        return 2 + Dungeon.Depth % 5 + Random.Int(3);
    }

    protected override void CreateMobs()
    {
        var nMobs = NMobs();
        for (var i = 0; i < nMobs; i++)
        {
            var mob = Bestiary.Mob(Dungeon.Depth);
            if (mob == null)
            {
                continue; // Bestiary returns null at depths whose mobs are not ported yet
            }

            do
            {
                mob.Pos = RandomRespawnCell();
            } while (mob.Pos == -1);

            Mobs.Add(mob);
            Actor.OccupyCell(mob);
        }
    }

    public override int RandomRespawnCell()
    {
        var count = 0;

        while (true)
        {
            if (++count > 10)
            {
                return -1;
            }

            var room = RandomRoom(RoomType.Standard, 10);
            if (room == null)
            {
                continue;
            }

            var cell = room.RandomCell();
            if (!Dungeon.Visible[cell] && Actor.FindChar(cell) == null && Level.Passable[cell])
            {
                return cell;
            }
        }
    }

    public override int RandomDestination()
    {
        while (true)
        {
            var room = Random.Element(Rooms);
            if (room == null)
            {
                continue;
            }

            var cell = room.RandomCell();
            if (Level.Passable[cell])
            {
                return cell;
            }
        }
    }

    protected override void CreateItems()
    {
        var nItems = 3;
        while (Random.Float() < 0.4f)
        {
            nItems++;
        }

        for (var i = 0; i < nItems; i++)
        {
            HeapType type = Random.Int(20) switch
            {
                0 => HeapType.Skeleton,
                1 or 2 or 3 or 4 => HeapType.Chest,
                5 => Dungeon.Depth > 1 ? HeapType.Mimic : HeapType.Chest,
                _ => HeapType.Heap
            };

            var item = Generator.Random();
            if (item != null)
            {
                Drop(item, RandomDropCell()).Type = type;
            }
        }

        foreach (var item in ItemsToSpawn)
        {
            var cell = RandomDropCell();
            // TODO: ScrollOfUpgrade re-rolls the cell while its FireTrap or SecretFireTrap
            Drop(item, cell).Type = HeapType.Heap;
        }
        
        // TODO: Bones.get() drops previous hero item as Skeleton heap
    }

    protected Room RandomRoom(RoomType type, int tries)
    {
        for (var i = 0; i < tries; i++)
        {
            var room = Random.Element(Rooms);
            if (room.Type == type)
            {
                return room;
            }
        }

        return null;
    }

    // room(pos) in the Java. Named RoomAt so that Room.Specials still resolves to the class inside this file.
    public Room RoomAt(int pos) => Rooms.FirstOrDefault(room => room.Type != RoomType.Null && room.Inside(pos));

    protected int RandomDropCell()
    {
        while (true)
        {
            var room = RandomRoom(RoomType.Standard, 1);

            if (room == null) continue;

            var pos = room.RandomCell();
            if (Passable[pos])
            {
                return pos;
            }
        }
    }

    public override int PitCell()
    {
        foreach (var room in Rooms.Where(room => room.Type == RoomType.Pit))
        {
            return room.RandomCell();
        }

        return base.PitCell();
    }

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Rooms = [.. bundle.GetCollection("rooms").Cast<Room>()];
        foreach (var room in Rooms.Where(room => room.Type == RoomType.WeakFloor))
        {
            WeakFloorCreated = true;
            break;
        }
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put("rooms", Rooms);
    }
}