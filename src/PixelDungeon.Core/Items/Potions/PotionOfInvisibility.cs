using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfInvisibility : Potion
{
    public PotionOfInvisibility()
    {
        NameValue = "Potion of Invisibility";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);
        GLog.I("You see your hands turn invisible!");
        Sample.Play(Assets.SndMeld);
    }

    public override string Desc() =>
        "Drinking this potion will render you temporarily invisible. While invisible, " +
        "enemies will be unable to see you. Attacking an enemy, as well as using a wand or a scroll " +
        "before enemy's eyes, will dispel the effect.";

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}