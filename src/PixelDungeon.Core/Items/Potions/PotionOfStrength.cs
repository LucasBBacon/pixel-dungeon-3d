using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfStrength : Potion
{
    public PotionOfStrength()
    {
        NameValue = "Potion of Strength";
    }

    protected override void Apply(Hero hero)
    {
        SetKnown();

        hero.STR++;
        hero.Sprite.ShowStatus(StatusColor.Positive, "+1 str");
        GLog.P("Newfound strength surges through your body.");
        
        // TODO: Badges.validateStrengthAttained()
    }

    public override string Desc() =>
        "This powerful liquid will course through your muscles, " +
        "permanently increasing your strength by one point.";

    public override int Price() => IsKnown() ? 100 * QuantityValue : base.Price();
}