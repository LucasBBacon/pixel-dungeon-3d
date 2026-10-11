using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class GeneratorTests : DungeonFixture
{
    [Fact]
    public void Random_EverySeed_ReturnsGold()
    {
        Dungeon.Depth = 1;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            Generator.Reset();
            Assert.IsType<Gold>(Generator.Random());
        }
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
        Assert.Null(Generator.Random(Generator.Category.Food));
        Assert.Null(Generator.Random(Generator.Category.Weapon));
        Assert.Null(Generator.Random(Generator.Category.Armor));
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