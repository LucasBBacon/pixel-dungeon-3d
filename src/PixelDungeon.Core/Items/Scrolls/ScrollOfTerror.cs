using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfTerror : Scroll
{
    public ScrollOfTerror()
    {
        NameValue = "Scroll of Terror";
    }

    protected override void DoRead()
    {
        CurUser.Sprite.Burst(0xFFFF0000, 5);
        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();

        var count = 0;
        Mob affected = null;
        foreach (var mob in Dungeon.Level.Mobs.ToArray())
        {
            if (Levels.Level.FieldOfView[mob.Pos])
            {
                Buff.Affect<Terror>(mob, Terror.Duration).Object = CurUser.Id();

                count++;
                affected = mob;
            }
        }

        switch (count)
        {
            case 0:
                GLog.I("The scroll emits a brilliant flash of red light");
                break;
            case 1:
                GLog.I("The scroll emits a brilliant flash of red light and the " + affected.Name + " flees!");
                break;
            default:
                GLog.I("The scroll emits a brilliant flash of red light and the monsters flee!");
                break;
        }

        SetKnown();

        ReadAnimation();
    }

    public override string Desc() =>
        "A flash of red light will overwhelm all creatures in your field of view with terror, " +
        "and they will turn and flee. Attacking a fleeing enemy will dispel the effect.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}