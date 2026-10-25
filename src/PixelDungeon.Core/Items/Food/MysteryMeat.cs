using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Food;

public class MysteryMeat : Food
{
    public MysteryMeat()
    {
        NameValue = "mystery meat";
        ImageValue = ItemSpriteSheet.Meat;
        Energy = Hunger.Starving - Hunger.Hungry;
        Message = "That food tasted... strange.";
    }

    public override void Execute(Hero hero, string action)
    {
        base.Execute(hero, action);

        if (action == AcEat)
        {
            switch (Utils.Random.Int(5))
            {
                case 0:
                    // TODO: GLog.w("Oh it's hot!") and Buff.affect(hero, Burning.class).reignite(hero); the Random.Int(5) roll is kept
                    break;
                case 1:
                    GLog.W("You can't feel your legs!");
                    Buff.Prolong<Roots>(hero, Paralysis.Duration(hero));
                    break;
                case 2:
                    GLog.W("You are not feeling well.");
                    Buff.Affect<Poison>(hero).Set(Poison.DurationFactor(hero) * hero.HT / 5);
                    break;
                case 3:
                    GLog.W("You are stuffed.");
                    Buff.Prolong<Slow>(hero, Slow.Duration(hero));
                    break;
            }
        }
    }

    public override string Info()
    {
        return "Eat at your own risk!";
    }

    public override int Price()
    {
        return 5 * QuantityValue;
    }
}