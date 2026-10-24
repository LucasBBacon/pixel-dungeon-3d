using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class Tamahawk : MissileWeapon
{
    public Tamahawk() : this(1)
    {
    }

    public Tamahawk(int number)
    {
        NameValue = "tomahawk";
        ImageValue = ItemSpriteSheet.Tomahawk;

        Str = 17;

        QuantityValue = number;
    }

    public override int Min() => 4;

    public override int Max() => 20;

    public override void Proc(Char attacker, Char defender, int damage)
    {
        base.Proc(attacker, defender, damage);
        Buff.Affect<Bleeding>(defender).Set(damage);
    }

    public override string Desc() =>
        "This throwing axe is not that heavy, but it still requires significant strength to be used effectively.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(5, 12);
        return this;
    }

    public override int Price() => 20 * QuantityValue;
}