using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels;

// Room.Door.Type is the java, top-level here because Room has a field named Type
public enum RoomType
{
    Null,
    Standard,
    Entrance,
    Exit,
    BossExit,
    Tunnel,
    Passage,
    Shop,
    Blacksmith,
    Treasury,
    Armory,
    Library,
    Laboratory,
    Vault,
    Traps,
    Storage,
    MagicWell,
    Garden,
    Crypt,
    Statue,
    Pool,
    RatKing,
    WeakFloor,
    Pit,
    Altar
}

public enum DoorType
{
    Empty,
    Tunnel,
    Regular,
    Unlocked,
    Hidden,
    Barricade,
    Locked
}

public static class RoomPainters
{
    public static void Paint(RoomType type, Level level, Room room)
    {
        switch (type)
        {
            case RoomType.Null:
                break;
            case RoomType.Standard:
                StandardPainter.Paint(level, room);
                break;
            case RoomType.Entrance:
                EntrancePainter.Paint(level, room);
                break;
            case RoomType.Exit:
                ExitPainter.Paint(level, room);
                break;
            case RoomType.Tunnel:
                TunnelPainter.Paint(level, room);
                break;
            default:
                StandardPainter.Paint(level, room);
                break;
        }
    }
}

public class Room : Rect, IGraphNode, IBundlable
{
    public HashSet<Room> Neighbours = [];

    public int Distance { get; set; }
    public int Price { get; set; } = 1;
    public Dictionary<Room, Door> Connected = new();

    private static readonly RoomType[] DefaultSpecials =
    [
        RoomType.Armory,
        RoomType.WeakFloor,
        RoomType.MagicWell,
        RoomType.Crypt,
        RoomType.Pool,
        RoomType.Garden,
        RoomType.Library,
        RoomType.Treasury,
        RoomType.Traps,
        RoomType.Storage,
        RoomType.Statue,
        RoomType.Laboratory,
        RoomType.Vault,
        RoomType.Altar
    ];

    public static readonly List<RoomType> Specials = [.. DefaultSpecials];

    public RoomType Type = RoomType.Null;

    public int RandomCell() => RandomCell(0);

    public int RandomCell(int m)
    {
        var x = Random.Int(Left + 1 + m, Right - m);
        var y = Random.Int(Top + 1 + m, Bottom - m);
        return x + y * Level.Width;
    }

    public void AddNeighbour(Room other)
    {
        var i = Intersect(other);

        if ((i.Width() != 0 || i.Height() < 3) &&
            (i.Height() != 0 || i.Width() < 3))
            return;

        Neighbours.Add(other);
        other.Neighbours.Add(this);
    }

    public void Connect(Room room)
    {
        if (Connected.TryAdd(room, null))
        {
            room.Connected[this] = null;
        }
    }

    public Door Entrance() => Connected.Values.First();

    public bool Inside(int p)
    {
        var x = p % Level.Width;
        var y = p / Level.Width;
        return x > Left && y > Top && x < Right && y < Bottom;
    }

    public Point Center() =>
        new(
            (Left + Right) / 2 + (((Right - Left) & 1) == 1 ? Random.Int(2) : 0),
            (Top + Bottom) / 2 + (((Bottom - Top) & 1) == 1 ? Random.Int(2) : 0)
        );

    public IEnumerable<IGraphNode> Edges() => Neighbours;

    private const string TagLeft = "left";
    private const string TagTop = "top";
    private const string TagRight = "right";
    private const string TagBottom = "bottom";
    private const string TagType = "type";

    public void RestoreFromBundle(Bundle bundle)
    {
        Left = bundle.GetInt(TagLeft);
        Top = bundle.GetInt(TagTop);
        Right = bundle.GetInt(TagRight);
        Bottom = bundle.GetInt(TagBottom);
        Type = bundle.GetEnum<RoomType>(TagType);
    }

    public void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagLeft, Left);
        bundle.Put(TagTop, Top);
        bundle.Put(TagRight, Right);
        bundle.Put(TagBottom, Bottom);
        bundle.Put(TagType, Type);
    }

    public static void ShuffleTypes()
    {
        var size = Specials.Count;
        for (var i = 0; i < size - 1; i++)
        {
            var j = Random.Int(i, size);
            if (j != i)
            {
                (Specials[i], Specials[j]) = (Specials[j], Specials[i]);
            }
        }
    }

    public static void UseType(RoomType type)
    {
        if (Specials.Remove(type))
        {
            Specials.Add(type);
        }
    }

    // not in the java, restores declared order
    public static void ResetSpecials()
    {
        Specials.Clear();
        Specials.AddRange(DefaultSpecials);
    }

    private const string TagRooms = "rooms";

    public static void RestoreRoomsFromBundle(Bundle bundle)
    {
        if (bundle.Contains(TagRooms))
        {
            Specials.Clear();
            foreach (var type in bundle.GetStringArray(TagRooms))
            {
                Specials.Add(Enum.Parse<RoomType>(type));
            }
        }
        else
        {
            ShuffleTypes();
        }
    }

    public static void StoreRoomsInBundle(Bundle bundle)
    {
        var array = new string[Specials.Count];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = Specials[i].ToString();
        }

        bundle.Put(TagRooms, array);
    }

    public class Door : Point
    {
        public DoorType Type = DoorType.Empty;

        public Door(int x, int y) : base(x, y)
        {
        }

        public void Set(DoorType type)
        {
            if (type.CompareTo(Type) > 0)
            {
                Type = type;
            }
        }
    }
}