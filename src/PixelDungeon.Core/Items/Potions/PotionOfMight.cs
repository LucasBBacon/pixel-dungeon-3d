using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfMight : Potion
{
    public PotionOfMight()
    {
        NameValue = "Potion of Might";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();

        hero.STR++;
        hero.HT += 5;
        hero.HP += 5;
        hero.Sprite.ShowStatus(StatusColor.Positive, "+1 str, +5 ht");
        GLog.P("Newfound strength surges through your body.");
        
        // TODO: Badges.validateStrengthAttained()
    }

    public override string Desc() =>
        "This powerful liquid will course through your muscles, permanently " +
        "increasing your strength by one point and health by five points.";

    public override int Price() => IsKnown() ? 200 * QuantityValue : base.Price();
}