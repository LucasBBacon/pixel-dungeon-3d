using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Ui;

public class QuickSlotTests : DungeonFixture
{
    [Fact]
    public void SaveAndRestore_ForAnItem_RestoresBothSlotsToThatInstance()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = item;
        QuickSlot.SecondaryValue = item;
        var bundle = new Bundle();
        bundle.Put("item", item);

        QuickSlot.PrimaryValue = null;
        QuickSlot.SecondaryValue = null;
        var restored = (TestItem)bundle.Get("item");

        Assert.Same(restored, QuickSlot.PrimaryValue);
        Assert.Same(restored, QuickSlot.SecondaryValue);
    }

    [Fact]
    public void Save_ForAnUnslottedItem_WritesNoKeys()
    {
        var bundle = new Bundle();
        QuickSlot.Save(bundle, new TestItem());
        Assert.Empty(bundle.Fields());
    }

    [Fact]
    public void Compress_movesTheSecondaryIntoAnEmptyPrimary()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = null;
        QuickSlot.SecondaryValue = item;
        QuickSlot.Compress();
        Assert.Same(item, QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }

    [Fact]
    public void Compress_ClearsADuplicateSecondary()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = item;
        QuickSlot.SecondaryValue = item;
        QuickSlot.Compress();
        Assert.Same(item, QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }

    [Fact]
    public void UpdateQuickslot_RefreshesOnlyWhenTheItemOrItsClassIsSlotted()
    {
        var stack = new StackItem(2);
        var single = new TestItem();

        stack.UpdateQuickslot();
        single.UpdateQuickslot();
        Assert.Equal(0, View.QuickSlotRefreshes);

        QuickSlot.PrimaryValue = typeof(StackItem);
        QuickSlot.SecondaryValue = single;
        stack.UpdateQuickslot();
        single.UpdateQuickslot();
        Assert.Equal(2, View.QuickSlotRefreshes);
    }

    [Fact]
    public void Target_IgnoresTheHero_AndRemembersAnyoneElse()
    {
        var rat = new Rat();
        QuickSlot.Target(new TestItem(), Dungeon.Hero);
        Assert.Null(QuickSlot.LastTarget);
        QuickSlot.Target(new TestItem(), rat);
        Assert.Same(rat, QuickSlot.LastTarget);
    }

    [Fact]
    public void DungeonInit_ClearsBothSlots()
    {
        QuickSlot.PrimaryValue = new TestItem();
        QuickSlot.SecondaryValue = typeof(StackItem);
        Dungeon.Init();
        Assert.Null(QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }
}