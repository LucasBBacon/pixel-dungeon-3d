using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfLevitation : Potion
{
    public PotionOfLevitation()
    {
        NameValue = "Potion of Levitation";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();
        Buff.Affect<Levitation>(hero, Levitation.Duration);
        GLog.I("You float into the air!");
    }

    public override string Desc() =>
        "Drinking this curious liquid will cause you to hover in the air, " +
        "able to drift effortlessly over traps. Flames and gases " +
        "fill the air, however, and cannot be bypassed while airborne.";

    public override int Price() => IsKnown() ? 35 * QuantityValue : base.Price();
}