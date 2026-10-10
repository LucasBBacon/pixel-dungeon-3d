using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class ContainerTests : DungeonFixture
{
    private static Bag Backpack => Dungeon.Hero.Belongings.Backpack;

    [Fact]
    public void Backpack_IsANineteenSlotBagOwnedByTheHero()
    {
        Assert.Equal(19, Belongings.BackpackSize);
        Assert.Equal(Belongings.BackpackSize, Backpack.Size);
        Assert.Equal("backpack", Backpack.Name());
        Assert.Same(Dungeon.Hero, Backpack.Owner);
    }

    [Fact]
    public void Collect_IntoTheBackpack_AddsTheItemAndRefreshesTheQuickSlots()
    {
        var item = new TestItem();
        Assert.True(item.Collect());
        Assert.Same(item, Assert.Single(Backpack.Items));
        Assert.Equal(1, View.QuickSlotRefreshes);
    }

    [Fact]
    public void Collect_ASecondStackOfTheSameClass_MergesIntoTheFirst()
    {
        var first = new StackItem(2);
        var second = new StackItem(3);
        first.Collect();
        Assert.True(second.Collect());
        Assert.Same(first, Assert.Single(Backpack.Items));
        Assert.Equal(5, first.Quantity());
    }

    [Fact]
    public void Collect_AnItemAlreadyInside_ReturnsTrueWithoutDuplicating()
    {
        var item = new TestItem();
        item.Collect();
        Assert.True(item.Collect());
        Assert.Single(Backpack.Items);
    }

    [Fact]
    public void Collect_IntoAFullBackpack_LogsAndRefuses()
    {
        for (var i = 0; i < Belongings.BackpackSize; i++)
        {
            Assert.True(new TestItem().Collect());
        }

        Assert.False(new TestItem().Collect());
        Assert.Equal(19, Backpack.Items.Count);
        Assert.True(View.Logged("Your pack is too full for the test item"));
    }

    [Fact]
    public void Collect_AKey_GoesIntoAHeldKeyRing()
    {
        var ring = new Keyring();
        ring.Collect();
        var key = new Key();

        Assert.True(key.Collect());

        Assert.Same(ring, Assert.Single(Backpack.Items));
        Assert.Same(key, Assert.Single(ring.Items));
    }

    [Fact]
    public void Collect_AKeyRing_PullsInKeyAlreadyInTheBackpack()
    {
        var key = new Key();
        var other = new TestItem();
        key.Collect();
        other.Collect();

        var ring = new Keyring();
        ring.Collect();

        Assert.Contains(key, ring.Items);
        Assert.DoesNotContain(key, Backpack.Items);
        Assert.Contains(other, Backpack.Items);
        Assert.Same(Dungeon.Hero, ring.Owner);
    }

    [Fact]
    public void Collect_SortsByCategory_MatchingSuperclassesByName()
    {
        var misc = new TestItem();
        var scroll = new Scroll();
        misc.Collect();
        scroll.Collect();

        Assert.Equal(new Item[] { scroll, misc }, Backpack.Items);
        Assert.Equal((int)Generator.Category.Scroll, Generator.Order(scroll));
        Assert.Equal((int)Generator.Category.Misc, Generator.Order(misc));
    }

    [Fact]
    public void Collect_KeepsEqualCategoryItemsInCollectionOrder()
    {
        // nineteen MISC items
        // List.Sort leaves insertion sort above 16 elements and may
        // reorder equal keys; Java's Collections.sort is stable and never does
        var collected = new List<Item>();
        for (var i = 0; i < Belongings.BackpackSize; i++)
        {
            var item = new TestItem();
            item.Collect();
            collected.Add(item);
        }

        Assert.Equal(collected, Backpack.Items);
    }

    [Fact]
    public void Detach_FromAStackOfThree_ReturnsAFreshSingleAndLeavesTwo()
    {
        var stack = new StackItem(3);
        stack.Collect();

        var detached = stack.Detach(Backpack);

        Assert.IsType<StackItem>(detached);
        Assert.NotSame(stack, detached);
        Assert.Equal(1, detached.Quantity());
        Assert.Equal(2, stack.Quantity());
        Assert.Contains(stack, Backpack.Items);
    }

    [Fact]
    public void Detach_TehLAstOne_RemovesTheItemItself()
    {
        var stack = new StackItem(1);
        stack.Collect();

        Assert.Same(stack, stack.Detach(Backpack));
        Assert.Empty(Backpack.Items);
    }

    [Fact]
    public void Detach_AnEmptyStack_ReturnsNull()
    {
        Assert.Null(new StackItem(0).Detach(Backpack));
    }

    [Fact]
    public void DetachAll_FindsAnItemInsideANestedBag()
    {
        var ring = new Keyring();
        ring.Collect();
        var key = new Key();
        key.Collect();

        Assert.Same(key, key.DetachAll(Backpack));
        Assert.Empty(ring.Items);
        Assert.Contains(ring, Backpack.Items);
    }

    [Fact]
    public void DetachAll_ABag_ClearsItsOwner()
    {
        var ring = new Keyring();
        ring.Collect();

        ring.DetachAll(Backpack);

        Assert.Null(ring.Owner);
    }

    [Fact]
    public void Bag_Enumerates_ANestedBagsItemRightAfterIt()
    {
        var first = new TestItem();
        var ring = new Keyring();
        var last = new TestItem();
        var key = new Key();
        first.Collect();
        ring.Collect();
        last.Collect();
        key.Collect();

        Assert.Equal(new Item[] { first, ring, key, last }, Backpack.ToList());
        Assert.True(Backpack.Contains(key));
    }

    [Fact]
    public void Bag_Bundle_RoundTripsNestedContents()
    {
        var bag = new Bag { Size = 5 };
        new Keyring().Collect(bag);
        new Key { Depth = 4 }.Collect(bag); // into the ring
        new StackItem(3).Collect(bag);

        var bundle = new Bundle();
        bag.StoreInBundle(bundle);
        // a bag's size is not bundled
        // belongings restores into its existing 19-slot backpack
        var restored = new Bag { Size = 5 };
        restored.RestoreFromBundle(bundle);

        var ring = Assert.Single(restored.Items.OfType<Keyring>());
        Assert.Equal(4, Assert.IsType<Key>(Assert.Single(ring.Items)).Depth);
        Assert.Equal(3, Assert.Single(restored.Items.OfType<StackItem>()).Quantity());
    }

    [Fact]
    public void Key_TakesTheCurrentDepth_AndIsNeverUpgradable()
    {
        Dungeon.Depth = 7;
        var key = new Key();
        Assert.Equal(7, key.Depth);
        Assert.False(key.IsUpgradable());
        Assert.True(key.IsIdentified());
        Assert.Equal("7\u007F", key.Status());
    }

    [Fact]
    public void DoPickUp_CollectsSpendsAndReportsThePickUp()
    {
        LoadLevel("###", "#.#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var item = new TestItem();

        Assert.True(item.DoPickUp(hero));

        Assert.Contains(item, Backpack.Items);
        Assert.Same(item, View.PickedUp[0]);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndItem);
        Assert.False(hero.Ready);
    }

    [Fact]
    public void DoPickUp_WithAFullPack_ReturnsFalseAndReportsNothing()
    {
        LoadLevel("###", "#.#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));
        for (var i = 0; i < Belongings.BackpackSize; i++)
        {
            new TestItem().Collect();
        }

        Assert.False(new TestItem().DoPickUp(hero));
        Assert.Empty(View.PickedUp);
    }
}