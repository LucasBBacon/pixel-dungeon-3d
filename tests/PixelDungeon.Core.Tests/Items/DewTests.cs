using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class DewTests : DungeonFixture
{
    private Hero PlaceHurtHero(int hp)
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        hero.HP = hp;
        return hero;
    }

    private static DewVial VialWith(int drops)
    {
        var vial = new DewVial();
        vial.Collect();
        if (drops > 0)
        {
            var dew = new Dewdrop();
            dew.Quantity(drops);
            vial.CollectDew(dew);
        }

        return vial;
    }

    [Fact]
    public void DewDrop_OnAHurtHero_HealsONeOnTheFirstFiveDepths()
    {
        Dungeon.Depth = 1;
        var hero = PlaceHurtHero(10);

        Assert.True(new Dewdrop().DoPickUp(hero));

        Assert.Equal(11, hero.HP);
        Assert.Contains((StatusColor.Positive, "+1HP"), ((FakeCharView)hero.Sprite).Statuses);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndDewdrop);
        Assert.False(hero.Ready);
    }

    [Fact]
    public void DewDrop_Stack_HealsValueTimeSQuantity_CappedAtTheMissingHp()
    {
        Dungeon.Depth = 6; // value 2
        var hero = PlaceHurtHero(15);
        var dew = new Dewdrop();
        dew.Quantity(3);

        dew.DoPickUp(hero);

        Assert.Equal(20, hero.HP); // 6 would over-heal, capped at HT - HP = 5
    }

    [Fact]
    public void Dewdrop_ForAHuntress_HealsOneMore()
    {
        Dungeon.Depth = 1;
        var hero = PlaceHurtHero(10);
        hero.HeroClass = HeroClass.Huntress;

        new Dewdrop().DoPickUp(hero);

        Assert.Equal(12, hero.HP);
    }

    [Fact]
    public void Dewdrop_AtFullHpWithAVialThatIsNotFull_GoesIntoTheVial()
    {
        var hero = PlaceHurtHero(20);
        var vial = VialWith(0);

        new Dewdrop().DoPickUp(hero);

        Assert.Equal(20, hero.HP);
        Assert.Equal("1/10", vial.Status());
        Assert.True(View.Logged("You collected a dewdrop into your dew vial."));
    }

    [Fact]
    public void CollectDew_CapsAtTen_AndSaysSo_AndGlows()
    {
        var vial = VialWith(12);

        Assert.True(vial.IsFull());
        Assert.Equal("10/10", vial.Status());
        Assert.True(View.Logged("Your dew vial is full!"));
        Assert.NotNull(vial.Glowing());
        Assert.Equal("dew vial (10/10)", vial.ToString());
    }

    [Fact]
    public void Drink_AtVolumeFive_HealsNineAndEndsTheTurnThroughOperate()
    {
        var hero = PlaceHurtHero(5);
        var vial = VialWith(5);

        vial.Execute(hero);

        Assert.Equal(14, hero.HP); // ceil(5^log10(20) / 20 * 20) = 9
        Assert.Equal("0/10", vial.Status());
        Assert.Equal(new[] { hero.Pos }, ((FakeCharView)hero.Sprite).Operations);
        Assert.False(hero.Ready);
    }

    [Fact]
    public void Drink_AnEmptyVial_Warns()
    {
        var hero = PlaceHurtHero(5);
        var vial = VialWith(0);

        vial.Execute(hero, "DRINK");

        Assert.Equal(5, hero.HP);
        Assert.True(View.Logged("Your dew vial is empty!"));
    }

    [Fact]
    public void Actions_ListDrinkOnlyWhenThereIsDew()
    {
        Assert.DoesNotContain("DRINK", VialWith(0).Actions(Dungeon.Hero));
        Assert.Contains("DRINK", VialWith(1).Actions(Dungeon.Hero));
    }

    [Fact]
    public void AutoDrink_DoesNothingUnlessTheVialIsFull()
    {
        var hero = PlaceHurtHero(5);
        VialWith(9);

        DewVial.AutoDrink(hero);

        Assert.Equal(5, hero.HP);
    }

    [Fact]
    public void Die_WithAFullVial_DrinksItAndSurvives()
    {
        var hero = PlaceHurtHero(3);
        VialWith(10);

        hero.Damage(8, new Rat()); // HP -5 Char.Damage reads src.GetType(), so the source cannot be null

        // float edge, pinned: 10^log10(20) is 20.000000000000004 in double arithmetic
        // heal = ceil(20.000000000000004) = 21
        // at HP -5 the HT - HP cap is 25, doesn't hide it
        // hero ends at 16, not 15
        // Whether the Java's Math.pow agrees in the last ulp is unverified
        Assert.True(hero.IsAlive());
        Assert.Equal(16, hero.HP);
        Assert.True(View.Logged("The dew vial was emptied to heal your wounds."));
        Assert.Equal(0, View.GameOvers);
    }

    [Fact]
    public void Die_WithAVialThatIsNotFull_StillDies()
    {
        var hero = PlaceHurtHero(3);
        VialWith(9);

        hero.Damage(8, new Rat());

        Assert.False(hero.IsAlive());
        Assert.Equal(1, View.GameOvers);
    }

    [Fact]
    public void DewVial_Bundle_RoundTripsTheVolume()
    {
        var vial = new DewVial();
        var dew = new Dewdrop();
        dew.Quantity(4);
        vial.CollectDew(dew);

        var bundle = new Bundle();
        bundle.Put("vial", vial);

        Assert.Equal("4/10", ((DewVial)bundle.Get("vial")).Status());
    }

    [Fact]
    public void TramplingHighGrass_DropsDewOnItsRoll_AndNeverLeavesAnEmptyHeap()
    {
        var drops = 0;
        for (var seed = 1; seed <= 60; seed++)
        {
            Random.Seed(seed);
            LoadLevel(
                "###",
                "#\"#",
                "###"
            );
            var cell = TestLevel.At(1, 1);

            HighGrass.Trample(Dungeon.Level, cell, null);

            if (Dungeon.Level.Heaps.TryGetValue(cell, out var heap))
            {
                Assert.IsType<Dewdrop>(heap.Peek());
                Assert.Equal(1, ((FakeHeapView)heap.Sprite).Drops);
                drops++;
            }
        }

        Assert.InRange(drops, 1, 59); // a 1 in 6 roll across 60 seeds
    }

    [Fact]
    public void SewerLevel_AtDepthThreeWithTheFlagSet_AlwaysSpawnsTheVialAndClearsTheFlag()
    {
        Random.Seed(3);
        Dungeon.Depth = 3; // Random.Int(4 - 3) is always 0
        Dungeon.DewVial = true;

        var level = new SewerLevel();
        level.Create();

        Assert.False(Dungeon.DewVial);
        Assert.Contains(level.Heaps.Values, h => h.Items.Any(i => i is DewVial));
    }

    [Fact]
    public void SewerLevel_WithTheFlagCleared_SpawnsNoVial()
    {
        Random.Seed(3);
        Dungeon.Depth = 3;
        Dungeon.DewVial = false;

        var level = new SewerLevel();
        level.Create();

        Assert.DoesNotContain(level.Heaps.Values, h => h.Items.Any(i => i is DewVial));
    }
}