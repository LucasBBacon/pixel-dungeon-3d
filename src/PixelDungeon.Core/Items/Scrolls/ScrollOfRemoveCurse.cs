using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfRemoveCurse : Scroll
{
    private const string TxtProceed = "Your pack glows with a cleasning light, and a malevolent light disperses.";
    private const string TxtNotProceed = "Your pack glows with a cleansing light, but nothing happens.";

    public ScrollOfRemoveCurse()
    {
        NameValue = "Scroll of Remove Curse";
    }

    protected override void DoRead()
    {
        CurUser.Sprite.Burst(0xFFFFFFFF, 6);
        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();

        var proceed = Uncurse(CurUser, [.. CurUser.Belongings.Backpack.Items]);
        proceed = Uncurse(CurUser,
            CurUser.Belongings.Weapon,
            CurUser.Belongings.Armor,
            CurUser.Belongings.Ring1,
            CurUser.Belongings.Ring2) || proceed;

        Buff.Detach<Weakness>(CurUser);

        if (proceed)
        {
            GLog.P(TxtProceed);
        }
        else
        {
            GLog.I(TxtNotProceed);
        }

        SetKnown();

        ReadAnimation();
    }

    public override string Desc() =>
        "The incantation on this scroll will instantly strip from " +
        "the reader's weapon, armor, rings and carried items any evil " +
        "enchantments that might prevent the wearer from removing them.";

    public static bool Uncurse(Hero hero, params Item[] items)
    {
        var proceed = false;
        foreach (var item in items)
        {
            if (item is not { Cursed: true }) continue;

            item.Cursed = false;
            proceed = true;
        }

        if (proceed)
        {
            hero.Sprite.Burst(0xFF440044, 10);
        }

        return proceed;
    }

    public override int Price() => IsKnown() ? 30 * QuantityValue : base.Price();
}