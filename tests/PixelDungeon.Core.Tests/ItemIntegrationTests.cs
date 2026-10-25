using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Weapon;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;

namespace PixelDungeon.Core.Tests;

public class ItemIntegrationTests : DungeonFixture
{
    [Fact]
    public void GeneratedSewerLevels_HoldOnlyRegisteredClassesAndTheDewVial_InNonEmptyHeaps()
    {
        var sawWeapon = false;
        var sawArmor = false;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            Dungeon.Depth = 1;
            Dungeon.DewVial = true;
            Generator.Reset();

            var level = new SewerLevel();
            level.Create();

            Assert.NotEmpty(level.Heaps);
            foreach (var heap in level.Heaps.Values)
            {
                Assert.False(heap.IsEmpty(), $"seed {seed} has an empty heap at {heap.Pos}");
                Assert.NotEqual(HeapType.Mimic, heap.Type);
                Assert.All(heap.Items,
                    item => Assert.True(Generator.RegisteredTypes.Contains(item.GetType()) || item is DewVial,
                        $"seed {seed}: {item}"));
                sawWeapon |= heap.Items.Any(item => item is Weapon);
                sawArmor |= heap.Items.Any(item => item is Armor);
            }
        }

        Assert.True(sawWeapon && sawArmor);
    }

    [Fact]
    public void GeneratedSewerLevels_EachCarryOneFoodItem()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            Dungeon.Depth = 1;
            Dungeon.DewVial = true;
            Generator.Reset();

            var level = new SewerLevel();
            level.Create();

            var food = Assert.Single(
                level.Heaps.Values
                    .SelectMany(heap => heap.Items)
                    .OfType<Food>());
            Assert.Equal(1, food.Quantity());
            Assert.True(food.GetType() == typeof(Food) || food.GetType() == typeof(Pasty),
                $"seed {seed}: {food}");
        }
    }

    [Theory]
    [InlineData(typeof(Gold))]
    public void EveryItemType_ConstructsByType(Type type)
    {
        // detach and item.virtual build items with Activator.CreateInstance
        // every class item needs a parameterless constructor
        Assert.IsAssignableFrom<Item>(Activator.CreateInstance(type));
    }

    [Fact]
    public void ClickingDistantGold_WalksThereAndPicksItUp()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        Dungeon.Level.Drop(new Gold(25), TestLevel.At(3, 1));

        hero.Handle(TestLevel.At(3, 1));
        for (var i = 0; i < 10 && Dungeon.Gold == 0; i++)
        {
            Actor.Process();
        }

        Assert.Equal(25, Dungeon.Gold);
        Assert.Equal(TestLevel.At(3, 1), hero.Pos);
        Assert.Empty(Dungeon.Level.Heaps);
    }

    [Fact]
    public void Heaps_SurviveLeavingALevelAndComingBack()
    {
        Dungeon.Hero = null; // Descend builds the hero, as GameScene._Ready's first transition does
        Interlevel.Mode = InterlevelMode.Descend;
        Interlevel.Run();

        var before = Snapshot();
        Assert.NotEmpty(before);

        Interlevel.Mode = InterlevelMode.Descend;
        Interlevel.Run();
        Interlevel.Mode = InterlevelMode.Ascend;
        Interlevel.Run();

        Assert.Equal(before, Snapshot());
    }

    private static List<(int Pos, HeapType Type, string Items)> Snapshot()
    {
        return Dungeon.Level.Heaps.Values
            .Select(h => (h.Pos, h.Type, string.Join(",", h.Items.Select(i => i.ToString()))))
            .OrderBy(h => h.Pos)
            .ToList();
    }
}