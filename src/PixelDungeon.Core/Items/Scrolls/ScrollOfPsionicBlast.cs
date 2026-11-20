using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfPsionicBlast : Scroll
{
    public ScrollOfPsionicBlast()
    {
        NameValue = "Scroll of Psionic Blast";
    }

    protected override void DoRead()
    {
        GameScene.Flash(0xFFFFFF);

        Sample.Play(Assets.SndBlast);
        Invisibility.Dispel();

        foreach (var mob in Dungeon.Level.Mobs.ToArray())
        {
            if (!Levels.Level.FieldOfView[mob.Pos]) continue;

            Buff.Prolong<Blindness>(mob, Utils.Random.Int(3, 6));
            mob.Damage(Utils.Random.IntRange(1, mob.HT * 2 / 3), this);
        }

        Buff.Prolong<Blindness>(CurUser, Utils.Random.Int(3, 6));
        Dungeon.Observe();

        SetKnown();

        ReadAnimation();
    }

    public override string Desc() =>
        "This scroll contains destructive energy, that can be psionically channeled to inflict a " +
        "massive damage to all creatures within a field of view. An accompanying flash of light will " +
        "temporarily blind everybody in the area of effect including the reader of the scroll.";

    public override int Price() => IsKnown() ? 80 * QuantityValue : base.Price();
}