using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Items.Weapon;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class GeneratorTests : DungeonFixture
{
    private static readonly Type[] WeaponClasses =
    [
        typeof(Dagger),
        typeof(Knuckles),
        typeof(Quarterstaff),
        typeof(Spear),
        typeof(Mace),
        typeof(Sword),
        typeof(Longsword),
        typeof(BattleAxe),
        typeof(WarHammer),
        typeof(Glaive),
        typeof(ShortSword),
        typeof(Dart),
        typeof(Javelin),
        typeof(CurareDart),
        typeof(Shuriken),
        typeof(Tamahawk)
    ];

    private static readonly float[] WeaponChances =
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
        0,
        0,
        1,
        1,
        1,
        1
    ];

    // The ARMOR table: tiers 1 to 5 in order, each weighted 1.
    private static readonly Type[] ArmorClasses =
    [
        typeof(ClothArmor),
        typeof(LeatherArmor),
        typeof(MailArmor),
        typeof(ScaleArmor),
        typeof(PlateArmor)
    ];

    private static readonly float[] ArmorChances = [1, 1, 1, 1, 1];

    [Fact]
    public void Random_OverManySeeds_ReturnsOnlyGoldWeaponsAndArmour()
    {
        Dungeon.Depth = 1;
        var gold = 0;
        var weapons = 0;
        var armour = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();

            var item = Generator.Random();
            switch (item)
            {
                case Gold:
                    gold++;
                    break;
                case Weapon:
                    weapons++;
                    break;
                case Armor:
                    armour++;
                    break;
                default:
                    Assert.Fail($"seed {seed}: {item?.GetType().Name ?? "null"}");
                    break;
            }
        }

        // Each is at least a 10-in-75 draw, so 200 seeds miss one with a chance below 1e-12.
        Assert.True(gold > 0 && weapons > 0 && armour > 0,
            $"gold {gold}, weapons {weapons}, armour {armour}");
    }

    [Fact]
    public void RandomCategory_HalvesThatCategoryWeight_AndResetRestoresIt()
    {
        Generator.Reset();
        Assert.Equal(50f, Generator.CategoryProbability(Generator.Category.Gold));

        Generator.Random(Generator.Category.Gold);
        Assert.Equal(25f, Generator.CategoryProbability(Generator.Category.Gold));

        Generator.Reset();
        Assert.Equal(50f, Generator.CategoryProbability(Generator.Category.Gold));
    }

    [Fact]
    public void InterlevelRun_ResetsTheWeightsFirst()
    {
        Generator.Random(Generator.Category.Gold);
        Interlevel.Mode = InterlevelMode.None; // a mode whose body does nothing
        Interlevel.Run();
        Assert.Equal(50f, Generator.CategoryProbability(Generator.Category.Gold));
    }

    [Fact]
    public void RandomCategory_WithRegisteredClass_ReturnsNull()
    {
        Assert.Null(Generator.Random(Generator.Category.Potion));
        Assert.Null(Generator.Random(Generator.Category.Scroll));
        Assert.Null(Generator.Random(Generator.Category.Wand));
        Assert.Null(Generator.Random(Generator.Category.Ring));
        Assert.Null(Generator.Random(Generator.Category.Seed));
        Assert.Null(Generator.Random(Generator.Category.Misc));
    }

    [Fact]
    public void RandomType_BuildsRegisteredClassesOnly()
    {
        Dungeon.Depth = 1;
        var gold = Assert.IsType<Gold>(Generator.Random(typeof(Gold)));
        Assert.InRange(gold.Quantity(), 30, 59);
        Assert.Null(Generator.Random(typeof(TestItem)));
    }

    [Theory]
    [InlineData(1, 30, 59)]
    [InlineData(4, 60, 119)]
    public void GoldRandom_StaysWithinTheDepthRange(int depth, int min, int max)
    {
        Dungeon.Depth = depth;
        for (var i = 0; i < 200; i++)
        {
            Assert.InRange(new Gold().Random().Quantity(), min, max);
        }
    }

    [Fact]
    public void Order_PutsGoldInGold_AndKeysBagsAndPlainItemsInMisc()
    {
        Assert.Equal((int)Generator.Category.Gold, Generator.Order(new Gold()));
        Assert.Equal((int)Generator.Category.Misc, Generator.Order(new IronKey()));
        Assert.Equal((int)Generator.Category.Misc, Generator.Order(new Keyring()));
        Assert.Equal((int)Generator.Category.Misc, Generator.Order(new TestItem()));
    }

    [Fact]
    public void EveryRegisteredType_Constructs()
    {
        Assert.NotEmpty(Generator.RegisteredTypes);
        foreach (var type in Generator.RegisteredTypes)
        {
            Assert.IsType<Item>(Activator.CreateInstance(type), exactMatch: false);
        }
    }

    [Fact]
    public void Registry_HoldsGold_AndEveryPortedWeaponsArmorAndFood()
    {
        // 1 + 16 + 5 + 3 = 25. Short Sword, Dart and Mystery Meat weigh 0 but are registered for kits,
        // loot and Random(Type)
        // TODO: Incendiary dart
        var expected = new[] { typeof(Gold) }
            .Concat(WeaponClasses)
            .Concat(ArmorClasses)
            .Concat([typeof(Food), typeof(Pasty), typeof(MysteryMeat)]);

        Assert.Equal(expected.Select(t => t.FullName)
                .OrderBy(n => n, StringComparer.Ordinal),
            Generator.RegisteredTypes.Select(t => t.FullName)
                .OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public void RandomType_BuildsTheZeroWeightKitAndLootClasses()
    {
        Assert.IsType<ShortSword>(Generator.Random(typeof(ShortSword)));
        Assert.IsType<MysteryMeat>(Generator.Random(typeof(MysteryMeat)));
        var darts = Assert.IsType<Dart>(Generator.Random(typeof(Dart)));
        Assert.InRange(darts.Quantity(), 5, 14);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void RandomWeapon_RollsTowClassesThenTwoItems_AndKeepsStrictlyCloserStrength(int potionsOfStrength)
    {
        Dungeon.PotionOfStrength = potionsOfStrength;
        Dungeon.Hero.STR = 25;
        var curStr = Hero.StartingStr + potionsOfStrength;
        var firstCloser = 0;
        var secondCloser = 0;
        var ties = 0;

        for (var seed = 1; seed <= 300; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            var result = Generator.Random(Generator.Category.Weapon);
            var nextAfterGenerator = Random.Float();

            Random.Seed(seed);
            var first = (Weapon)Activator.CreateInstance(WeaponClasses[Random.Chances(WeaponChances)]);
            var second = (Weapon)Activator.CreateInstance(WeaponClasses[Random.Chances(WeaponChances)]);
            Assert.NotNull(first);
            Assert.NotNull(second);
            first.Random();
            second.Random();
            var nextAfterReplay = Random.Float();

            var d1 = Math.Abs(curStr - first.Str);
            var d2 = Math.Abs(curStr - second.Str);
            var expected = d1 < d2 ? first : second;
            if (d1 < d2)
            {
                firstCloser++;
            }
            else if (d1 > d2)
            {
                secondCloser++;
            }
            else
            {
                ties++;
            }

            var weapon = Assert.IsType<Weapon>(result, exactMatch: false);
            Assert.True(expected.GetType() == weapon.GetType(),
                $"seed {seed}: expected a {expected.GetType().Name}, got a {weapon.GetType().Name}");
            Assert.Equal(expected.Str, weapon.Str);
            Assert.Equal(expected.Level(), weapon.Level());
            Assert.Equal(expected.Cursed, weapon.Cursed);
            Assert.Equal(expected.Quantity(), weapon.Quantity());
            Assert.Equal(nextAfterReplay, nextAfterGenerator);
        }

        // All three branches occur, two same-STR classes (Dagger and Knuckles, say) tie on most rolls.
        Assert.True(firstCloser > 0 && secondCloser > 0 && ties > 0,
            $"first closer {firstCloser}, second closer {secondCloser}, ties {ties}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void RandomArmor_RollsTwoClassesThenTwoItems_AndKeepsTheStrictlyCloserStrength(int potionsOfStrength)
    {
        Dungeon.PotionOfStrength = potionsOfStrength;
        Dungeon.Hero.STR = 25;
        var curStr = Hero.StartingStr + potionsOfStrength;
        var firstCloser = 0;
        var secondCloser = 0;
        var ties = 0;

        for (var seed = 1; seed <= 300; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            var result = Generator.Random(Generator.Category.Armor);
            var nextAfterGenerator = Random.Float();

            Random.Seed(seed);
            var tier1 = Random.Chances(ArmorChances) + 1; // ArmorClasses is ordered by tier
            var tier2 = Random.Chances(ArmorChances) + 1;
            var level1 = ArmorRolls.ReplayArmorRandom(out _);
            var level2 = ArmorRolls.ReplayArmorRandom(out _);
            var nextAfterReplay = Random.Float();

            var str1 = 7 + tier1 * 2 - level1;
            var str2 = 7 + tier2 * 2 - level2;
            var d1 = Math.Abs(curStr - str1);
            var d2 = Math.Abs(curStr - str2);
            var keepFirst = d1 < d2; // a tie keeps the second
            if (d1 < d2)
            {
                firstCloser++;
            }
            else if (d1 > d2)
            {
                secondCloser++;
            }
            else
            {
                ties++;
            }

            var armour = Assert.IsType<Armor>(result, exactMatch: false);
            var level = keepFirst ? level1 : level2;
            Assert.Equal(ArmorClasses[(keepFirst ? tier1 : tier2) - 1], armour.GetType());
            Assert.Equal(keepFirst ? str1 : str2, armour.Str);
            Assert.Equal(level, armour.Level());
            Assert.Equal(level < 0, armour.Cursed);
            Assert.Equal(nextAfterReplay, nextAfterGenerator);
        }

        Assert.True(firstCloser > 0 && secondCloser > 0 && ties > 0,
            $"first closer {firstCloser}, second closer {secondCloser}, ties {ties}");
    }

    [Fact]
    public void RandomCategory_Food_RollsARationOrAPasty_AndNeverMysteryMeat()
    {
        Type[] foodClasses = [typeof(Food), typeof(Pasty), typeof(MysteryMeat)];
        var rations = 0;
        var pasties = 0;

        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            var result = Generator.Random(Generator.Category.Food);
            var nextAfterGenerator = Random.Float();

            Random.Seed(seed);
            var expected = foodClasses[Random.Chances([4, 1, 0])];
            var nextAfterReplay = Random.Float();

            Assert.NotNull(result);
            Assert.Equal(expected, result.GetType());
            Assert.Equal(nextAfterReplay, nextAfterGenerator);
            if (result.GetType() == typeof(Food))
            {
                rations++;
            }

            if (result.GetType() == typeof(Pasty))
            {
                pasties++;
            }
        }

        Assert.Equal(200, rations + pasties);
        Assert.InRange(pasties, 1, 199);
    }

    [Fact]
    public void GoldDoPickUp_AddsToTheTotals_ShowsTheValue_AndStaysOutOfTheBackpack()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;

        Assert.True(new Gold(45).DoPickUp(hero));

        Assert.Equal(45, Dungeon.Gold);
        Assert.Equal(45, Statistics.GoldCollected);
        Assert.Contains((StatusColor.Neutral, "+45"), view.Statuses);
        Assert.IsType<Gold>(View.PickedUp[0]);
        Assert.Empty(hero.Belongings.Backpack.Items);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndGold);
    }

    [Fact]
    public void Gold_InfoAndBundle_FollowTheQuantity()
    {
        Assert.Equal("One gold coin. Collect gold coins to spend them later in a shop.", new Gold(1).Info());
        Assert.Equal("A pile of 7 gold coins. Collect gold coins to spend them later in a shop.", new Gold(7).Info());

        var bundle = new Bundle();
        bundle.Put("gold", new Gold(33));
        Assert.Equal(33, ((Gold)bundle.Get("gold")).Quantity());
    }

    private sealed class SureGnoll : Gnoll
    {
        public SureGnoll()
        {
            LootChance = 1f;
        }
    }

    [Fact]
    public void GnollDeath_WithinTheHeroLevelGate_DropsGoldWithAHop()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 1;
        var gnoll = new SureGnoll { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
        GameScene.Add(gnoll);

        gnoll.Die(Dungeon.Hero);

        var heap = Dungeon.Level.Heaps[TestLevel.At(3, 1)];
        Assert.InRange(Assert.IsType<Gold>(heap.Peek()).Quantity(), 30, 59);
        Assert.Equal(1, ((FakeHeapView)heap.Sprite).Drops);
    }

    [Fact]
    public void GnollDeath_ToAHeroAboveMaxLvlPlusTwo_DoesNothing()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        Dungeon.Hero.Lvl = 11; // GnollMaxLvl = 8
        var gnoll = new SureGnoll { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
        GameScene.Add(gnoll);

        gnoll.Die(Dungeon.Hero);

        Assert.Empty(Dungeon.Level.Heaps);
    }
}