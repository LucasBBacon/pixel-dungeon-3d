using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Tests.Actors.Hero;

public class HeroItemTests : DungeonFixture
{
    private Core.Actors.Hero.Hero PlaceIn(string[] rows, int x, int y)
    {
        LoadLevel(rows);
        var hero = PlaceHero(TestLevel.At(x, y));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static readonly string[] Corridor =
    [
        "#####",
        "#...#",
        "#####"
    ];

    private static Heap DropAt(int cell, Item item, HeapType type)
    {
        var heap = Dungeon.Level.Drop(item, cell);
        heap.Type = type;
        return heap;
    }

    // region Pick up

    [Fact]
    public void Handle_OnAHeapUnderfoot_PicksUpTheTopItemAndLogsIt()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var item = new TestItem();
        DropAt(hero.Pos, item, HeapType.Heap);

        hero.Handle(hero.Pos);

        Assert.Contains(item, hero.Belongings.Backpack.Items);
        Assert.True(View.Logged("You now have test item"));
        Assert.False(Dungeon.Level.Heaps.ContainsKey(hero.Pos));
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Handle_OnAHeadUnderfoot_SpendsOneTurn()
    {
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceIn(Corridor, 1, 1);
        DropAt(hero.Pos, new TestItem(), HeapType.Heap);

        hero.Handle(hero.Pos);

        Assert.Equal(1f, hero.TimeLeft);
    }

    [Fact]
    public void Handle_OnHeapWithMoreUnderneath_SaysThereIsSomethingElse()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        DropAt(hero.Pos, new TestItem(), HeapType.Heap);
        DropAt(hero.Pos, new StackItem(1), HeapType.Heap);

        hero.Handle(hero.Pos);

        Assert.True(View.Logged("There is something else here"));
        Assert.Single(Dungeon.Level.Heaps[hero.Pos].Items);
    }

    [Fact]
    public void Handle_OnGold_AddsItToTheTotal()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        DropAt(hero.Pos, new Gold(40), HeapType.Heap);

        hero.Handle(hero.Pos);

