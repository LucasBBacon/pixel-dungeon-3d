using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class Shuriken : MissileWeapon
{
    public Shuriken() : this(1)
    {
    }

    public Shuriken(int number)
    {
        NameValue = "shuriken";
        ImageValue = ItemSpriteSheet.Shuriken;

        Str = 13;

        Dly = 0.5f;

        QuantityValue = number;
    }

    public override int Min() => 2;

    public override int Max() => 6;

    public override string Desc() =>
        "Star-shaped pieces of metal with razor-sharp blades do significant damage when they hit a target. They can be thrown at very high rate.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(5, 15);
        return this;
    }

    public override int Price() => 15 * QuantityValue;
}