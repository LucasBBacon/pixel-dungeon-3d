using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Hero;

public enum HeroClass
{
    Warrior,
    Mage,
    Rogue,
    Huntress
}

public static class HeroClasses
{
    public static string Title(this HeroClass heroClass)
    {
        return heroClass switch
        {
            HeroClass.Warrior => "warrior",
            HeroClass.Mage => "mage",
            HeroClass.Rogue => "rogue",
            HeroClass.Huntress => "huntress",
            _ => null
        };
    }

    private const string TagClass = "class";

    public static void StoreInBundle(this HeroClass heroClass, Bundle bundle)
    {
        bundle.Put(TagClass, heroClass);
    }

    public static HeroClass RestoreInBundle(Bundle bundle)
    {
        var value = bundle.GetString(TagClass);
        return value.Length > 0 ? Enum.Parse<HeroClass>(value) : HeroClass.Rogue;
    }
}