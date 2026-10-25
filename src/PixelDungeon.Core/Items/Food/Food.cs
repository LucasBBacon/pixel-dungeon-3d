using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Food;

public class Food : Item
{
    private const float TimeToEat = 3f;

    public const string AcEat = "EAT";

    public float Energy = Hunger.Hungry;
    public string Message = "That food tasted delicious!";

    public Food()
    {
        Stackable = true;
        NameValue = "ration of food";
        ImageValue = ItemSpriteSheet.Ration;
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        actions.Add(AcEat);
        return actions;
    }

    public override void Execute(Hero hero, string action)
    {
        if (action == AcEat)
        {
            Detach(hero.Belongings.Backpack);

            hero.GetBuff<Hunger>()
                .Satisfy(Energy); // unchecked, as the Java's cast is, Live() gives every playing hero Hunger
            GLog.I(Message);

            switch (hero.HeroClass)
            {
                case HeroClass.Warrior:
                    if (hero.HP < hero.HT)
                    {
                        hero.HP = Math.Min(hero.HP + 5, hero.HT);
                        hero.Sprite.Burst(0xFF33FF33, 1); // Speck.HEALING
                    }

                    break;
                case HeroClass.Mage:
                    // TODO: hero.belongings.charge(false) recharges every wand the Mage holds
                    // TODO: ScrollOfRecharging.charge(hero) bursts energy particles over the hero
                    break;
                case HeroClass.Rogue:
                case HeroClass.Huntress:
                default:
                    break;
            }

            hero.Sprite.Operate(hero.Pos);
            hero.Busy();
            hero.Sprite.Burst(0xFFFFCC66,
                1); // SpellSprite.show(hero, SpellSprite.FOOD) floats a food icon over the hero
            Sample.Play(Assets.SndEat);

            hero.Spend(TimeToEat);

            Statistics.FoodEaten++;
            // TODO: Badges.validateFoodEaten()
        }
        else
        {
            base.Execute(hero, action);
        }
    }

    public override string Info() =>
        "Nothing fancy here: dried meat, " +
        "some biscuits - things like that.";

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public override int Price() => 10 * QuantityValue;
}