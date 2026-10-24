using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Weapon;

public abstract class Weapon : KindOfWeapon
{
    private const int HitsToKnow = 20;

    private const string TxtIdentify = "You are now familiar enough with your {0} to identify it. It is {1}.";

    private const string TxtIncompatible =
        "Interaction of different types of magic has negated the enchantment on this weapon!";

    private const string TxtToString = "{0} :{1}";
    private const string TxtBroken = "broken {0} :{1}";

    public int Str = 10;
    public float Acu = 1;
    public float Dly = 1f;

    public enum Imbue
    {
        None,
        Speed,
        Accuracy
    }

    public Imbue ImbueValue = Imbue.None;

    private int _hitsToKnow = HitsToKnow;

    // TODO: Enchantment class (Fire, Poison, Death, Paralysis, Leech, Slow, Shock, Instability, Horror, Luck, Tempering) protected enchantment field

    private static readonly float[] EnchantmentChances =
    [
        10,
        10,
        1,
        2,
        1,
        2,
        6,
        3,
        2,
        2,
        3
    ];


    public override void Proc(Char attacker, Char defender, int damage)
    {
        // TODO: enchantment.proc(this, attacker, defender, damage) when weapon enchanted

        if (!LevelKnown)
        {
            if (--_hitsToKnow <= 0)
            {
                LevelKnown = true;
                GLog.I(TxtIdentify, Name(), ToString());
                // TODO: Badges.validateItemLevelAcquired(this)
            }
        }

        Use();
    }

    private const string TagUnfamiliarity = "unfamiliarity";
    private const string TagEnchantment = "enchantment";
    private const string TagImbue = "imbue";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        if ((_hitsToKnow = bundle.GetInt(TagUnfamiliarity)) == 0)
        {
            _hitsToKnow = HitsToKnow;
        }

        // TODO: enchantment = (Enchantment)bundle.get(ENCHANTMENT)
        ImbueValue = bundle.GetEnum<Imbue>(TagImbue);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagUnfamiliarity, _hitsToKnow);
        // TODO: bundle.put(ENCHANTMENT, enchantment)
        bundle.Put(TagImbue, ImbueValue);
    }

    public override float AccuracyFactor(Hero hero)
    {
        var encumbrance = Str - hero.Str();

        if (this is MissileWeapon)
        {
            switch (hero.HeroClass)
            {
                case HeroClass.Warrior:
                    encumbrance += 3;
                    break;
                case HeroClass.Huntress:
                    encumbrance -= 2;
                    break;
                default:
                    break;
            }
        }

        return (encumbrance > 0 ? (float)(Acu / Math.Pow(1.5, encumbrance)) : Acu) *
               (ImbueValue == Imbue.Accuracy ? 1.5f : 1.0f);
    }

    public override float SpeedFactor(Hero hero)
    {
        var encumbrance = Str - hero.Str();
        if (this is MissileWeapon && hero.HeroClass == HeroClass.Huntress)
        {
            encumbrance -= 2;
        }

        return (encumbrance > 0 ? (float)(Dly * Math.Pow(1.2, encumbrance)) : Dly) *
               (ImbueValue == Imbue.Speed ? 0.6f : 1.0f);
    }

    public override int DamageRoll(Hero hero)
    {
        var damage = base.DamageRoll(hero);

        if ((hero.RangedWeapon != null) == (hero.HeroClass == HeroClass.Huntress))
        {
            var exStr = hero.Str() - Str;
            if (exStr > 0)
            {
                damage += Utils.Random.IntRange(0, exStr);
            }
        }

        return damage;
    }

    public virtual Item Upgrade(bool enchantment)
    {
        // TODO: an enchanted weapon upgraded with enchant false loses its enchantment prb
        if (enchantment)
        {
            Enchant();
        }

        return base.Upgrade();
    }

    public override int MaxDurability(int lvl) => 5 * (lvl < 16 ? 16 - lvl : 1);

    public override string ToString() =>
        LevelKnown
            ? TextUtils.Format(IsBroken()
                    ? TxtBroken
                    : TxtToString,
                base.ToString(),
                Str)
            : base.ToString();

    // TODO: name() returns enchantment.name(super.name()) for enchanted weapon

    public override Item Random()
    {
        if (!(Utils.Random.Float() < 0.4))
            return this;

        var n = 1;
        if (Utils.Random.Int(3) == 0)
        {
            n++;
            if (Utils.Random.Int(3) == 0)
            {
                n++;
            }
        }

        if (Utils.Random.Int(2) == 0)
        {
            Upgrade(n);
        }
        else
        {
            Degrade(n);
            Cursed = true;
        }

        return this;
    }

    // TODO: enchant(Enchantment)

    public Weapon Enchant()
    {
        // TODO: Enchantment.random() builds 1 of the 11 enchantments
        Utils.Random.Chances(EnchantmentChances);
        return this;
    }

    // TODO: glowing() returns the enchantments glow
}