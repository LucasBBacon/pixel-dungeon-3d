using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class AlchemyItems : DungeonFixture
{
    private static readonly string[] PotRoom =
    [
        "#########",
        "#PPPPPPP#",
        "#PPAPPPP#",
        "#PPPPPPP#",
        "#########"
    ];

    private static readonly string[] HiddenPot =
    [
        "#########",
        "#.#PPP###",
        "#.#PAP###",
        "#.#PPP###",
        "#.......#",
        "#########"
    ];

    private static int C(int x, int y = 1) => TestLevel.At(x, y);

    private static readonly int Pot = C(3, 2);

    private sealed class NullClassSeed : Plant.Seed;

    private static void Tick(Actor actor) =>
        typeof(Actor)
            .GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(actor, null);

    private static void ThrowAt(Item item, int cell)
    {
        typeof(Item)
            .GetMethod("OnThrow", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(item, [cell]);
    }

    private static Heap HeapWith(int cell, params Item[] items)
    {
        var heap = new Heap { Pos = cell };
        Dungeon.Level.Heaps[cell] = heap;
        foreach (var item in items)
        {
            heap.Drop(item);
        }

        return heap;
    }

    private static T Many<T>(int n) where T : Item, new()
    {
        var item = new T();
        item.Quantity(n);
        return item;
    }

    private static int SeedWhere(bool zero)
    {
        for (var s = 1; s < 1000; s++)
        {
            Random.Seed(s);
            if ((Random.Int(3) == 0) == zero)
            {
                return s;
            }
        }

        throw new InvalidOperationException();
    }

    [Fact]
    public void Transmute_TwoSeeds_ReturnsNullAndKeepsTheHeap()
    {
        LoadLevel(PotRoom);
        var heap = HeapWith(Pot, Many<Firebloom.Seed>(2));

        var result = heap.Transmute();

        Assert.Null(result);
        Assert.Same(heap, Dungeon.Level.Heaps[Pot]);
        Assert.Equal(2, heap.Items.First.Value.Quantity());
        Assert.Equal(new[] { (Pot, 0xFFAADDFFu, 3), (Pot, 0xFFFFFFFFu, 3) }, View.Splashes);
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndPuff);
        Assert.Equal(0, Statistics.PotionsCooked);
    }

    [Fact]
    public void Transmute_ThreeFirebloomSeeds_BrewsLiquidFlameOrARandomPotion()
    {
        // branch one: Random.Int(3) == 0 gives any potion.
        LoadLevel(PotRoom);
        var heap = HeapWith(Pot, Many<Firebloom.Seed>(3));
        Statistics.PotionsCooked = 0;
        Random.Seed(SeedWhere(true));
        var result = heap.Transmute();
        Assert.IsType<Potion>(result, exactMatch: false);
        Assert.Equal(1, Statistics.PotionsCooked);
        Assert.False(Dungeon.Level.Heaps.ContainsKey(Pot));
        Assert.Contains((Pot, 0xFFEEEEEEu, 6), View.Splashes);
        Assert.Contains((Pot, 0xFFFFFFFFu, 3), View.Splashes);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndPuff);

        // branch two: the seed's own potion.
        LoadLevel(PotRoom);
        heap = HeapWith(Pot, Many<Firebloom.Seed>(3));
        Statistics.PotionsCooked = 0;
        Random.Seed(SeedWhere(false));
        result = heap.Transmute();
        Assert.IsType<PotionOfLiquidFlame>(result);
        Assert.Equal(1, Statistics.PotionsCooked);
        Assert.False(Dungeon.Level.Heaps.ContainsKey(Pot));
        Assert.Null(heap.Items);
    }

    [Fact]
    public void Transmute_ANonSeed_SpoilsTheCount()
    {
        LoadLevel(PotRoom);
        var heap = HeapWith(Pot, Many<Firebloom.Seed>(3), new PotionOfHealing());
        View.Splashes.Clear();

        var result = heap.Transmute();

        Assert.Null(result);
        Assert.Equal(2, heap.Items.Count);
        Assert.DoesNotContain(View.Splashes, s => s.Color == 0xFFEEEEEEu);
        Assert.Equal(0, Statistics.PotionsCooked);
    }

    [Fact]
    public void Transmute_MixedStacks_WeightByQuantity()
    {
        var seen = new HashSet<Type>();
        for (var s = 1; s <= 200; s++)
        {
            LoadLevel(PotRoom);
            var heap = HeapWith(Pot, Many<Firebloom.Seed>(1), Many<Icecap.Seed>(2));
            var weights = heap.Items.Select(i => (float)i.Quantity()).ToArray();

            Random.Seed(s);
            var random = Random.Int(3) == 0;
            var index = random ? -1 : Random.Chances(weights);
            var expected = random ? null : ((Plant.Seed)heap.Items.ElementAt(index)).AlchemyClass;

            Random.Seed(s);
            var result = heap.Transmute();

            if (random)
            {
                Assert.IsType<Potion>(result, exactMatch: false);
            }
            else
            {
                Assert.IsType(expected, result);
                seen.Add(expected);
            }
        }

        Assert.Contains(typeof(PotionOfLiquidFlame), seen);
        Assert.Contains(typeof(PotionOfFrost), seen);
    }

    [Fact]
    public void Transmute_NullAlchemyClass_BrewsARandomPotion()
    {
        LoadLevel(PotRoom);
        var heap = HeapWith(Pot, Many<NullClassSeed>(3));
        Statistics.PotionsCooked = 0;
        Random.Seed(SeedWhere(false));

        var result = heap.Transmute();

        Assert.IsType<Potion>(result, exactMatch: false);
        Assert.Equal(1, Statistics.PotionsCooked);
        Assert.False(Dungeon.Level.Heaps.ContainsKey(Pot));
    }

    [Fact]
    public void AlchemyTransmute_DropsTheResultOnAnEmptySpNeighbour()
    {
        LoadLevel(PotRoom);
        PlaceHero(C(6, 3));
        HeapWith(Pot, Many<Firebloom.Seed>(3));

        Alchemy.Transmute(Pot);

        Assert.False(Dungeon.Level.Heaps.ContainsKey(Pot));
        var potions = Dungeon.Level.Heaps.Values
            .Where(h => h.Items.First.Value is Potion)
            .ToList();
        Assert.Single(potions);
        Assert.Contains(potions[0].Pos - Pot, Level.Neighbours8);
    }

    [Fact]
    public void AlchemyTransmute_NoHeap_DoesNothing()
    {
        LoadLevel(PotRoom);
        Alchemy.Transmute(Pot);
        Assert.Empty(Dungeon.Level.Heaps);
    }

    [Fact]
    public void AlchemyBlob_SeedMovesTheCell_AndVolumeStaysOne()
    {
        LoadLevel(PotRoom);
        var blob = Blob.Seed<Alchemy>(Pot, 1);
        Assert.Equal(1, blob.Cur[Pot]);

        var again = Blob.Seed<Alchemy>(C(5, 2), 1);

        Assert.Same(blob, again);
        Assert.Equal(0, blob.Cur[Pot]);
        Assert.Equal(1, blob.Cur[C(5, 2)]);
        Assert.Equal(1, blob.Volume);

        Tick(blob);
        Tick(blob);
        Assert.Equal(1, blob.Cur[C(5, 2)]);
        Assert.Equal(1, blob.Volume);
    }

    [Fact]
    public void AlchemyBlob_Bundle_RestoresTheCell()
    {
        var blob = new Alchemy();
        blob.Seed(C(4, 2), 1);
        var bundle = new Bundle();

        bundle.Put("alchemy", blob);
        var restored = (Alchemy)bundle.Get("alchemy");

        Assert.Equal(1, restored.Cur[C(4, 2)]);
        restored.Seed(C(2, 2), 1);
        Assert.Equal(0, restored.Cur[C(4, 2)]);
        Assert.Equal(1, restored.Cur[C(2, 2)]);
    }

    [Fact]
    public void Press_PotWithACharacter_DoesNothing()
    {
        LoadLevel(PotRoom);
        var hero = PlaceHero(C(1, 1));
        var heap = HeapWith(Pot, Many<Firebloom.Seed>(3));

        Dungeon.Level.Press(Pot, hero);

        Assert.Same(heap, Dungeon.Level.Heaps[Pot]);
        Assert.Equal(3, heap.Items.First.Value.Quantity());
        Assert.Equal(0, Statistics.PotionsCooked);
    }

    [Fact]
    public void Cook_PotInView_OpensTheSeedSelection()
    {
        LoadLevel(PotRoom);
        var hero = PlaceHero(C(1, 2));
        Assert.True(Dungeon.Visible[Pot]);
        var ready = View.ReadyCount;

        var again = hero.Handle(Pot);

        Assert.False(again);
        Assert.Equal(ready + 1, View.ReadyCount);
        Assert.Equal(BagMode.Seed, View.ItemSelections[^1].Mode);
        Assert.Equal("Select a seed to throw", View.ItemSelections[^1].Title);
        Assert.Equal(AlchemyPot.TxtSelectSeed, View.ItemSelections[^1].Title);
    }

    [Fact]
    public void Cook_PotOutOfView_WalksTowardsIt()
    {
        LoadLevel(HiddenPot);
        Array.Fill(Dungeon.Level.Visited, true); // the walk only follows cells the hero has seen
        var hero = PlaceHero(C(1, 1));
        Assert.False(Dungeon.Visible[C(4, 2)]);
        int start = hero.Pos;

        bool again = hero.Handle(C(4, 2));

        Assert.True(again);
        Assert.NotEqual(start, hero.Pos);
        Assert.Empty(View.ItemSelections);
    }

    [Fact]
    public void Cook_ChoosingNothing_DoesNothing()
    {
        LoadLevel(PotRoom);
        var hero = PlaceHero(C(1, 2));
        hero.Handle(Pot);

        View.ChooseItem(null);

        Assert.Empty(View.Missiles);
    }

    [Fact]
    public void Cook_ChoosingASeed_CastsItAtThePot()
    {
        LoadLevel(PotRoom);
        var hero = PlaceHero(C(1, 2));
        View.HoldMissiles = true;
        var seed = new Firebloom.Seed();
        seed.Collect();
        hero.Handle(Pot);

        View.ChooseItem(seed);

        Assert.Equal((hero.Pos, Pot, (Item)seed), View.Missiles[^1]);
    }

    [Fact]
    public void DropThirdSeedOnPot_NeverTouchesTheDestroyedHeapView()
    {
        LoadLevel(PotRoom);
        PlaceHero(C(6, 3));
        ThrowAt(new Firebloom.Seed(), Pot);
        ThrowAt(new Firebloom.Seed(), Pot);
        var view = (FakeHeapView)Dungeon.Level.Heaps[Pot].Sprite;
        Assert.False(view.Killed);

        ThrowAt(new Firebloom.Seed(), Pot);

        Assert.True(view.Killed);
        Assert.Equal(view.DropsAtKill, view.Drops);
        Assert.Equal(view.LinksAtKill, view.Links.Count);
        Assert.Equal(1, Statistics.PotionsCooked);
    }

    [Fact]
    public void ThreeCasts_BrewOnePotionBesideThePot()
    {
        LoadLevel(PotRoom);
        var hero = PlaceHero(C(1, 2));
        View.HoldMissiles = true;
        var seeds = new Firebloom.Seed();
        seeds.Quantity(3);
        seeds.Collect();

        for (var i = 0; i < 3; i++)
        {
            Assert.False(hero.Handle(Pot));
            View.ChooseItem(seeds);
            View.CompleteMissile();
        }

        Assert.Equal(3, View.Missiles.Count);
        Assert.Equal(1, Statistics.PotionsCooked);
        Assert.False(Dungeon.Level.Heaps.ContainsKey(Pot));
        Assert.Single(Dungeon.Level.Heaps.Values.Where(h => h.Items.First.Value is Potion));
    }
}