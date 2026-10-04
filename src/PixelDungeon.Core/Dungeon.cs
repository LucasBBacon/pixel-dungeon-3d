using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
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
        Room.ResetSpecials();

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
        Chasm.JumpConfirmed = false;

        _levels.Clear();
    }

    public static void Init()
    {
        // TODO: challenges = PixelDungeon.Challenges()
        Challenges = 0;

        Actor.Clear();

        PathFinder.SetMapSize(Level.Width, Level.Height);

        // TODO: Scroll.InitLabels(), Potion.InitColors(), Wand.InitWoods(), Ring.InitGems()

        Statistics.Reset();
        // TODO: Journal.Reset()

        Depth = 0;
        Gold = 0;

        DroppedItems = new Dictionary<int, List<Item>>();

        PotionOfStrength = 0;
        ScrollsOfUpgrade = 0;
        ScrollsOfEnchantment = 0;
        DewVial = true;

        Chapters = new HashSet<int>();

        // TODO: Ghost.Quest.Rest(), Wandmaker.Quest.Reset(), Blacksmith.Quest.Reset(), Imp.Quest.Rest()

        Room.ShuffleTypes();

        // TODO: Quickslot.PrimaryValue & SecondaryValue cleared

        Hero = new Hero();
        Hero.Live();

        // TODO: Badges.Reset()
        // TODO: StartScene.CurClass.InitHero(Hero) sets class, kit, etc
        Hero.UpdateAwareness(); // InitHero ends with UpdateAwareness(), lvl 1 Rogue 0.1 -> 0.15
    }

    public static Level NewLevel()
    {
        Level = null;
        Actor.Clear();

        Depth++;
        if (Depth > Statistics.DeepestFloor)
        {
            Statistics.DeepestFloor = Depth;

            if (Statistics.QualifiedForNoKilling)
            {
                Statistics.CompletedWithNoKilling = true;
            }
            else
            {
                Statistics.CompletedWithNoKilling = false;
            }
        }

        Array.Fill(Visible, false);

        Level level;
        switch (Depth)
        {
            case 1:
            case 2:
            case 3:
            case 4:
                level = new SewerLevel();
                break;
            default:
                // TODO: 5 SewerBossLevel, 6-9 PrisonLevel...
                level = new SewerLevel();
                break;
        }

        level.Create();

        Statistics.QualifiedForNoKilling = !BossLevel();

        return level;
    }

    public static void ResetLevel()
    {
        Actor.Clear();

        Array.Fill(Visible, false);

        Level.Reset();
        SwitchLevel(Level, Level.Entrance);
    }

    public static void SwitchLevel(Level level, int pos)
    {
        NightMode = DateTime.Now.Hour < 7;

        Level = level;
        Actor.Init();

        var respawner = level.Respawner();
        if (respawner != null)
        {
            Actor.Add(level.Respawner()); // java builds a second respawner here and schedules that one
        }

        Hero.Pos = pos != -1 ? pos : level.Exit;

        // TODO: light buff raises viewDistance to Max(Light.Distance, level.ViewDistance)
        Hero.ViewDistance = level.ViewDistance;

        Observe();
    }

    private const string TagLevel = "level";

    // java writes one depth file per level
    // keep each visited depth as bundle in memory
    // TODO: file streams around same calls
    private static readonly Dictionary<int, Bundle> _levels = new();

    public static void SaveLevel()
    {
        var bundle = new Bundle();
        bundle.Put(TagLevel, Level);
        _levels[Depth] = bundle;
    }

    public static Level LoadLevel()
    {
        Level = null;
        Actor.Clear();

        if (!_levels.TryGetValue(Depth, out var bundle))
        {
            throw new KeyNotFoundException($"No level saved for depth {Depth}"); // FileNotFound in java
        }

        return (Level)bundle.Get(TagLevel) ??
               throw new InvalidOperationException($"The level saved for depth {Depth} failed to restore");
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

    public static bool PosNeeded()
    {
        int[] quota = [4, 2, 9, 4, 14, 6, 19, 8, 24, 9];
        return Chance(quota, PotionOfStrength);
    }

    public static bool SouNeeded()
    {
        int[] quota = [5, 3, 10, 6, 15, 9, 20, 12, 25, 13];
        return Chance(quota, ScrollsOfUpgrade);
    }

    public static bool SoeNeeded() => Random.Int(12 * (1 + ScrollsOfEnchantment)) < Depth;

    private static bool Chance(int[] quota, int number)
    {
        for (var i = 0; i < quota.Length; i += 2)
        {
            var qDepth = quota[i];

            if (Depth > qDepth) continue;

            var qNumber = quota[i + 1];
            return Random.Float() < (float)(qNumber - number) / (qDepth - Depth + 1);
        }

        return false;
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