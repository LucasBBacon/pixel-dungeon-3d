using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;

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

public class Scroll : Item
{
    public Scroll()
    {
        NameValue = "test scroll";
    }
}