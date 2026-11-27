using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Levels.Traps;

public static class PoisonTrap
{
    // 0xBB66EE
    public static void Trigger(int pos, Char ch)
    {
        if (ch != null)
        {
            Buff.Affect<Poison>(ch).Set(Poison.DurationFactor(ch) * (4 + Dungeon.Depth / 2));
        }

        GameScene.Splash(pos, 0xFF8844FF, 3);
    }
}