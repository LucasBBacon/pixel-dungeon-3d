using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items;

public class Dewdrop : Item
{
    private const string TxtValue = "{0:+#;-#;+0}HP";

    public Dewdrop()
    {
        NameValue = "dewdrop";
        ImageValue = ItemSpriteSheet.Dewdrop;

        Stackable = true;
    }

    public override bool DoPickUp(Hero hero)
    {
        var vial = hero.Belongings.GetItem<DewVial>();

        if (hero.HP < hero.HT || vial == null || vial.IsFull())
        {
            var value = 1 + (Dungeon.Depth - 1) / 5;
            if (hero.HeroClass == HeroClass.Huntress)
            {
                value++;
            }

            var effect = Math.Min(hero.HT - hero.HP, value * QuantityValue);
            if (effect > 0)
            {
                hero.HP += effect;
                hero.Sprite.Burst(0xFF33FF33, 1); // Speck.Healing
                hero.Sprite.ShowStatus(StatusColor.Positive, TextUtils.Format(TxtValue, effect));
            }
        }
        else if (vial != null)
        {
            vial.CollectDew(this);
        }

        Sample.Play(Assets.SndDewdrop);
        hero.SpendAndNext(TimeToPickUp);

        return true;
    }

    public override string Info()
    {
        return "A crystal clear dewdrop.";
    }
}