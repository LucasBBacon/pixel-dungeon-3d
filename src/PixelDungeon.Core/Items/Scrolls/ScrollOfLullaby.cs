using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfLullaby : Scroll
{
    public ScrollOfLullaby()
    {
        NameValue = "Scroll of Lullaby";
    }

    protected override void DoRead()
    {
        CurUser.Sprite.Burst(0xFFAADDFF, 5);
        Sample.Play(Assets.SndLullaby);
        Invisibility.Dispel();

        var count = 0;
        Mob affected = null;
        foreach (var mob in Dungeon.Level.Mobs.ToArray())
        {
            if (!Levels.Level.FieldOfView[mob.Pos]) continue;
            
            Buff.Affect<Sleep>(mob);
            
            if (mob.GetBuff<Sleep>() == null) continue;
            
            affected = mob;
            count++;
        }

        switch (count)
        {
            case 0:
                GLog.I("The scroll utters a soothing melody.");
                break;
            case 1:
                GLog.I("The scroll utters a soothing melody and the " + affected.Name + " falls asleep!");
                break;
            default:
                GLog.I("The scroll utters a soothing melody and the monsters fall asleep!");
                break;
        }

        SetKnown();

        ReadAnimation();
    }

    public override string Desc() =>
        "A soothing melody will put all creatures in your field of view into a deep sleep, " +
        "giving you a chance to flee or make a surprise attack on them.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}