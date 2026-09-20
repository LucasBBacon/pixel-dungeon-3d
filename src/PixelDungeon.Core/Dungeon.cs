using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core;

public class Dungeon
{
    public static int Challenges;

    public static Level Level;
    public static int Depth;

    public static bool[] Visible = new bool[Level.Length];

    public static bool IsChallenged(int mask)
    {
        return (Challenges & mask) != 0;
    }

    public static bool ShopOnLevel() => Depth is 6 or 11 or 16;

    public static bool BossLevel() => BossLevel(Depth);

    public static bool BossLevel(int depth) => depth is 5 or 10 or 15 or 20 or 25;
}