using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Armor;

public class Armor : EquipableItem
{
    private const int HitsToKnow = 10;

    private const string TxtEquipCursed = "your {0} constricts around you painfully";

    private const string TxtIdentify = "you are now familiar enough with your {0} to identify it. It is {1}.";

    private const string TxtToString = "{0} :{1}";
    private const string TxtBroken = "broken {0} :{1}";

    private const string TxtIncompatible =
        "Interaction of different types of magic has erased the glyph on this armor!";


    public int Tier;
    public int Str;

    private int _hitsToKnow = HitsToKnow;

    // TODO: the public glyph field and the nested Glyph class (Bounce, Affection, AntiEntropy, Multiplicity, Potential, Metabolism, Stench, Viscosity, Displacement, Entanglement, AutoRepair)

    private static readonly float[] GlyphChances =
    [
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1,
        1
    ];

    public Armor(int tier)
    {
        Tier = tier;
        Str = TypicalStr();
    }

    private const string UnfamiliarityTag = "unfamiliarity";
    private const string GlyphTag = "glpyph";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        if ((_hitsToKnow = bundle.GetInt(UnfamiliarityTag)) == 0)
        {
            _hitsToKnow = HitsToKnow;
        }

        // TODO: inscribe((Glyph)bundle.get(Glyph))
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(UnfamiliarityTag, _hitsToKnow);
        // TODO: bundle.put(GLYPH, glyph)
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        actions.Add(IsEquipped(hero) ? AcUnequip : AcEquip);
        return actions;
    }

    public override bool DoEquip(Hero hero)
    {
        Detach(hero.Belongings.Backpack);

        if (hero.Belongings.Armor == null || hero.Belongings.Armor.DoUnequip(hero, true, false))
        {
            hero.Belongings.Armor = this;

            CursedKnown = true;
            if (Cursed)
            {
                EquipCursed(hero);
                GLog.N(TxtEquipCursed, ToString());
            }

            hero.Sprite.UpdateArmor(hero.Tier()); // ((HeroSprite)hero.sprite).updateArmor() in the Java

            hero.SpendAndNext(Time2Equip(hero));
            return true;
        }
        else
        {
            Collect(hero.Belongings.Backpack);
            return false;
        }
    }

    protected internal override float Time2Equip(Hero hero) => 2 / hero.Speed();

    public override bool DoUnequip(Hero hero, bool collect, bool single)
    {
        if (base.DoUnequip(hero, collect, single))
        {
            hero.Belongings.Armor = null;
            hero.Sprite.UpdateArmor(hero.Tier()); // ((HeroSprite)hero.sprite).updateArmor() in the Java

            return true;
        }
        else
        {
            return false;
        }
    }

    public override bool IsEquipped(Hero hero) => hero.Belongings.Armor == this;

    public int Dr()
    {
        // TODO: a glyph adds 1 inside the brackets
        return Tier * (2 + EffectiveLevel());
    }

    public override Item Upgrade() => Upgrade(false);

    public Item Upgrade(bool inscribe)
    {
        // TODO: an inscribed armour upgraded with inscribe false loses its glyph when Random.Int(level()) > 0, logging TXT_INCOMPATIBLE
        if (inscribe)
        {
            Inscribe();
        }
        
        Str--;

        return base.Upgrade();
    }

    public Item SafeUpgrade()
    {
        // TODO: passe glyph != null
        return Upgrade(false);
    }

    public override Item Degrade()
    {
        Str++;
        return base.Degrade();
    }

    public override int MaxDurability(int lvl) => 6 * (lvl < 16 ? 16 - lvl : 1);

    public virtual int Proc(Char attacker, Char defender, int damage)
    {
        // TODO: damage = glyph.proc(this, attacker, defender, damage) when armor with glyph

        if (!LevelKnown)
        {
            if (--_hitsToKnow <= 0)
            {
                LevelKnown = true;
                GLog.W(TxtIdentify, Name(), ToString());
                // TODO: Badges.validateItemLevelAcquired(this)
            }
        }

        Use();
        return damage;
    }

    public override string ToString()
    {
        return LevelKnown
            ? TextUtils.Format(
                IsBroken()
                    ? TxtBroken
                    : TxtToString,
                base.ToString(),
                Str)
            : base.ToString();
    }

    // TODO: name() returns glyph.name(super.name()) for an inscribed armor

    public override Item Random()
    {
        if (Utils.Random.Float() < 0.4)
        {
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
        }

        if (Utils.Random.Int(10) == 0)
        {
            Inscribe();
        }

        return this;
    }

    public int TypicalStr() => 7 + Tier * 2;

    public int TypicalDr() => Tier * 2;

    // TODO: inscribe(Glyph) stores a glyph and isInscribed() reports one

    public Armor Inscribe()
    {
        // TODO: Glyph.random() builds one of the eleven glyphs, rerolling a repeat of the old glyph's class, and inscribe(gl) stores it
        Utils.Random.Chances(GlyphChances);
        return this;
    }

    // TODO: glowing() returns the glyph's glow
}