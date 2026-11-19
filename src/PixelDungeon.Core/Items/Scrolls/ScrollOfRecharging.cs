using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfRecharging : Scroll
{
    public ScrollOfRecharging()
    {
        NameValue = "Scroll of Recharging";
    }

    protected override void DoRead()
    {
        // TODO: count = curUser.belongings.charge(true), number of wands recharged
        var count = 0;
        Charge(CurUser);
        
        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();

        if (count > 0)
        {
            GLog.I("a surge of energy courses through your pack, recharging your wand" + (count > 1 ? "s" : ""));
            CurUser.Sprite.Burst(0xFFFFFFAA, 1);
        }
        else
        {
            GLog.I("a surge of energy courses through your pack, but nothing happens");
        }
        SetKnown();
        
        ReadAnimation();
    }

    public override string Desc() =>
        "The raw magical power bound up in this parchment will, when released, " +
        "recharge all of the reader's wands to full power.";

    public static void Charge(Hero hero) => hero.Sprite.Burst(0xFFFFFFAA, 15);

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}