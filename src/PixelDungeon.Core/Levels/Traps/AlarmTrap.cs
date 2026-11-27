using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Traps;

public static class AlarmTrap
{
    // 0xDD3333
    public static void Trigger(int pos, Char ch)
    {
        // beckon doesn't change the mob set, but a copy keeps the walk safe from mobs that react
        foreach (var mob in new List<Mob>(Dungeon.Level.Mobs).Where(mob => mob != ch))
        {
            mob.Beckon(pos);
        }

        if (Dungeon.Visible[pos])
        {
            GLog.W("The trap emits a piercing sound that echoes throughout the dungeon!");
            GameScene.Splash(pos, 0xFFFFFFFF, 3);
        }

        Sample.Play(Assets.SndAlert);
    }
}