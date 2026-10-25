using PixelDungeon.Core.Actors.Hero;
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

    private static readonly Dictionary<Category, (float Chance, string Cls)[]> Tables = new()
    {
        [Category.Gold] =
        [
            (1, "Gold")
        ],
        [Category.Scroll] =
        [
            (30, "ScrollOfIdentify"),
            (10, "ScrollOfTeleportation"),
            (15, "ScrollOfRemoveCurse"),
            (10, "ScrollOfRecharging"),
            (15, "ScrollOfMagicMapping"),
            (12, "ScrollOfChallenge"),
            (8, "ScrollOfTerror"),
            (8, "ScrollOfLullaby"),
            (4, "ScrollOfPsionicBlast"),
            (6, "ScrollOfMirrorImage"),
            (0, "ScrollOfUpgrade"),
            (1, "ScrollOfEnchantment")
        ],
        [Category.Potion] =
        [
            (45, "PotionOfHealing"),
            (4, "PotionOfExperience"),
            (15, "PotionOfToxicGas"),
            (10, "PotionOfParalyticGas"),
            (15, "PotionOfLiquidFlame"),
            (10, "PotionOfLevitation"),
            (0, "PotionOfStrength"),
            (20, "PotionOfMindVision"),
            (12, "PotionOfPurity"),
            (10, "PotionOfInvisibility"),
            (0, "PotionOfMight"),
            (10, "PotionOfFrost")
        ],
        [Category.Wand] =
        [
            (10, "WandOfTeleportation"),
            (10, "WandOfSlowness"),
            (15, "WandOfFirebolt"),
            (6, "WandOfRegrowth"),
            (10, "WandOfPoison"),
            (11, "WandOfBlink"),
            (15, "WandOfLightning"),
            (10, "WandOfAmok"),
            (6, "WandOfReach"),
            (10, "WandOfFlock"),
            (0, "WandOfMagicMissile"),
            (5, "WandOfDisintegration"),
            (5, "WandOfAvalanche")
        ],
        [Category.Weapon] =
        [
            (1, "Dagger"),
            (1, "Knuckles"),
            (1, "Quarterstaff"),
            (1, "Spear"),
            (1, "Mace"),
            (1, "Sword"),
            (1, "Longsword"),
            (1, "BattleAxe"),
            (1, "WarHammer"),
            (1, "Glaive"),
            (0, "ShortSword"),
            (0, "Dart"),
            (1, "Javelin"),
            (1, "IncendiaryDart"),
            (1, "CurareDart"),
            (1, "Shuriken"),
            (0, "Boomerang"),
            (1, "Tamahawk")
        ],
        [Category.Armor] =
        [
            (1, "ClothArmor"),
            (1, "LeatherArmor"),
            (1, "MailArmor"),
            (1, "ScaleArmor"),
            (1, "PlateArmor")
        ],
        [Category.Food] =
        [
            (4, "Food"),
            (1, "Pasty"),
            (0, "MysteryMeat")
        ],
        [Category.Ring] =
        [
            (1, "RingOfMending"),
            (1, "RingOfDetection"),
            (1, "RingOfShadows"),
            (1, "RingOfPower"),
            (1, "RingOfHerbalism"),
            (1, "RingOfAccuracy"),
            (1, "RingOfEvasion"),
            (1, "RingOfSatiety"),
            (1, "RingOfHaste"),
            (1, "RingOfElements"),
            (0, "RingOfHaggler"),
            (0, "RingOfThorns")
        ],
        [Category.Seed] =
        [
            (1, "Firebloom.Seed"),
            (1, "Icecap.Seed"),
            (1, "Sorrowmoss.Seed"),
            (1, "Dreamweed.Seed"),
            (1, "Sungrass.Seed"),
            (1, "Earthroot.Seed"),
            (1, "Fadeleaf.Seed"),
            (0, "Rotberry.Seed")
        ],
        [Category.Misc] =
        [
            (2, "Bomb"),
            (1, "Honeypot")
        ],
    };

    private static readonly Dictionary<Type, Func<Item>> Registry = new()
    {
        [typeof(Gold)] = () => new Gold(),

        [typeof(Weapon.Melee.Dagger)] = () => new Weapon.Melee.Dagger(),
        [typeof(Weapon.Melee.Knuckles)] = () => new Weapon.Melee.Knuckles(),
        [typeof(Weapon.Melee.Quarterstaff)] = () => new Weapon.Melee.Quarterstaff(),
        [typeof(Weapon.Melee.Spear)] = () => new Weapon.Melee.Spear(),
        [typeof(Weapon.Melee.Mace)] = () => new Weapon.Melee.Mace(),
        [typeof(Weapon.Melee.Sword)] = () => new Weapon.Melee.Sword(),
        [typeof(Weapon.Melee.Longsword)] = () => new Weapon.Melee.Longsword(),
        [typeof(Weapon.Melee.BattleAxe)] = () => new Weapon.Melee.BattleAxe(),
        [typeof(Weapon.Melee.WarHammer)] = () => new Weapon.Melee.WarHammer(),
        [typeof(Weapon.Melee.Glaive)] = () => new Weapon.Melee.Glaive(),
        [typeof(Weapon.Melee.ShortSword)] = () => new Weapon.Melee.ShortSword(), // weight 0: a class kit's
        [typeof(Weapon.Missiles.Dart)] = () => new Weapon.Missiles.Dart(), // weight 0: a class kit's
        [typeof(Weapon.Missiles.Javelin)] = () => new Weapon.Missiles.Javelin(),
        [typeof(Weapon.Missiles.CurareDart)] = () => new Weapon.Missiles.CurareDart(),
        [typeof(Weapon.Missiles.Shuriken)] = () => new Weapon.Missiles.Shuriken(),
        [typeof(Weapon.Missiles.Tamahawk)] = () => new Weapon.Missiles.Tamahawk(),

        [typeof(Armor.ClothArmor)] = () => new Armor.ClothArmor(),
        [typeof(Armor.LeatherArmor)] = () => new Armor.LeatherArmor(),
        [typeof(Armor.MailArmor)] = () => new Armor.MailArmor(),
        [typeof(Armor.ScaleArmor)] = () => new Armor.ScaleArmor(),
        [typeof(Armor.PlateArmor)] = () => new Armor.PlateArmor(),

        [typeof(Food.Food)] = () => new Food.Food(),
        [typeof(Food.Pasty)] = () => new Food.Pasty(),
        [typeof(Food.MysteryMeat)] = () => new Food.MysteryMeat(),
    };

    public static IEnumerable<Type> RegisteredTypes => Registry.Keys;

    public static readonly Dictionary<Category, float> CategoryProbabilities = new();

    public static float CategoryProbability(Category category) => CategoryProbabilities[category];

    public static void Reset()
    {
        foreach (var category in Enum.GetValues<Category>())
        {
            CategoryProbabilities[category] = CategoryInfo[(int)category].Prob;
        }
    }

    public static Item Random()
    {
        // only categories with a registered class take part in roll
        var eligible = new Dictionary<Category, float>();
        foreach (var category in Enum.GetValues<Category>())
        {
            if (Tables[category].Any(entry => Resolve(entry.Cls) != null))
            {
                eligible[category] = CategoryProbabilities[category];
            }
        }

        // Utils.Random.Chances reads the first weight unguarded, empty table throws
        return eligible.Count == 0
            ? null
            : Random(Utils.Random.Chances(eligible));
    }

    public static Item Random(Category category)
    {
        // java wraps this in a catch-all for reflection failures
        // registry can't fail that way
        // an unported class is an explicit null, no catch
        CategoryProbabilities[category] /= 2;

        switch (category)
        {
            case Category.Armor:
                return RandomArmor();
            case Category.Weapon:
                return RandomWeapon();
            default:
                var cl = Roll(Tables[category]);
                return cl == null ? null : Registry[cl]().Random();
        }
    }

    public static Item Random(Type cl) =>
        Registry.TryGetValue(cl, out var factory)
            ? factory().Random()
            : null;

    private static Item RandomArmor()
    {
        var curStr = Hero.StartingStr + Dungeon.PotionOfStrength;

        var cat = Category.Armor;

        var cl1 = Roll(Tables[cat]);
        if (cl1 == null)
        {
            return null;
        }

        var a1 = (Armor.Armor)Registry[cl1]();
        var a2 = (Armor.Armor)Registry[Roll(Tables[cat])]();

        a1.Random();
        a2.Random();

        return Math.Abs(curStr - a1.Str) < Math.Abs(curStr - a2.Str) ? a1 : a2;
    }

    private static Item RandomWeapon()
    {
        var curStr = Hero.StartingStr + Dungeon.PotionOfStrength;

        var cat = Category.Weapon;

        var cl1 = Roll(Tables[cat]);
        if (cl1 == null)
        {
            return null;
        }

        var w1 = (Weapon.Weapon)Registry[cl1]();
        var w2 = (Weapon.Weapon)Registry[Roll(Tables[cat])]();

        w1.Random();
        w2.Random();

        return Math.Abs(curStr - w1.Str) < Math.Abs(curStr - w2.Str) ? w1 : w2;
    }

    private static Type Roll((float Chance, string Cls)[] table)
    {
        var chances = new List<float>();
        var classes = new List<Type>();

        foreach (var (chance, name) in table)
        {
            var cl = Resolve(name);
            if (cl == null) continue;

            chances.Add(chance);
            classes.Add(cl);
        }

        return classes.Count == 0
            ? null
            : classes[Utils.Random.Chances([.. chances])];
    }

    private static Type Resolve(string name)
    {
        return (from type in Registry.Keys
            let tableName = type.DeclaringType != null
                ? $"{type.DeclaringType.Name}.{type.Name}"
                : type.Name
            where tableName == name
            select type).FirstOrDefault();
    }
}