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