        Assert.Equal(40, Dungeon.Gold);
        Assert.True(View.Logged("You now have gold"));
    }

    [Fact]
    public void Handle_OnHeapWIthAFullPack_PutsTheItemBackAndMakesTheHeroReady()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        for (var i = 0; i < Belongings.BackpackSize; i++)
        {
            new TestItem().Collect();
        }

        var item = new StackItem(1);
        DropAt(hero.Pos, item, HeapType.Heap);

        hero.Handle(hero.Pos);

        var heap = Dungeon.Level.Heaps[hero.Pos];
        Assert.Same(item, heap.Peek());
        Assert.Equal(1, ((FakeHeapView)heap.Sprite).Drops);
        Assert.True(hero.Ready);
    }

    // region Chests

    [Fact]
    public void Handle_OnAnAdjacentChest_OperatesAndOpensItWhenTheAnimationEnds()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var chest = TestLevel.At(2, 1);
        var heap = DropAt(chest, new Gold(5), HeapType.Chest);

        hero.Handle(chest);

        Assert.Equal(new[] { chest }, ((FakeCharView)hero.Sprite).Operations);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndUnlock);
        Assert.Equal(HeapType.Chest, heap.Type);

        hero.OnOperateComplete();

        Assert.Equal(HeapType.Heap, heap.Type);
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Handle_OnAnAdjacentChest_SpendsTimeTOUnlockBeforeOperateCompletes()
    {
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceIn(Corridor, 1, 1);
        var chest = TestLevel.At(2, 1);
        DropAt(chest, new Gold(5), HeapType.Chest);

        hero.Handle(chest);

        Assert.Equal(1f, hero.TimeLeft);
    }

    [Theory]
    [InlineData(HeapType.LockedChest)]
    [InlineData(HeapType.CrystalChest)]
    public void Handle_OnALockedChestWithoutAGoldenKey_REfusesAndMakesTheHeroReady(HeapType type)
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var chest = TestLevel.At(2, 1);
        DropAt(chest, new Gold(5), type);

        hero.Handle(chest);

        Assert.True(View.Logged("This chest is locked and you don't have matching key"));
        Assert.Empty(((FakeCharView)hero.Sprite).Operations);
        Assert.True(hero.Ready);
    }

    [Fact]
    public void Handle_OnALockedChestWithAGoldenKeyFromThisDepth_UsesUpTheKey()
    {
        Dungeon.Depth = 2;
        var hero = PlaceIn(Corridor, 1, 1);
        var chest = TestLevel.At(2, 1);
        var heap = DropAt(chest, new Gold(5), HeapType.LockedChest);
        var key = new GoldenKey();
        key.Collect();

        hero.Handle(chest);
        hero.OnOperateComplete();

        Assert.DoesNotContain(key, hero.Belongings.Backpack.Items);
        Assert.Equal(HeapType.Heap, heap.Type);
    }

    [Fact]
    public void OpeningSkeletalRemains_PlaysBonesNotTheLock()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var remains = TestLevel.At(2, 1);
        DropAt(remains, new TestItem(), HeapType.Skeleton);

        hero.Handle(remains);
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndUnlock);

        hero.OnOperateComplete();
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndBones);
    }

    [Fact]
    public void OpeningATomb_RumblesAndShakes()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var tomb = TestLevel.At(2, 1);
        DropAt(tomb, new TestItem(), HeapType.Tomb);

        hero.Handle(tomb);

        Assert.Contains(View.Sounds, s => s.Id == Assets.SndTomb);
        Assert.Equal((1f, 0.5), View.Shakes[0]);
    }

    // region Doors

    private static readonly string[] LockedDoor =
    [
        "#####",
        "#.L.#",
        "#####"
    ];

    [Fact]
    public void Handle_OnALockedDoorWIthAnIronKeyFromThisDepth_UnlocksItWhenTheAnimationEnds()
    {
        Dungeon.Depth = 2;
        var hero = PlaceIn(LockedDoor, 1, 1);
        var door = TestLevel.At(2, 1);
        var key = new IronKey();
        key.Collect();

        hero.Handle(door);

        Assert.Equal(new[] { door }, ((FakeCharView)hero.Sprite).Operations);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndUnlock);

        hero.OnOperateComplete();

        Assert.Equal(Terrain.Door, Dungeon.Level.Map[door]);
        Assert.Contains(door, View.UpdatedCells);
        Assert.DoesNotContain(key, hero.Belongings.Backpack.Items);
        Assert.Equal(0, IronKey.CurDepthQuantity);
    }

    [Fact]
    public void Handle_OnALockedDoorWithAKeyFromAnotherDepth_Refuses()
    {
        Dungeon.Depth = 2;
        var hero = PlaceIn(LockedDoor, 1, 1);
        new IronKey { Depth = 1 }.Collect();

        hero.Handle(TestLevel.At(2, 1));

        Assert.True(View.Logged("You don't have a matching key"));
        Assert.Equal(Terrain.LockedDoor, Dungeon.Level.Map[TestLevel.At(2, 1)]);
        Assert.True(hero.Ready);
    }

    [Fact]
    public void Handle_OnALockedExitWithASkeletonKey_UnlocksTheExit()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var exit = TestLevel.At(2, 1);
        Level.Set(exit, Terrain.LockedExit);
        new SkeletonKey().Collect();

        hero.Handle(exit);
        hero.OnOperateComplete();

        Assert.Equal(Terrain.UnlockedExit, Dungeon.Level.Map[exit]);
    }

    // region Death

    [Fact]
    public void Death_ScattersTheBackpackOverFreeNeighbours_OneItemPerCell()
    {
        var hero = PlaceIn([
            "#####",
            "#...#",
            "#...#",
            "#...#",
            "#####"
        ], 2, 2);
        var occupied = new[]
        {
            TestLevel.At(1, 1),
            TestLevel.At(2, 1),
            TestLevel.At(3, 1),
            TestLevel.At(1, 2),
            TestLevel.At(3, 2),
        };
        foreach (var cell in occupied)
        {
            DropAt(cell, new TestItem(), HeapType.Heap);
        }

        for (var i = 0; i < 5; i++)
        {
            new TestItem().Collect();
        }

        hero.Damage(hero.HP, new Rat());

        Assert.False(hero.IsAlive());
        foreach (var cell in occupied)
        {
            Assert.Single(Dungeon.Level.Heaps[cell].Items);
        }

        foreach (var cell in new[] { TestLevel.At(1, 3), TestLevel.At(2, 3), TestLevel.At(3, 3) })
        {
            var heap = Dungeon.Level.Heaps[cell];
            Assert.Single(heap.Items);
            Assert.Equal(new[] { hero.Pos }, ((FakeHeapView)heap.Sprite).DropsFrom);
        }

        Assert.Equal(1, View.GameOvers);
    }

    // region Attack indicator

    [Fact]
    public void Act_TargetsAnAdjacentVisibleEnemy_AndForgetsItWhenNoneIsAdjacent()
    {
        var hero = PlaceIn([
            "######",
            "#....#",
            "######"
        ], 1, 1);
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);

        Actor.Process();
        Assert.Same(rat, AttackIndicator.LastTarget);

        rat.Pos = TestLevel.At(4, 1);
        AttackIndicator.UpdateState();
        Assert.Null(AttackIndicator.LastTarget);
    }

    [Fact]
    public void OnAttackComplete_MakesTheEnemyTheAttackTarget()
    {
        var hero = PlaceIn(Corridor, 1, 1);
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();
        hero.Handle(rat.Pos); // first sight of the rat interrupts

        hero.Handle(rat.Pos);
        hero.OnAttackComplete();

        Assert.Same(rat, AttackIndicator.LastTarget);
    }

    // region Items Down a Chasm
    [Fact]
    public void LandDroppedItems_LandsThisDepthsItemsAndClearsTheEntry()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        Dungeon.Depth = 4;
        var first = new TestItem();
        var second = new TestItem();
        Dungeon.DroppedItems[4] = [first, second];

        GameScene.LandDroppedItems();

        var landed = Dungeon.Level.Heaps.Values
            .SelectMany(h => h.Items)
            .ToList();
        Assert.Contains(first, landed);
        Assert.Contains(second, landed);
        Assert.False(Dungeon.DroppedItems.ContainsKey(4));
    }
}