using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class HeapTests : DungeonFixture
{
    private static readonly int Here = TestLevel.At(2, 1);

    private Hero LoadRoom()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#.x.#",
            "#####"
        );
        return PlaceHero(TestLevel.At(1, 1));
    }

    private static Heap DropAt(int cell, Item item, HeapType type = HeapType.Heap)
    {
        var heap = Dungeon.Level.Drop(item, cell);
        heap.Type = type;
        return heap;
    }

    private static FakeHeapView ViewOf(Heap heap) => (FakeHeapView)heap.Sprite;

    [Fact]
    public void DropAStackOfAClassAlreadyThere_Merges_AndMovesTheStackToTheFront()
    {
        LoadRoom();
        var stack = new StackItem(2);
        DropAt(Here, stack);
        var other = new TestItem();
        var heap = DropAt(Here, other);
        Assert.Same(other, heap.Peek());

        DropAt(Here, new StackItem(3));

        Assert.Equal([stack, other], heap.Items);
        Assert.Equal(5, stack.Quantity());
    }

    [Fact]
    public void Drop_ADewDrop_GoesToTheBack()
    {
        LoadRoom();
        var first = new TestItem();
        var dew = new Dewdrop();
        var last = new StackItem(1);
        DropAt(Here, first);
        DropAt(Here, dew);
        var heap = DropAt(Here, last);

        Assert.Equal([last, first, dew], heap.Items);
    }

    [Fact]
    public void Drop_RefreshesTheViewWithTheTopItem()
    {
        LoadRoom();
        DropAt(Here, new TestItem());
        var heap = DropAt(Here, new Gold(4));

        Assert.Equal(2, ViewOf(heap).Views.Count);
        Assert.Equal(ItemSpriteSheet.Gold, ViewOf(heap).Views[^1].Image);
    }

    [Fact]
    public void Drop_Null_IsIgnored()
    {
        var heap = new Heap();
        heap.Drop(null);
        Assert.True(heap.IsEmpty());
    }

    [Fact]
    public void PickUp_TakesTheTopItem_AndKeepsTheHeapWhileItemsRemain()
    {
        LoadRoom();
        var bottom = new TestItem();
        var top = new StackItem(1);
        DropAt(Here, bottom);
        var heap = DropAt(Here, top);

        Assert.Same(top, heap.PickUp());

        Assert.Same(heap, Dungeon.Level.Heaps[Here]);
        Assert.Same(bottom, heap.Peek());
        Assert.False(ViewOf(heap).Killed);
    }

    [Fact]
    public void PickUP_TheLastItem_DestroysTheHeap()
    {
        LoadRoom();
        var heap = DropAt(Here, new TestItem());

        heap.PickUp();

        Assert.False(Dungeon.Level.Heaps.ContainsKey(Here));
        Assert.True(ViewOf(heap).Killed);
        Assert.True(heap.IsEmpty());
    }

    [Fact]
    public void Open_AChest_BecomesAheapAndRelinksAndHopsItsView()
    {
        var hero = LoadRoom();
        var heap = DropAt(Here, new Gold(5), HeapType.Chest);

        heap.Open(hero);

        Assert.Equal(HeapType.Heap, heap.Type);
        Assert.Equal(1, ViewOf(heap).Relinks);
        Assert.Equal(1, ViewOf(heap).Drops);
    }

    [Fact]
    public void Open_AMimic_OpensAsAPlainChestUntilMimicsArePorted()
    {
        var hero = LoadRoom();
        var heap = DropAt(Here, new Gold(6), HeapType.Mimic);

        heap.Open(hero);

        Assert.Equal(HeapType.Heap, heap.Type);
        Assert.Same(heap, Dungeon.Level.Heaps[Here]);
    }

    [Fact]
    public void OPen_SkeletalRemainsWithoutACurse_JustOpen()
    {
        var hero = LoadRoom();
        var heap = DropAt(Here, new TestItem(), HeapType.Skeleton);

        heap.Open(hero);

        Assert.Equal(HeapType.Heap, heap.Type);
        Assert.Equal(20, hero.HP);
    }

    [Fact]
    public void Open_SkeletalRemainsWIthACursedItem_CostHalfTheHerosHp()
    {
        var hero = LoadRoom();
        var heap = DropAt(Here, new TestItem { Cursed = true }, HeapType.Skeleton);

        heap.Open(hero);

        Assert.Equal(10, hero.HP);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndCursed);
        Assert.Equal(HeapType.Heap, heap.Type);
    }

    [Theory]
    [InlineData(HeapType.Chest, ItemSpriteSheet.Chest)]
    [InlineData(HeapType.Mimic, ItemSpriteSheet.Chest)]
    [InlineData(HeapType.LockedChest, ItemSpriteSheet.LockedChest)]
    [InlineData(HeapType.CrystalChest, ItemSpriteSheet.CrystalChest)]
    [InlineData(HeapType.Tomb, ItemSpriteSheet.Tomb)]
    [InlineData(HeapType.Skeleton, ItemSpriteSheet.Bones)]
    [InlineData(HeapType.Hidden, ItemSpriteSheet.Hidden)]
    [InlineData(HeapType.Heap, ItemSpriteSheet.Gold)]
    public void Image_ShowsTheContainer_OrTheTopItemForAPlainHeap(HeapType type, int image)
    {
        var heap = new Heap { Type = type };
        heap.Drop(new Gold(5));
        Assert.Equal(image, heap.Image());
    }

    [Fact]
    public void Glowing_ComesFromTheTopItemOfAPlainHeapOnly()
    {
        var vial = new DewVial();
        vial.Fill();
        var heap = new Heap();
        heap.Drop(vial);
        Assert.NotNull(heap.Glowing());

        heap.Type = HeapType.Chest;
        Assert.Null(heap.Glowing());
    }

    [Fact]
    public void Replace_SwapsAnItemInPlace()
    {
        var a = new TestItem();
        var b = new TestItem();
        var c = new TestItem();
        var heap = new Heap();
        heap.Drop(c);
        heap.Drop(a);

        heap.Replace(c, b);

        Assert.Equal([a, b], heap.Items);
    }

    [Fact]
    public void Bundle_RoundTripPosTypeAndItems()
    {
        var heap = new Heap { Pos = 77, Type = HeapType.Chest };
        heap.Drop(new Gold(12));
        heap.Drop(new GoldenKey { Depth = 3 });

        var bundle = new Bundle();
        bundle.Put("heap", heap);
        var restored = (Heap)bundle.Get("heap");

        Assert.Equal(77, restored.Pos);
        Assert.Equal(HeapType.Chest, restored.Type);
        Assert.Equal(3, Assert.IsType<GoldenKey>(restored.Peek()).Depth);
        Assert.Equal(12, restored.Items.OfType<Gold>().Single().Quantity());
    }

    [Fact]
    public void LevelDrop_OntoALockedChest_SpillsToANeighbour()
    {
        LoadRoom();
        DropAt(Here, new Gold(1), HeapType.LockedChest);

        var spilled = Dungeon.Level.Drop(new TestItem(), Here);

        Assert.NotEqual(Here, spilled.Pos);
        Assert.True(Level.Adjacent(Here, spilled.Pos));
        Assert.Single(Dungeon.Level.Heaps[Here].Items);
    }

    [Fact]
    public void LevelDrop_IntoAChasm_FallsToTheNextDepthAndIsDiscarded()
    {
        LoadRoom();
        Dungeon.Depth = 2;
        var chasm = TestLevel.At(2, 2);
        var item = new TestItem();

        Dungeon.Level.Drop(item, chasm);

        Assert.False(Dungeon.Level.Heaps.ContainsKey(chasm));
        Assert.Single(View.DiscardedHeaps);
        Assert.Same(item, Assert.Single(Dungeon.DroppedItems[3]));
    }
}