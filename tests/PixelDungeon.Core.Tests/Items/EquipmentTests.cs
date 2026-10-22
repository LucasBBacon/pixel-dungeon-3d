using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Weapon;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
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

    [Fact]
    public void DamageRoll_WithExcessStrength_AddsARollUpToTheExcess()
    {
        var wep = new TestWeapon { Str = 8 }; // hero's STR 10: exStr = 10 - 8 = 2

        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var damage = wep.DamageRoll(Dungeon.Hero);
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.NormalIntRange(1, 4) + Random.IntRange(0, 2),
                damage); // the base roll, then IntRange(0, exStr)
            Assert.Equal(Random.Int(1000), next);
        }
    }

    [Fact]
    public void DamageRoll_ForAHuntressInMelee_AddsNoStrengthBonus()
    {
        Dungeon.Hero.HeroClass = HeroClass.Huntress;
        var wep = new TestWeapon { Str = 8 };

        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var damage = wep.DamageRoll(Dungeon.Hero);
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.NormalIntRange(1, 4),
                damage); // (rangedWeapon != null) == (heroClass == HUNTRESS) is false in melee
            Assert.Equal(Random.Int(1000), next); // so IntRange is never drawn
        }
    }

    [Theory]
    [InlineData(10, 1.0)] // encumbrance 10 - 10 = 0: ACU
    [InlineData(8, 1.0)] // encumbrance -2: ACU
    [InlineData(11, 0.6667)] // encumbrance 1: 1 / 1.5 = 0.66667
    [InlineData(13, 0.2963)] // encumbrance 3: 1 / 1.5^3 = 1 / 3.375 = 0.29630
    public void AccuracyFactor_DividesBy1Point5PerPointOfEncumbrance(int str, double expected)
    {
        var wep = new TestWeapon { Str = str };

        Assert.Equal(expected, wep.AccuracyFactor(Dungeon.Hero), 4);
    }

    [Fact]
    public void AccuracyFactor_WithTheAccuracyImbue_IsHalfAgainHigher()
    {
        var plain = new TestWeapon { ImbueValue = Weapon.Imbue.Accuracy };
        Assert.Equal(1.5, plain.AccuracyFactor(Dungeon.Hero), 4); // ACU 1 * 1.5

        var heavy = new TestWeapon { Str = 11, Acu = 1.2f, ImbueValue = Weapon.Imbue.Accuracy };
        Assert.Equal(1.2, heavy.AccuracyFactor(Dungeon.Hero), 4); // 1.2 / 1.5^1 = 0.8, then * 1.5
    }

    [Theory]
    [InlineData(10, 1.0)] // encumbrance 0: DLY
    [InlineData(8, 1.0)] // encumbrance -2: DLY
    [InlineData(11, 1.2)] // encumbrance 1: 1 * 1.2
    [InlineData(13, 1.728)] // encumbrance 3: 1.2^3 = 1.728
    public void SpeedFactor_MultipliesBy1Point2PerPointOfEncumbrance(int str, double expected)
    {
        var wep = new TestWeapon { Str = str };

        Assert.Equal(expected, wep.SpeedFactor(Dungeon.Hero), 4);
    }

    [Fact]
    public void SpeedFactor_WithTheSpeedImbue_IsSixTenths()
    {
        var plain = new TestWeapon { ImbueValue = Weapon.Imbue.Speed };
        Assert.Equal(0.6, plain.SpeedFactor(Dungeon.Hero), 4); // DLY 1 * 0.6

        var heavy = new TestWeapon { Str = 11, Dly = 0.5f, ImbueValue = Weapon.Imbue.Speed };
        Assert.Equal(0.36, heavy.SpeedFactor(Dungeon.Hero), 4); // 0.5 * 1.2^1 = 0.6, then * 0.6
    }

    [Fact]
    public void Proc_TwentiethHitWhileUnknown_IdentifiesWithTheJavaMessage()
    {
        var wep = new TestWeapon();
        var rat = new Rat();

        for (var i = 0; i < 19; i++)
        {
            wep.Proc(Dungeon.Hero, rat, 1);
        }

        Assert.False(wep.LevelKnown);
        Assert.Empty(View.Logs);

        wep.Proc(Dungeon.Hero, rat, 1); // hit 20: --hitsToKnow reaches 0

        Assert.True(wep.LevelKnown);
        // name() and then toString(), which now shows the strength: "%s :%d"
        Assert.Equal(
            ("You are now familiar enough with your test weapon to identify it. It is test weapon :10.", LogKind.Info),
            Assert.Single(View.Logs));
    }

    [Fact]
    public void Proc_WithTheTestLevelKnown_CountsNoHits()
    {
        var wep = new TestWeapon { LevelKnown = true };
        var rat = new Rat();

        for (var i = 0; i < 25; i++)
        {
            wep.Proc(Dungeon.Hero, rat, 1);
        }

        var bundle = new Bundle();
        wep.StoreInBundle(bundle);
        Assert.Equal(20, bundle.GetInt("unfamiliarity")); // still HITS_TO_KNOW
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Proc_UsesTheWeapon()
    {
        var wep = new TestWeapon();
        wep.Upgrade(); // level 1: durability 5 * (16 - 1) = 75

        wep.Proc(Dungeon.Hero, new Rat(), 1);

        Assert.Equal(74, wep.Durability()); // Item.use() wears one point
    }

    [Fact]
    public void Bundle_RoundTripsUnfamiliarityAndImbue()
    {
        var wep = new TestWeapon { ImbueValue = Weapon.Imbue.Speed };
        var rat = new Rat();
        for (var i = 0; i < 5; i++)
        {
            wep.Proc(Dungeon.Hero, rat, 1);
        }

        var bundle = new Bundle();
        bundle.Put("item", wep);
        var stored = bundle.GetBundle("item");
        Assert.Equal(15, stored.GetInt("unfamiliarity")); // HITS_TO_KNOW 20 - 5 hits
        Assert.Equal("Speed", stored.GetString("imbue"));

        var restored = (TestWeapon)bundle.Get("item");
        Assert.Equal(Weapon.Imbue.Speed, restored.ImbueValue);
        var again = new Bundle();
        restored.StoreInBundle(again);
        Assert.Equal(15, again.GetInt("unfamiliarity"));
    }

    [Fact]
    public void Bundle_ZeroUnfamiliarity_RestoresTwenty()
    {
        var bundle = new Bundle();
        new TestWeapon().StoreInBundle(bundle);
        bundle.Put("unfamiliarity", 0);

        var restored = new TestWeapon();
        restored.RestoreFromBundle(bundle);
        var again = new Bundle();
        restored.StoreInBundle(again);

        Assert.Equal(20, again.GetInt("unfamiliarity")); // 0 restores to HITS_TO_KNOW
    }

    [Fact]
    public void MaxDurability_IsFivePerLevelBelowSixteen()
    {
        var wep = new TestWeapon();

        Assert.Equal(80, wep.MaxDurability(0)); // 5 * (16 - 0)
        Assert.Equal(85, wep.MaxDurability(-1)); // 5 * (16 + 1)
        Assert.Equal(5, wep.MaxDurability(15)); // 5 * (16 - 15)
        Assert.Equal(5, wep.MaxDurability(16)); // 5 * 1
        Assert.Equal(5, wep.MaxDurability(30)); // 5 * 1
        Assert.Equal(80, wep.Durability()); // Item's constructor filled durability through the override
    }

    [Fact]
    public void ToString_KnownLevel_addsTheStrength_AndBrokenPrefixesIt()
    {
        var wep = new TestWeapon();
        Assert.Equal("test weapon", wep.ToString()); // level unknown: Item.toString()

        wep.LevelKnown = true;
        Assert.Equal("test weapon :10", wep.ToString()); // "%s :%d"

        wep.LevelKnown = false;
        wep.Upgrade(); // level 1, durability 75
        for (var i = 0; i < 75; i++)
        {
            wep.Use();
        }

        wep.LevelKnown = true;

        Assert.True(wep.IsBroken());
        Assert.Equal("broken test weapon+1 :10", wep.ToString()); // "broken %s :%d"
    }

    [Fact]
    public void Random_RollsUpgradeOrCurseInTheJavaOrder()
    {
        var upgraded = false;
        var degraded = false;
        var untouched = false;

        // Small consecutive seeds do not provide independent rolls with this RNG.
        foreach (var seed in new[] { 1, 4096, 4097 })
        {
            Random.Seed(seed);
            var wep = new TestWeapon();
            wep.Random();
            var next = Random.Int(1000);

            Random.Seed(seed);
            var level = WeaponRolls.ReplayWeaponRandom();

            Assert.Equal(level, wep.Level());
            Assert.Equal(level < 0, wep.Cursed); // a degrade curses; an upgrade leaves it clean
            Assert.Equal(Random.Int(1000), next); // Weapon.random() draws nothing more

            upgraded |= level > 0;
            degraded |= level < 0;
            untouched |= level == 0;
        }

        // These seeds cover no change, a degrade, and an upgrade.
        Assert.True(upgraded);
        Assert.True(degraded);
        Assert.True(untouched);
        Assert.True(upgraded && degraded && untouched);
    }

    [Fact]
    public void Upgrade_WithEnchant_DrawsEnchantmentRandomsRoll_AndWithoutDrawsNothing()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var enchanted = new TestWeapon();
            enchanted.Upgrade(true);
            var afterEnchant = Random.Int(1000);

            Random.Seed(seed);
            var plain = new TestWeapon();
            plain.Upgrade(false);
            var afterPlain = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.Int(1000), afterPlain); // upgrade(false) on an unenchanted weapon draws nothing

            Random.Seed(seed);
            Random.Chances(WeaponRolls.EnchantmentChances); // enchant() -> Enchantment.random()
            Assert.Equal(Random.Int(1000), afterEnchant);

            Assert.Equal(1, enchanted.Level());
            Assert.Equal(1, plain.Level());
        }
    }
}