using PixelDungeon.Core.Items.Bags;

namespace PixelDungeon.Core.Items;

public static class Generator
{
    public enum Category
    {
        Weapon,
        Armor,
        Potion,
        Scroll,
        Wand,
        Ring,
        Seed,
        Food,
        Gold,
        Misc
    }

    public static readonly (float Prob, string SuperClass)[] CategoryInfo =
    [
        (15, "Weapon"),
        (10, "Armor"),
        (50, "Potion"),
        (40, "Scroll"),
        (4, "Wand"),
        (2, "Ring"),
        (5, "Seed"),
        (0, "Food"),
        (50, "Gold"),
        (5, "Item")
    ];

    // Category.order(item) in java
    // C# enums cant hold methods, superclasses are matched by name, so a superclass
    // not ported yey simply never matches
    // MISC's Item matches every item, so the Bag fallback is unreachable (just like Java)
    public static int Order(Item item)
    {
        for (var i = 0; i < CategoryInfo.Length; i++)
        {
            if (IsInstance(CategoryInfo[i].SuperClass, item))
            {
                return i;
            }
        }

        return item is Bag ? int.MaxValue : int.MaxValue - 1;
    }

    private static bool IsInstance(string className, Item item)
    {
        for (var type = item.GetType(); type != null; type = type.BaseType)
        {
            if (type.Name == className)
            {
                return true;
            }
        }

        return false;
    }

    // TODO: category weights, per-category class tables, reset()

    public static Item Random()
    {
        return null;
    }

    public static Item Random(Category cat)
    {
        return null;
    }
}