using System.Reflection;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Plants;

public class SeedTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#########",
        "#.......#",
        "#.A.x...#",
        "#########"
    ];

    private static int C(int x, int y = 1) => TestLevel.At(x, y);

    private static void ThrowAt(Item item, int cell) =>
        typeof(Item)
            .GetMethod("OnThrow", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(item, [cell]);

    [Fact]
    public void Seed_Actions_AreDropThrowPlant()
    {
        LoadLevel(Room);
        var hero = PlaceHero(C(1));

        Assert.Equal(new[] { "DROP", "THROW", "PLANT" }, new Firebloom.Seed().Actions(hero));
        Assert.Equal("PLANT", Plant.Seed.AcPlant);
        Assert.Equal("THROW", new Firebloom.Seed().DefaultAction);
    }

    [Fact]
    public void Seed_InfoPriceIdentity()
    {
        var seed = new Icecap.Seed();
        seed.Quantity(3);

        Assert.Equal(
            "Throw this seed to the place where you want to grow an Icecap.\n\nUpon touching an Icecap excretes a pollen, which freezes everything in its vicinity.",
            seed.Info());
        Assert.Equal(30, seed.Price());
        Assert.False(seed.IsUpgradable());
        Assert.True(seed.IsIdentified());
        Assert.True(seed.Stackable);
        Assert.Equal("seed of Icecap", seed.Name());
        Assert.Equal(ItemSpriteSheet.SeedIcecap, seed.Image());
    }

    [Fact]
    public void Seed_ThrownOnFloor_Plants_OnPot_Drops_IntoPit_Falls()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 1));

        ThrowAt(new Sungrass.Seed(), C(3, 1));
        Assert.IsType<Sungrass>(Dungeon.Level.Plants[C(3, 1)]);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndPlant);
        Assert.False(Dungeon.Level.Heaps.ContainsKey(C(3, 1)));

        ThrowAt(new Sungrass.Seed(), C(2, 2)); // the alchemy pot
        Assert.False(Dungeon.Level.Plants.ContainsKey(C(2, 2)));
        Assert.IsType<Sungrass.Seed>(Dungeon.Level.Heaps[C(2, 2)].Peek());

        var falling = new Sungrass.Seed();
        ThrowAt(falling, C(4, 2)); // the chasm
        Assert.False(Dungeon.Level.Plants.ContainsKey(C(4, 2)));
        Assert.Contains(falling, Dungeon.DroppedItems[Dungeon.Depth + 1]);
    }

    [Fact]
    public void Seed_Plant_SpendsOneTurnAndPlantsUnderTheHero()
    {
        LoadLevel(Room);
        Dungeon.Hero = new SpyHero();
        var hero = PlaceHero(C(3, 1));
        hero.Sprite = new FakeCharView();
        var seed = new Icecap.Seed();
        seed.Quantity(2);
        seed.Collect();

        seed.Execute(hero, "PLANT");

        Assert.Equal(1f, ((SpyHero)hero).TimeLeft);
        Assert.False(hero.Ready);
        Assert.IsType<Icecap>(Dungeon.Level.Plants[C(3, 1)]);
        Assert.Equal(1, seed.Quantity());
        Assert.Equal(C(3, 1), Assert.Single(((FakeCharView)hero.Sprite).Operations));
    }

    [Fact]
    public void LandDroppedItems_PlantsASeed()
    {
        var level = LoadLevel(Room);
        level.RespawnCells = () => C(6, 1);
        Dungeon.DroppedItems[Dungeon.Depth] = [new Fadeleaf.Seed()];

        GameScene.LandDroppedItems();

        Assert.IsType<Fadeleaf>(level.Plants[C(6, 1)]);
        Assert.False(level.Heaps.ContainsKey(C(6, 1)));
        Assert.False(Dungeon.DroppedItems.ContainsKey(Dungeon.Depth));
    }

    [Fact]
    public void Seed_AlchemyClasses_MatchTheJava()
    {
        (Plant.Seed Seed, Type Potion)[] pairs =
        [
            (new Firebloom.Seed(), typeof(PotionOfLiquidFlame)),
            (new Icecap.Seed(), typeof(PotionOfFrost)),
            (new Sorrowmoss.Seed(), typeof(PotionOfToxicGas)),
            (new Dreamweed.Seed(), typeof(PotionOfInvisibility)),
            (new Sungrass.Seed(), typeof(PotionOfHealing)),
            (new Earthroot.Seed(), typeof(PotionOfParalyticGas)),
            (new Fadeleaf.Seed(), typeof(PotionOfMindVision)),
            (new Rotberry.Seed(), typeof(PotionOfStrength))
        ];
        foreach (var (seed, potion) in pairs)
        {
            Assert.Equal(potion, seed.AlchemyClass);
            Assert.Equal("seed of " + seed.PlantName, seed.Name());
        }
    }

    [Fact]
    public void SeedPouch_GrabsOnlySeeds_AndPullsCarriedSeedsIn()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var seed = new Firebloom.Seed();
        seed.Collect();
        var pebble = new StackItem(2);
        pebble.Collect();

        var pouch = new SeedPouch();
        pouch.Collect();

        Assert.True(pouch.Grab(new Icecap.Seed()));
        Assert.False(pouch.Grab(pebble));
        Assert.Contains(seed, pouch.Items);
        Assert.DoesNotContain(seed, Dungeon.Hero.Belongings.Backpack.Items);
        Assert.Contains(pebble, Dungeon.Hero.Belongings.Backpack.Items);

        new Icecap.Seed().Collect();
        Assert.Equal(2, pouch.Items.Count);
        Assert.Equal("seed pouch", pouch.Name());
        Assert.Equal(8, pouch.Size);
        Assert.Equal(50, pouch.Price());
        Assert.Equal("This small velvet pouch allows you to store any number of seeds in it. Very convenient.",
            pouch.Info());
    }

    [Fact]
    public void SeedPouch_InBackpack_Bundle_RoundTrips()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        new SeedPouch().Collect();
        new Sorrowmoss.Seed().Collect();

        var bundle = new Bundle();
        Dungeon.Hero.Belongings.Backpack.StoreInBundle(bundle);
        var restored = new Bag { Size = 19 };
        restored.RestoreFromBundle(bundle);

        var back = Assert.Single(restored.Items.OfType<SeedPouch>());
        Assert.IsType<Sorrowmoss.Seed>(Assert.Single(back.Items));
    }

    [Fact]
    public void HighGrass_TrampleCanDropASeed()
    {
        var seeds = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            LoadLevel(
                "###",
                "#\"#",
                "###"
            );
            var cell = C(1, 1);

            HighGrass.Trample(Dungeon.Level, cell, null);

            if (Dungeon.Level.Heaps.TryGetValue(cell, out var heap) &&
                heap.Items.Any(i => i is Plant.Seed))
            {
                seeds++;
            }
        }

        Assert.InRange(seeds, 1, 199);
    }

    [Fact]
    public void HighGrass_LeafBurst_UsesTheLevelMidpoint()
    {
        LoadLevel("###", "#\"#", "###");
        HighGrass.Trample(Dungeon.Level, C(1, 1), null);
        Assert.Contains((C(1, 1), 0xFF448822u, 4), View.Splashes);

        Dungeon.Depth = 1;
        var sewer = new SewerLevel();
        sewer.Create();
        Dungeon.Level = sewer;
        View.Splashes.Clear();
        var cell = Enumerable
            .Range(0, Level.Length)
            .First(i => Level.Passable[i]);

        HighGrass.Trample(sewer, cell, null);

        Assert.Contains((cell, 0xFF508743u, 4), View.Splashes);
    }

    [Fact]
    public void RandomSeed_NeverRotberry_OverAThousandRolls()
    {
        var kinds = new HashSet<Type>();
        for (var seed = 1; seed <= 1000; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            var item = Generator.Random(Generator.Category.Seed);
            Assert.IsType<Plant.Seed>(item, exactMatch: false);
            Assert.IsNotType<Rotberry.Seed>(item);
            kinds.Add(item.GetType());
        }

        Assert.Equal(7, kinds.Count);
    }

    [Fact]
    public void Seed_OrderIsSeedCategory()
    {
        Assert.Equal((int)Generator.Category.Seed, Generator.Order(new Rotberry.Seed()));
    }
}