using System.Text;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class MeleeWeapon : Weapon
{
    private int _tier;

    public MeleeWeapon(int tier, float acu, float dly) : base()
    {
        _tier = tier;

        Acu = acu;
        Dly = dly;

        Str = TypicalStr();
    }

    protected virtual int Min0() => _tier;

    // float division, as in the Java: the War Hammer's 30 / 1.2f is 24.999998f, which truncates to 24
    protected virtual int Max0() => (int)((_tier * _tier - _tier + 10) / Acu * Dly);

    public override int Min() => IsBroken() ? Min0() : Min0() + Level();

    public override int Max() => IsBroken() ? Max0() : Max0() + Level() * _tier;

    public sealed override Item Upgrade() => Upgrade(false);

    public override Item Upgrade(bool enchantment)
    {
        Str--;
        return base.Upgrade(enchantment);
    }

    public Item SafeUpgrade()
    {
        // TODO: passes enchantment != null, enchanted weapon keeps its enchantment through the upgrade
        return Upgrade(false);
    }

    public override Item Degrade()
    {
        Str++;
        return base.Degrade();
    }

    public int TypicalStr() => 8 + _tier * 2;

    public override string Info()
    {
        const string p = "\n\n";

        var info = new StringBuilder(Desc());

        var lvl = VisiblyUpgraded();
        var quality = lvl != 0
            ? (lvl > 0
                ? (IsBroken()
                    ? "broken"
                    : "upgraded")
                : "degraded")
            : "";
        info.Append(p);
        info.Append("This " + NameValue + " is " + TextUtils.Indefinite(quality));
        info.Append(" tier-" + _tier + " melee weapon. ");

        if (LevelKnown)
        {
            var min = Min();
            var max = Max();
            info.Append("Its average damage is " + (min + (max - min) / 2) + " points per hit. ");
        }
        else
        {
            var min = Min0();
            var max = Max0();
            info.Append(
                "Its typical average damage is " + (min + (max - min) / 2) + " points per hit " +
                "and usually it requires " + TypicalStr() + " points of strength. ");
            if (TypicalStr() > Dungeon.Hero.Str())
            {
                info.Append("Probably this weapon is too heavy for you. ");
            }
        }

        if (Dly != 1f)
        {
            info.Append("This is a rather " + (Dly < 1f ? "fast" : "slow"));
            if (Acu != 1f)
            {
                if ((Acu > 1f) == (Dly < 1f))
                {
                    info.Append(" and ");
                }
                else
                {
                    info.Append(" but ");
                }

                info.Append(Acu > 1f ? "accurate" : "inaccurate");
            }

            info.Append(" weapon. ");
        }
        else if (Acu != 1f)
        {
            info.Append("This is a rather " + (Acu > 1f ? "accurate" : "inaccurate") + " weapon. ");
        }

        switch (ImbueValue)
        {
            case Imbue.Speed:
                info.Append("It was balanced to make it faster. ");
                break;
            case Imbue.Accuracy:
                info.Append("It was balanced to make it more accurate. ");
                break;
            case Imbue.None:
            default:
                break;
        }

        // TODO: "It is enchanted." when the weapon carries an enchantment

        if (LevelKnown && Dungeon.Hero.Belongings.Backpack.Items.Contains(this))
        {
            if (Str > Dungeon.Hero.Str())
            {
                info.Append(p);
                info.Append(
                    "Because of your inadequate strength the accuracy and speed " +
                    "of your attack with this " + NameValue + " is decreased.");
            }

            if (Str < Dungeon.Hero.Str())
            {
                info.Append(p);
                info.Append(
                    "Because of your excess strength the damage " +
                    "of your attack with this " + NameValue + " is increased.");
            }
        }

        if (IsEquipped(Dungeon.Hero))
        {
            info.Append(p);
            info.Append("You hold the " + NameValue + " at the ready" +
                        (Cursed ? ", and because it is cursed, you are powerless to let go." : "."));
        }
        else
        {
            if (CursedKnown && Cursed)
            {
                info.Append(p);
                info.Append("You can feel a malevolent magic lurking within " + NameValue + ".");
            }
        }

        return info.ToString();
    }

    public override int Price()
    {
        var price = 20 * (1 << (_tier - 1));
        // TODO: an enchanted weapon's price is multiplied by 1.5
        return ConsiderState(price);
    }

    public override Item Random()
    {
        base.Random();

        // enchant roll stays in stream, Enchant() draws Enchant.radom() roll
        // TODO: apply roll
        if (Utils.Random.Int(10 + Level()) == 0)
        {
            Enchant();
        }

        return this;
    }
}