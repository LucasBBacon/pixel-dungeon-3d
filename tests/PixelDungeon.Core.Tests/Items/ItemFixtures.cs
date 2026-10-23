using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Weapon;

namespace PixelDungeon.Core.Tests.Items;

public class TestItem : Item
{
    public TestItem()
    {
        NameValue = "test item";
    }
}

public class StackItem : Item
{
    public StackItem()
    {
        NameValue = "pebble";
        Stackable = true;
    }

    public StackItem(int quantity) : this()
    {
        QuantityValue = quantity;
    }
}

public class DurableItem : Item
{
    public DurableItem()
    {
        NameValue = "durable thing";
    }

    public override int MaxDurability(int lvl) => 12;
}

public class TestWeapon : Weapon
{
    public int Activations;

    public TestWeapon()
    {
        NameValue = "test weapon";
    }

    public override int Min() => 1;

    public override int Max() => 4;

    public override void Activate(Hero hero) => Activations++;
}

public static class WeaponRolls
{
    public static readonly float[] EnchantmentChances =
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

    public static int ReplayWeaponRandom()
    {
        if (!(Random.Float() < 0.4))
            return 0;

        var n = 1;
        if (Random.Int(3) != 0)
            return Random.Int(2) == 0 ? n : -n;

        n++;
        if (Random.Int(3) == 0)
        {
            n++;
        }

        return Random.Int(2) == 0 ? n : -n;
    }
}

public static class ArmorRolls
{
    public static readonly float[] GlyphChances =
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

    public static int ReplayArmorRandom(out bool inscribed)
    {
        var level = WeaponRolls.ReplayWeaponRandom();
        inscribed = Random.Int(10) == 0;
        if (inscribed)
        {
            Random.Chances(GlyphChances);
        }

        return level;
    }
}

public class Scroll : Item
{
    public Scroll()
    {
        NameValue = "test scroll";
    }
}