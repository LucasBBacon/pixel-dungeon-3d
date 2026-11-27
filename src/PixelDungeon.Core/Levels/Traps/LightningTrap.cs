using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Traps;

public static class LightningTrap
{
    private const string Name = "lightning trap";

    // 0x66CCEE
    public static void Trigger(int pos, Char ch)
    {
        if (ch != null)
        {
            ch.Damage(Math.Max(1, Random.Int(ch.HP / 3, 2 * ch.HP / 3)), Lightning);
            if (ch == Dungeon.Hero)
            {
                GameScene.Shake(2, 0.3f);

                if (!ch.IsAlive())
                {
                    Dungeon.Fail(TextUtils.Format(ResultDescriptions.Trap, Name, Dungeon.Depth));
                    GLog.N("You were killed by a discharge of a lightning trap...");
                }
            }

            GameScene.Lightning([pos - Level.Width, pos + Level.Width], 2, null);
            GameScene.Lightning([pos - 1, pos + 1], 2, null);
        }

        GameScene.Splash(pos, 0xFFFFFFFF, Random.IntRange(3, 4));
    }

    public static readonly Electricity Lightning = new();

    // The damage source that resistances key on.
    public class Electricity
    {
    }
}