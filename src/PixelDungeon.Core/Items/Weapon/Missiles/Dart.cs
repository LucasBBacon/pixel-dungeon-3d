using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class Dart : MissileWeapon
{
    public Dart() : this(1)
    {
    }

    public Dart(int number)
    {
        NameValue = "dart";
        ImageValue = ItemSpriteSheet.Dart;

        QuantityValue = number;
    }

    public override int Min() => 1;

    public override int Max() => 4;

    public override string Desc() =>
        "These simple metal spikes are weighted to fly true and sting their prey with a flick of the wrist.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(5, 15);
        return this;
    }

    public override int Price() => QuantityValue * 2;
}