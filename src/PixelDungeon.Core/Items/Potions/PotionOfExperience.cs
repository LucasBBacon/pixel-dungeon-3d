using PixelDungeon.Core.Actors.Hero;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfExperience : Potion
{
    public PotionOfExperience()
    {
        NameValue = "Potion of Experience";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();
        hero.EarnExp(hero.MaxExp() - hero.Exp);
    }

    public override string Desc() =>
        "The storied experiences of multitudes of battles reduced to liquid form, " +
        "this draught will instantly raise your experience level.";

    public override int Price() => IsKnown() ? 80 * QuantityValue : base.Price();
}