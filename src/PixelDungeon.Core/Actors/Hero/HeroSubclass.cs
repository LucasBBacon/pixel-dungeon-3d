using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Hero;

public enum HeroSubClass
{
    None,
    Gladiator,
    Beserker,
    Warlock,
    Battlemage,
    Assassin,
    Freerunner,
    Sniper,
    Warden
}

public static class HeroSubClasses
{
    private static string Title(this HeroSubClass subClass)
    {
        return subClass switch
        {
            HeroSubClass.Gladiator => "gladiator",
            HeroSubClass.Beserker => "berserker",
            HeroSubClass.Warlock => "warlock",
            HeroSubClass.Battlemage => "battlemage",
            HeroSubClass.Assassin => "assassin",
            HeroSubClass.Freerunner => "freerunner",
            HeroSubClass.Sniper => "sniper",
            HeroSubClass.Warden => "warden",
            _ => null
        };
    }

    public static string Desc(this HeroSubClass subClass)
    {
        return subClass switch
        {
            HeroSubClass.Gladiator =>
                "A successful attack with a melee weapon allows the _Gladiator_ to start a combo, " +
                "in which every next successful hit inflicts more damage.",
            HeroSubClass.Beserker => "When severely wounded, the _Berserker_ enters a state of wild fury " +
                                     "significantly increasing his damage output.",
            HeroSubClass.Warlock => "After killing an enemy the _Warlock_ consumes its soul. " +
                                    "It heals his wounds and satisfies his hunger.",
            HeroSubClass.Battlemage =>
                "When fighting with a wand in his hands, the _Battlemage_ inflicts additional damage depending " +
                "on the current number of charges. Every successful hit restores 1 charge to this wand.",
            HeroSubClass.Assassin =>
                "When performing a surprise attack, the _Assassin_ inflicts additional damage to his target.",
            HeroSubClass.Freerunner =>
                "The _Freerunner_ can move almost twice faster, than most of the monsters. When he " +
                "is running, the Freerunner is much harder to hit. For that he must be unencumbered and not starving.",
            HeroSubClass.Sniper => "_Snipers_ are able to detect weak points in an enemy's armor, " +
                                   "effectively ignoring it when using a missile weapon.",
            HeroSubClass.Warden =>
                "Having a strong connection with forces of nature gives _Wardens_ an ability to gather dewdrops and " +
                "seeds from plants. Also trampling a high grass grants them a temporary armor buff.",
            _ => null
        };
    }

    private const string TagSubClass = "subClass";

    public static void StoreInBundle(this HeroSubClass subClass, Bundle bundle)
    {
        bundle.Put(TagSubClass, subClass);
    }

    public static HeroSubClass RestoreInBundle(Bundle bundle)
    {
        var value = bundle.GetString(TagSubClass);
        return Enum.TryParse(value, out HeroSubClass result) ? result : HeroSubClass.None;
    }
}