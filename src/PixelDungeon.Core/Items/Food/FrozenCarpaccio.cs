using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Food;

public class FrozenCarpaccio : Food
{
    public FrozenCarpaccio()
    {
        NameValue = "frozen carpaccio";
        ImageValue = ItemSpriteSheet.Carpaccio;
        Energy = Hunger.Starving - Hunger.Hungry;
    }

    public override void Execute(Hero hero, string action)
    {
        base.Execute(hero, action);

        if (action == AcEat)
        {
            switch (Utils.Random.Int(5))
            {
                case 0:
                    GLog.I("You see your hands turn invisible!");
                    Buff.Affect<Invisibility>(hero, Invisibility.Duration);
                    break;
                case 1:
                    GLog.I("You feel your skin hardens!");
                    // TODO: Buff.affect(hero, Barkskin.class).level(hero.HT / 4)
                    break;
                case 2:
                    GLog.I("Refreshing!");
                    Buff.Detach<Poison>(hero);
                    Buff.Detach<Cripple>(hero);
                    Buff.Detach<Weakness>(hero);
                    Buff.Detach<Bleeding>(hero);
                    break;
                case 3:
                    GLog.I("You feel better!");
                    if (hero.HP < hero.HT)
                    {
                        hero.HP = Math.Min(hero.HP + hero.HT / 4, hero.HT);
                        hero.Sprite.Burst(0xFF33FF33, 1);
                    }

                    break;
            }
        }
    }

    public override string Info() =>
        "It's a piece of frozen raw meat. The only way to eat it is " +
        "by cutting thin slices of it. And this way it's surprisingly good.";

    public override int Price() => 10 * QuantityValue;

    public static Food Cook(MysteryMeat ingredient)
    {
        var result = new FrozenCarpaccio
        {
            QuantityValue = ingredient.Quantity()
        };
        return result;
    }
}