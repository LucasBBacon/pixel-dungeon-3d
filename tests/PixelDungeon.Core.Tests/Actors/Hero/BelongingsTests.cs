using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Hero;

public class BelongingsTests : DungeonFixture
{
    private static Belongings Stuff => Dungeon.Hero.Belongings;

    [Fact]
    public void Enumerator_ListsEquippedSlotsThenTheBackpackWithNestedBags()
    {
        var weapon = new TestWeapon();
        var ring = new TestItem();
        Stuff.Weapon = weapon;
        Stuff.Ring1 = ring;
        var keyring = new Keyring();
        keyring.Collect();
        var loose = new StackItem(2);
        loose.Collect();
        var key = new IronKey();
        key.Collect();

        Assert.Equal(new Item[] { weapon, ring, keyring, key, loose }, Stuff.ToList());
    }

    [Fact]
    public void GetItem_FindsEquippedAndPackedItemsByType()
    {
        var armor = new TestItem();
        Stuff.Armor = armor;
        var stack = new StackItem(1);
        stack.Collect();

        Assert.Same(armor, Stuff.GetItem<TestItem>());
        Assert.Same(stack, Stuff.GetItem<StackItem>());
        Assert.Same(stack, Stuff.GetItem(typeof(StackItem)));
        Assert.Null(Stuff.GetItem<DurableItem>());
    }

    [Fact]
    public void GetKey_MatchesTheExactClassAndDepth()
    {
        var golden = new GoldenKey { Depth = 2 };
        var iron = new IronKey { Depth = 2 };
        golden.Collect();
        iron.Collect();

        Assert.Same(golden, Stuff.GetKey<GoldenKey>(2));
        Assert.Same(iron, Stuff.GetKey<IronKey>(2));
        Assert.Null(Stuff.GetKey<GoldenKey>(3));
        Assert.Null(Stuff.GetKey<SkeletonKey>(2));
    }

    [Fact]
    public void IronKeys_CountONlyThoseFromTheCurrentDepth()
    {
        Dungeon.Depth = 3;
        var here = new IronKey();
        var elsewhere = new IronKey { Depth = 2 };

        here.Collect();
        elsewhere.Collect();
        Assert.Equal(1, IronKey.CurDepthQuantity);

        here.DetachAll(Stuff.Backpack);
        Assert.Equal(0, IronKey.CurDepthQuantity);
    }

    [Fact]
    public void Keys_HaveTheirJavaNamesANdText()
    {
        Assert.Equal("iron key from depth 4", new IronKey { Depth = 4 }.ToString());
        Assert.Equal("golden key", new GoldenKey().Name());
        Assert.Equal("skeleton key", new SkeletonKey().Name());
    }

    [Fact]
    public void Identify_IdentifiesEverything()
    {
        var packed = new TestItem();
        packed.Collect();
        Stuff.Weapon = new TestWeapon();

        Stuff.Identify();

        Assert.True(packed.IsIdentified());
        Assert.True(Stuff.Weapon.IsIdentified());
    }

    [Fact]
    public void Observe_IdentifiesTheSlots_AndRevealsBackpackCursesOnly()
    {
        var packed = new TestItem();
        packed.Collect();
        Stuff.Armor = new TestItem();

        Stuff.Observe();

        Assert.True(Stuff.Armor.IsIdentified());
        Assert.True(packed.CursedKnown);
        Assert.False(packed.LevelKnown);
    }

    [Fact]
    public void RandomUnequipped_PicksFromTheBackpack()
    {
        var packed = new TestItem();
        packed.Collect();
        Stuff.Weapon = new TestWeapon();

        Assert.Same(packed, Stuff.RandomUnequipped());
    }

    [Fact]
    public void Resurrect_KeepsUniqueItemsAndOtherDepthKeys_AndUncursesTheSlots()
    {
        var keep = new TestItem { Unique = true };
        var lose = new TestItem();
        var thisDepthKey = new GoldenKey { Depth = 2 };
        var otherDepthKey = new GoldenKey { Depth = 3 };
        foreach (var item in new Item[] { keep, lose, thisDepthKey, otherDepthKey })
        {
            item.Collect();
        }

        Stuff.Armor = new TestItem { Cursed = true };

        Stuff.Resurrect(2);

        Assert.Contains(keep, Stuff.Backpack.Items);
        Assert.DoesNotContain(lose, Stuff.Backpack.Items);
        Assert.DoesNotContain(thisDepthKey, Stuff.Backpack.Items);
        Assert.Contains(otherDepthKey, Stuff.Backpack.Items);
        Assert.False(Stuff.Armor.Cursed);
    }

    [Fact]
    public void Bundle_RoundTripsTheBackpackAndTheSlots()
    {
        new Keyring().Collect();
        new GoldenKey { Depth = 5 }.Collect();
        new StackItem(4).Collect();
        Stuff.Armor = new TestItem();

        var bundle = new Bundle();
        Stuff.StoreInBundle(bundle);
        var restored = new Belongings(Dungeon.Hero);
        restored.RestoreFromBundle(bundle);

        Assert.IsType<TestItem>(restored.Armor);
        Assert.Null(restored.Weapon);
        var ring = Assert.Single(restored.Backpack.Items.OfType<Keyring>());
        Assert.Equal(5, Assert.IsType<GoldenKey>(Assert.Single(ring.Items)).Depth);
        Assert.Equal(4, Assert.Single(restored.Backpack.Items.OfType<StackItem>()).Quantity());
    }

    [Fact]
    public void Bundle_RestoringAWeapon_ActivatesIt()
    {
        Stuff.Weapon = new TestWeapon();

        var bundle = new Bundle();
        Stuff.StoreInBundle(bundle);
        var restored = new Belongings(Dungeon.Hero);
        restored.RestoreFromBundle(bundle);

        var weapon = Assert.IsType<TestWeapon>(restored.Weapon);
        Assert.NotSame(Stuff.Weapon, weapon);
        Assert.Equal(1, weapon.Activations); // Belongings.restoreFromBundle, if (weapon != null) weapon.activate(owner)
    }

    [Fact]
    public void Resurrect_UncursesAndReactivatesTheWeapon()
    {
        var weapon = new TestWeapon { Cursed = true };
        Stuff.Weapon = weapon;

        Stuff.Resurrect(1);

        Assert.False(weapon.Cursed);
        Assert.Equal(1, weapon.Activations); // Belongings.resurrect: weapon.cursed = false; weapon.activate(owner)
    }

    [Fact]
    public void DungeonInit_GivesTheHeroAKeyRing()
    {
        Dungeon.Init();
        Assert.IsType<Keyring>(Assert.Single(Dungeon.Hero.Belongings.Backpack.Items));
    }
}