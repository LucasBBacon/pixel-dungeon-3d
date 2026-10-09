using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class ItemTests : DungeonFixture
{
    [Fact]
    public void ToString_PlainItem_IsItsName()
    {
        Assert.Equal("test item", new TestItem().ToString());
    }

    [Fact]
    public void ToString_Stack_AppendsTheQuantity()
    {
        Assert.Equal("pebble x3", new StackItem(3).ToString());
    }

    [Fact]
    public void ToString_KnownLevel_AppendsASignedLevel()
    {
        var item = new TestItem();
        item.Upgrade(2);
        Assert.Equal("test item", item.ToString()); // level unknown
        item.LevelKnown = true;
        Assert.Equal("test item+2", item.ToString());
    }

    [Fact]
    public void ToString_KnownNegativeLevelStack_ShowsBoth()
    {
        var item = new StackItem(2) { LevelKnown = true };
        item.Degrade();
        Assert.Equal("pebble-1 x2", item.ToString());
    }

    [Fact]
    public void Upgrade_ClearsAndRevealsTheCurse_AndRestoresDurability()
    {
        var item = new DurableItem { Cursed = true, CursedKnown = false };
        item.Upgrade();
        Assert.False(item.Cursed);
        Assert.True(item.CursedKnown);
        Assert.Equal(1, item.Level());
        Assert.Equal(12, item.Durability());
    }

    [Fact]
    public void Use_WarnsOnTheEleventhUse_AndBreaksOnTheTwelfth()
    {
        var item = new DurableItem { LevelKnown = true };
        item.Upgrade();

        for (var i = 0; i < 10; i++)
        {
            item.Use();
        }

        Assert.False(View.Logged("going to break soon"));

        item.Use();
        Assert.True(View.Logged("Because of frequent use, your durable thing is going to break soon."));
        Assert.False(item.IsBroken());

        item.Use();
        Assert.True(item.IsBroken());
        Assert.True(View.Logged("Because of frequent use, your durable thing has broken."));
        Assert.Equal(0, item.EffectiveLevel());
    }

    [Fact]
    public void Use_OnALevelZeroItem_DoesNothing()
    {
        var item = new DurableItem();
        item.Use();
        Assert.Equal(12, item.Durability());
    }

    [Fact]
    public void ConsiderState_KnownCurse_HalvesThePrice()
    {
        var item = new TestItem { Cursed = true, CursedKnown = true };
        Assert.Equal(5, item.ConsiderState(10));
    }

    [Fact]
    public void ConsiderState_BrokenUpgradeItem_HalvesAgain()
    {
        var item = new TestItem { LevelKnown = true }; // MaxDurability 1, 1 use breaks it
        item.Upgrade(2);
        item.Use();
        Assert.True(item.IsBroken());
        Assert.Equal(15, item.ConsiderState(10));
    }

    [Fact]
    public void ConsiderState_KnownNegativeLevel_DividesByOneMinusLevel()
    {
        var item = new TestItem { LevelKnown = true };
        item.Degrade(2);
        Assert.Equal(3, item.ConsiderState(10));
    }

    [Fact]
    public void ConsiderState_NeverGoesBelowOne()
    {
        Assert.Equal(1, new TestItem().ConsiderState(0));
    }

    [Fact]
    public void Identify_RevealsIsLevelAndCurse()
    {
        var item = new TestItem();
        Assert.False(item.IsIdentified());
        Assert.Same(item, item.Identify());
        Assert.True(item.IsIdentified());
    }

    [Fact]
    public void VisiblyUpgraded_IsZeroUntilTheLevelIsKnown()
    {
        var item = new TestItem();
        item.Upgrade();
        Assert.Equal(0, item.VisiblyUpgraded());
        item.LevelKnown = true;
        Assert.Equal(1, item.VisiblyUpgraded());
    }

    [Fact]
    public void Status_ShowsTheQuantityOnlyWhenNotOne()
    {
        Assert.Null(new TestItem().Status());
        Assert.Equal("3", new StackItem(3).Status());
    }

    [Fact]
    public void Virtual_BuildsAnItemOfQuantifyZero()
    {
        var item = Item.Virtual(typeof(StackItem));
        Assert.IsType<StackItem>(item);
        Assert.Equal(0, item.Quantity());
    }

    [Fact]
    public void Bundle_RoundTripsState_AndReplaysTheLevelThroughUpgrade()
    {
        var item = new DurableItem { LevelKnown = true, Cursed = true, CursedKnown = false };
        item.Upgrade(2);
        item.Cursed = true; // Upgrade cleared it; put it back to store a cursed +2
        item.CursedKnown = false;
        item.Quantity(3);
        item.Use();

        var bundle = new Bundle();
        bundle.Put("item", item);
        var restored = (DurableItem)bundle.Get("item");

        Assert.Equal(3, restored.Quantity());
        Assert.Equal(2, restored.Level());
        Assert.True(restored.LevelKnown);
        Assert.True(restored.Cursed);
        // java restores cursedKnown first and then replays the level through
        // upgrade(), which sets cursedKnown = true, Kept verbatim
        Assert.True(restored.CursedKnown);
        Assert.Equal(11, restored.Durability());
    }
}