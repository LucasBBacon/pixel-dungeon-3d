using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class EquipmentTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#######",
        "#.....#",
        "#######"
    ];

    private SpyHero PlaceSpy(int x)
    {
        LoadLevel(Corridor);
        Dungeon.Hero = new SpyHero();
        return (SpyHero)PlaceHero(TestLevel.At(x, 1));
    }

    [Fact]
    public void Actions_OfferEquipOrUnequip_ByWhetherTheWeaponIsHeld()
    {
        var hero = Dungeon.Hero;
        var wep = new TestWeapon();

        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow, EquipableItem.AcEquip }, wep.Actions(hero));
        Assert.False(wep.IsEquipped(hero));

        hero.Belongings.Weapon = wep;

        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow, EquipableItem.AcUnequip }, wep.Actions(hero));
        Assert.True(wep.IsEquipped(hero));
    }

    [Fact]
    public void Equip_IntoAnEmptyHand_WieldsItActivatesItAndSpendsOneTurn()
    {
        var hero = PlaceSpy(2);
        var view = new FakeCharView();
        hero.Sprite = view;
        var wep = new TestWeapon();
        wep.Collect();

        wep.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(wep, hero.Belongings.Weapon);
        Assert.DoesNotContain(wep, hero.Belongings.Backpack.Items);
        Assert.Equal(1, wep.Activations);
        Assert.True(wep.CursedKnown); // equipping reveals the curse state
        Assert.Equal(0, view.Bursts); // no curse, no shadow burst
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndCursed); // no curse sound
        Assert.Empty(View.Logs); // and no warning
        Assert.False(hero.Ready);
        Assert.Equal(1f, hero.TimeLeft); // KindOfWeapon.TIME_TO_EQUIP
    }

    [Fact]
    public void Equip_OverAnotherWeapon_packsTheOldOneAndSpendsTwoTurns()
    {
        var hero = PlaceSpy(2);
        var old = new TestWeapon();
        hero.Belongings.Weapon = old;
        var wep = new TestWeapon();
        wep.Collect();

        wep.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(wep, hero.Belongings.Weapon);
        Assert.Contains(old, hero.Belongings.Backpack.Items);
        Assert.DoesNotContain(wep, hero.Belongings.Backpack.Items);
        // 2x SpendAndNext calls in one action, old weapon's DoUnequip(hero, true) spends
        // time2equip (1), then the new weapon spends TIME_TO_EQUIP (1)
        // Spend accumulates, and the second Next() finds the scheduler already released
        Assert.Equal(2f, hero.TimeLeft);
    }

    [Fact]
    public void Equip_Cursed_WieldsItWithABurstTheCurseSoundAndTheWinceWarning()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var wep = new TestWeapon { Cursed = true };
        wep.Collect();

        wep.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(wep, hero.Belongings.Weapon); // a cursed weapon still equips
        Assert.True(wep.CursedKnown);
        Assert.Equal(6, view.Bursts); // ShadowParticle.CURSE, 6
        Assert.Contains((Assets.SndCursed, 1f), View.Sounds);
        Assert.Contains(
            ("you wince as your grip involuntarily tightens around your test weapon", LogKind.Negative),
            View.Logs);
    }

    [Fact]
    public void Equip_OverACursedWeapon_KeepsTheOldOneAndRepacksTheNewOne()
    {
        var hero = PlaceSpy(2);
        var old = new TestWeapon { Cursed = true };
        hero.Belongings.Weapon = old;
        var wep = new TestWeapon();
        wep.Collect();

        wep.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(old, hero.Belongings.Weapon);
        Assert.Contains(wep, hero.Belongings.Backpack.Items); // collect(backpack)
        Assert.Contains(("You can't remove cursed test weapon!", LogKind.Warning), View.Logs);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void Unequip_PacksTheWeaponAndSpendsOneTurn()
    {
        var hero = PlaceSpy(2);
        var wep = new TestWeapon();
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, EquipableItem.AcUnequip);

        Assert.Null(hero.Belongings.Weapon);
        Assert.Contains(wep, hero.Belongings.Backpack.Items);
        Assert.False(hero.Ready);
        Assert.Equal(1f, hero.TimeLeft); // EquipableItem.time2equip
    }

    [Fact]
    public void Unequip_WithAFullPack_DorpsTheWeaponAtTheHeroCell()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(2, 1));
        for (var i = 0; i < Belongings.BackpackSize; i++)
        {
            new TestItem().Collect();
        }

        var wep = new TestWeapon();
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, EquipableItem.AcUnequip);

        Assert.Null(hero.Belongings.Weapon);
        Assert.DoesNotContain(wep, hero.Belongings.Backpack.Items);
        Assert.Same(wep, Dungeon.Level.Heaps[hero.Pos].Peek());
        Assert.True(View.Logged("Your pack is too full for the test weapon"));
    }

    [Fact]
    public void Unequip_Cursed_refusesWithTheJavaMessage_AndSpendsNothing()
    {
        var hero = PlaceSpy(2);
        var wep = new TestWeapon { Cursed = true };
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, EquipableItem.AcUnequip);

        Assert.Same(wep, hero.Belongings.Weapon);
        Assert.Contains(("You can't remove cursed test weapon!", LogKind.Warning), View.Logs);
        // EquipableItem.doUnequip only logs
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndCursed);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void Drop_AnEquippedWeapon_UnequipsThenDrops_ForATurnAndAHalf()
    {
        var hero = PlaceSpy(2);
        var wep = new TestWeapon();
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, Item.AcDrop);

        Assert.Null(hero.Belongings.Weapon);
        Assert.DoesNotContain(wep, hero.Belongings.Backpack.Items);
        Assert.Same(wep, Dungeon.Level.Heaps[hero.Pos].Peek());
        // DoUnequip(hero, false, false) spends time2equip (1) with no Next()
        // Item.doDrop's SpendAndNext(TIME_TO_DROP) then adds 0.5 and ends the action
        Assert.Equal(1.5f, hero.TimeLeft);
    }

    [Fact]
    public void Drop_ACursedEquippedWeapon_KeepsItWielded()
    {
        var hero = PlaceSpy(2);
        var wep = new TestWeapon { Cursed = true };
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, Item.AcDrop);

        Assert.Same(wep, hero.Belongings.Weapon);
        Assert.Empty(Dungeon.Level.Heaps);
        Assert.Contains(("You can't remove cursed test weapon!", LogKind.Warning), View.Logs);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void Throw_AnEquippedWeapon_UnequipsWithoutReleasingTheTurn_ThenFlies()
    {
        var hero = PlaceSpy(1);
        hero.Sprite = new FakeCharView();
        var wep = new TestWeapon();
        hero.Belongings.Weapon = wep;
        Actor.Process(); // the hero acts, becomes ready, and stays current

        View.HoldMissiles = true;
        wep.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.Null(hero.Belongings.Weapon);
        Assert.Equal(1f, hero.TimeLeft); // DoUnequip(hero, false, false): Spend(time2equip) and no Next()
        Actor.Process();
        Assert.False(hero.Ready); // the hero is still current, so nothing acted while the weapon flies

        View.CompleteMissile();

        Assert.Same(wep, Dungeon.Level.Heaps[TestLevel.At(4, 1)].Peek());
        Assert.Equal(2f, hero.TimeLeft); // plus Item.TIME_TO_THROW from the missile's SpendAndNext
    }

    [Fact]
    public void Throw_ACursedEquippedWeapon_LaunchesNothing()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var wep = new TestWeapon { Cursed = true };
        hero.Belongings.Weapon = wep;

        wep.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.Same(wep, hero.Belongings.Weapon);
        Assert.Empty(View.Missiles);
        Assert.Contains(("You can't remove cursed test weapon!", LogKind.Warning), View.Logs);
    }

    [Fact]
    public void DamageRoll_IsOneNormalRollBetweenMinAndMax()
    {
        var wep = new TestWeapon(); // STR 10 against the hero's 10: no excess strength

        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var damage = wep.DamageRoll(Dungeon.Hero);
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.NormalIntRange(1, 4), damage); // TestWeapon's Min() and Max()
            Assert.Equal(Random.Int(1000), next); // nothing else was drawn
        }
    }
}