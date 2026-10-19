using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Tests.Items;

public class ItemActionTests : DungeonFixture
{
    private void LoadCorridor() =>
        LoadLevel(
            "#######",
            "#.....#",
            "#######"
        );

    [Fact]
    public void Actions_ForAPlainItem_AreDropAndThrow_AndGoldHasNone()
    {
        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow }, new TestItem().Actions(Dungeon.Hero));
        Assert.Empty(new Gold(5).Actions(Dungeon.Hero));
    }

    [Fact]
    public void ExecuteDrop_DropsTheWholeStackAtTheHeroCell_WithAHopFromThere()
    {
        LoadCorridor();
        var hero = PlaceHero(TestLevel.At(2, 1));
        var stack = new StackItem(3);
        stack.Collect();

        stack.Execute(hero, Item.AcDrop);

        Assert.Empty(hero.Belongings.Backpack.Items);
        var heap = Dungeon.Level.Heaps[hero.Pos];
        Assert.Same(stack, heap.Peek());
        Assert.Equal(3, stack.Quantity());
        Assert.Equal(new[] { hero.Pos }, ((FakeHeapView)heap.Sprite).DropsFrom);
        Assert.False(hero.Ready);
    }

    [Fact]
    public void ExecuteDrop_SpendsTimeToDrop()
    {
        LoadCorridor();
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(2, 1));
        var item = new TestItem();
        item.Collect();

        item.Execute(hero, Item.AcDrop);

        Assert.Equal(0.5f, hero.TimeLeft);
    }

    [Fact]
    public void Throw_AtACell_ZapsFliesAndLandsOneItemWhereTheBoltStops()
    {
        LoadCorridor();
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var stack = new StackItem(2);
        stack.Collect();

        stack.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.Equal(new[] { TestLevel.At(4, 1) }, view.Zaps);
        Assert.Equal((TestLevel.At(1, 1), TestLevel.At(4, 1), (Item)stack), View.Missiles[0]);
        Assert.Equal(1, stack.Quantity());
        var heap = Dungeon.Level.Heaps[TestLevel.At(4, 1)];
        Assert.IsType<StackItem>(heap.Peek());
        Assert.NotSame(stack, heap.Peek());
        Assert.Equal(new[] { TestLevel.At(4, 1) }, ((FakeHeapView)heap.Sprite).DropsFrom);
    }

    [Fact]
    public void Throw_WhileTheMissileFiles_HoldTheHeroAndTheScheduler()
    {
        LoadCorridor();
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var item = new TestItem();
        item.Collect();
        Actor.Process(); // hero acts, becomes ready, stays current
        Assert.True(hero.Ready);

        View.HoldMissiles = true;
        item.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(3, 1));

        Assert.False(hero.Ready);
        Actor.Process(); // nothing may act while missile flies
        Assert.False(hero.Ready);
        Assert.Empty(Dungeon.Level.Heaps);
        Assert.Contains(item, hero.Belongings.Backpack.Items);

        View.CompleteMissile();

        Assert.DoesNotContain(item, hero.Belongings.Backpack.Items);
        Assert.Same(item, Dungeon.Level.Heaps[TestLevel.At(3, 1)].Peek());
        Actor.Process(); // the throw's turn is spent, hero acts again
        Assert.True(hero.Ready);
    }

    [Fact]
    public void Throw_Completed_SpendsTimeToThrow()
    {
        LoadCorridor();
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var item = new TestItem();
        item.Collect();
        Actor.Process();

        View.HoldMissiles = true;
        item.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(3, 1));
        View.CompleteMissile();

        Assert.Equal(1f, hero.TimeLeft);
    }

    [Fact]
    public void Throw_Cancelled_KeepsTheItemAndLaunchesNothing()
    {
        LoadCorridor();
        var hero = PlaceHero(TestLevel.At(1, 1));
        var item = new TestItem();
        item.Collect();

        item.Execute(hero, Item.AcThrow);
        View.Answer(null);

        Assert.Empty(View.Missiles);
        Assert.Contains(item, hero.Belongings.Backpack.Items);
    }

    [Fact]
    public void Throw_Cancelled_SpendsNoTime()
    {
        LoadCorridor();
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        var item = new TestItem();
        item.Collect();
        var before = hero.TimeLeft;

        item.Execute(hero, Item.AcThrow);
        View.Answer(null);

        Assert.Equal(before, hero.TimeLeft);
    }

    [Fact]
    public void Throw_AtMob_StopsOnItAndRemembersItAsTheQuickSlotTarget()
    {
        LoadCorridor();
        var hero = PlaceHero(TestLevel.At(1, 1));
        var rat = new Rat { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        var item = new TestItem();
        item.Collect();

        item.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(5, 1));

        Assert.Same(rat, QuickSlot.LastTarget);
        Assert.Same(item, Dungeon.Level.Heaps[TestLevel.At(3, 1)].Peek());
    }

    [Fact]
    public void Bag_DefaultActioOpensIt_ButOpenIsNotListed()
    {
        var ring = new Keyring();

        ring.Execute(Dungeon.Hero);

        Assert.Same(ring, Assert.Single(View.ShownBags));
        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow }, ring.Actions(Dungeon.Hero));
    }
}