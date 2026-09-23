using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core;

public static class Dungeon
{
    public static int PotionOfStrength;
    public static int ScrollsOfUpgrade;
    public static int ScrollsOfEnchantment;
    public static bool DewVial; // true if dew vial can be spawned

    public static int Challenges;

    public static Hero Hero;
    public static Level Level;

    public static int Depth;
    public static int Gold;

    public static string ResultDescription; // reason for death

    public static HashSet<int> Chapters = [];

    public static bool[] Visible = new bool[Level.Length];

    public static bool NightMode;

    public static Dictionary<int, List<Item>> DroppedItems = new();

    public static void Reset()
    {
        Actor.Clear();
        Level.ResetStatics();
        Statistics.Reset();

        Hero = null;
        Level = null;
        Depth = 0;
        Gold = 0;
        Challenges = 0;
        ResultDescription = null;
        Chapters = new HashSet<int>();
        NightMode = false;
        Array.Fill(Visible, false);
        DroppedItems = new Dictionary<int, List<Item>>();
        PotionOfStrength = 0;
        ScrollsOfUpgrade = 0;
        ScrollsOfEnchantment = 0;
        DewVial = true;

        Interlevel.Mode = InterlevelMode.None;
        Interlevel.FallIntoPit = false;
    }

    public static bool IsChallenged(int mask) => (Challenges & mask) != 0;

    public static bool ShopOnLevel() => Depth is 6 or 11 or 16;

    public static bool BossLevel() => BossLevel(Depth);

    public static bool BossLevel(int depth) => depth is 5 or 10 or 15 or 20 or 25;

    public static void DropToChasm(Item item)
    {
        var depth = Depth + 1;
        var dropped = DroppedItems.GetValueOrDefault(depth);
        if (dropped == null)
        {
            DroppedItems[depth] = dropped = new List<Item>();
        }

        dropped.Add(item);
    }

    public static void Fail(string desc)
    {
        ResultDescription = desc;
    }

    public static void Observe()
    {
        if (Level == null)
        {
            return;
        }

        Level.UpdateFieldOfView(Hero);
        Array.Copy(Level.FieldOfView,
            0,
            Visible,
            0,
            Visible.Length);

        BArray.Or(Level.Visited, Visible, Level.Visited);

        GameScene.AfterObserve();
    }

    private static readonly bool[] _passable = new bool[Level.Length];


    public static int FindPath(Char ch, int from, int to, bool[] pass, bool[] visible)
    {
        if (Level.Adjacent(from, to))
        {
            return Actor.FindChar(to) == null && (pass[to] || Level.Avoid[to]) ? to : -1;
        }

        if (ch.Flying)
        {
            BArray.Or(pass, Level.Avoid, _passable);
        }
        else
        {
            Array.Copy(pass,
                0,
                _passable,
                0,
                Level.Length);
        }

        foreach (var actor in Actor.All())
        {
            if (actor is Char other)
            {
                var pos = other.Pos;
                if (visible[pos])
                {
                    _passable[pos] = false;
                }
            }
        }

        return PathFinder.GetStep(from, to, _passable);
    }

    public static int Flee(Char ch, int cur, int from, bool[] pass, bool[] visible)
    {
        if (ch.Flying)
        {
            BArray.Or(pass, Level.Avoid, _passable);
        }
        else
        {
            Array.Copy(pass,
                0,
                _passable,
                0,
                Level.Length);
        }

        foreach (var actor in Actor.All())
        {
            if (actor is Char other)
            {
                var pos = other.Pos;
                if (visible[pos])
                {
                    _passable[pos] = false;
                }
            }
        }

        _passable[cur] = true;

        return PathFinder.GetStepBack(cur, from, _passable);
    }
}