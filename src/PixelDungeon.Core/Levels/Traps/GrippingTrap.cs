using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Levels.Traps;

public static class GrippingTrap
{
    public static void Trigger(int pos, Char ch)
    {
        if (ch != null)
        {
            var damage = Math.Max(0, (Dungeon.Depth + 3) - Random.IntRange(0, ch.Dr() / 2));
            Buff.Affect<Bleeding>(ch).Set(damage);
            Buff.Prolong<Cripple>(ch, Cripple.Duration);
            GameScene.Effect(EffectKind.Wound, ch.Pos);
        }
        else
        {
            GameScene.Effect(EffectKind.Wound, pos);
        }
    }
}