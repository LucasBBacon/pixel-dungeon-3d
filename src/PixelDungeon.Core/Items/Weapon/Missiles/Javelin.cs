using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class Javelin : MissileWeapon
{
    public Javelin() : this(1)
    {
    }

    public Javelin(int number)
    {
        NameValue = "javelin";
        ImageValue = ItemSpriteSheet.Javelin;

        Str = 15;

        QuantityValue = number;
    }

    public override int Min() => 2;

    public override int Max() => 15;

    public override void Proc(Char attacker, Char defender, int damage)
    {
        base.Proc(attacker, defender, damage);
        Buff.Prolong<Cripple>(defender, Cripple.Duration);
    }

    public override string Desc() =>
        "This length of metal is weighted to keep the spike at its tip foremost as it sails through the air.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(5, 15);
        return this;
    }

    public override int Price() => 15 * QuantityValue;
}