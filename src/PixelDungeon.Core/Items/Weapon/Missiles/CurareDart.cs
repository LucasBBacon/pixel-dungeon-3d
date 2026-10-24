using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Missiles;

public class CurareDart : MissileWeapon
{
    public static float Duration = 3f;

    public CurareDart() : this(1)
    {
    }

    public CurareDart(int number)
    {
        NameValue = "curare dart";
        ImageValue = ItemSpriteSheet.CurareDart;

        Str = 14;

        QuantityValue = number;
    }

    public override int Min() => 1;

    public override int Max() => 3;

    public override void Proc(Char attacker, Char defender, int damage)
    {
        Buff.Prolong<Paralysis>(defender, Duration);
        base.Proc(attacker, defender, damage);
    }

    public override string Desc() =>
        "These little evil darts don't do much damage but they can paralyze the target leaving it helpless and motionless for some time.";

    public override Item Random()
    {
        QuantityValue = Utils.Random.Int(2, 5);
        return this;
    }

    public override int Price() => 12 * QuantityValue;
}