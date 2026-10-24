using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class MissileWeaponTests : DungeonFixture
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
        var hero = (SpyHero)PlaceHero(TestLevel.At(x, 1));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static Rat PlaceRat(int x)
    {
        var rat = new Rat { Pos = TestLevel.At(x, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        return rat;
    }

    private sealed class ExposedDart : Dart
    {
        public void ThrowAt(int cell) => OnThrow(cell);
    }

    private sealed class Dodger : Mob
    {
        public Dodger()
        {
            Name = "dodger";
            HP = HT = 10;
        }

        public override int DefenseSkill(Char enemy) => 1000000;
    }

    private sealed class SpyDart : Dart
    {
        public MissileWeapon SeenRangedWeapon;

        public override float AccuracyFactor(Hero hero)
        {
            SeenRangedWeapon = hero.RangedWeapon;
            return base.AccuracyFactor(hero);
        }
    }

    [Theory]
    [InlineData(typeof(Dart), "dart", ItemSpriteSheet.Dart, 10, 1f, 1, 4, 2)]
    [InlineData(typeof(CurareDart), "curare dart", ItemSpriteSheet.CurareDart, 14, 1f, 1, 3, 12)]
    [InlineData(typeof(Shuriken), "shuriken", ItemSpriteSheet.Shuriken, 13, 0.5f, 2, 6, 15)]
    [InlineData(typeof(Javelin), "javelin", ItemSpriteSheet.Javelin, 15, 1f, 2, 15, 15)]
    [InlineData(typeof(Tamahawk), "tomahawk", ItemSpriteSheet.Tomahawk, 17, 1f, 4, 20, 20)]
    public void Stats_MatchTheJava(Type type,
        string name,
        int image,
        int str,
        float dly,
        int min,
        int max,
        int unitPrice)
    {
        var single = (MissileWeapon)Activator.CreateInstance(type);
        var stack = (MissileWeapon)Activator.CreateInstance(type, 3);

        Assert.NotNull(single);
        Assert.NotNull(stack);
        Assert.Equal(name, single.Name());
        Assert.Equal(image, single.Image());
        Assert.Equal(str, single.Str);
        Assert.Equal(1f, single.Acu);
        Assert.Equal(dly, single.Dly);
        Assert.Equal(min, single.Min());
        Assert.Equal(max, single.Max());
        Assert.Equal(1, single.Quantity());
        Assert.Equal(3, stack.Quantity());
        Assert.Equal(unitPrice, single.Price());
        Assert.Equal(unitPrice * 3, stack.Price());
    }

    [Theory]
    [InlineData(typeof(Dart),
        "These simple metal spikes are weighted to fly true and sting their prey with a flick of the wrist.")]
    [InlineData(typeof(CurareDart),
        "These little evil darts don't do much damage but they can paralyze the target leaving it helpless and motionless for some time.")]
    [InlineData(typeof(Shuriken),
        "Star-shaped pieces of metal with razor-sharp blades do significant damage when they hit a target. They can be thrown at very high rate.")]
    [InlineData(typeof(Javelin),
        "This length of metal is weighted to keep the spike at its tip foremost as it sails through the air.")]
    [InlineData(typeof(Tamahawk),
        "This throwing axe is not that heavy, but it still requires significant strength to be used effectively.")]
    public void Desc_IsTheJavas(Type type, string desc)
    {
        Assert.Equal(desc, ((Item)Activator.CreateInstance(type))?.Desc());
    }

    [Fact]
    public void Defaults_AreAThrowStack_ThatIsAlwaysIdentified()
    {
        var dart = new Dart();

        Assert.True(dart.Stackable);
        Assert.True(dart.LevelKnown);
        Assert.Equal(Item.AcThrow, dart.DefaultAction);
        Assert.False(dart.IsUpgradable());
        Assert.False(dart.CursedKnown);
        Assert.True(dart.IsIdentified()); // identified although its curse was never revealed
    }

    [Theory]
    [InlineData(typeof(Dart), 5, 15)]
    [InlineData(typeof(CurareDart), 2, 5)]
    [InlineData(typeof(Shuriken), 5, 15)]
    [InlineData(typeof(Javelin), 5, 15)]
    [InlineData(typeof(Tamahawk), 5, 12)]
    public void Random_RollsOnlyTheQuantity(Type type, int min, int max)
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var missile = (MissileWeapon)Activator.CreateInstance(type);
            Assert.NotNull(missile);

            Random.Seed(seed);
            var result = missile.Random();
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Same(missile, result);
            Assert.Equal(Random.Int(min, max), missile.Quantity()); // one Random.Int(min, max), which excludes max
            Assert.Equal(Random.Int(1000), next); // and nothing else: no upgrade, curse or enchant roll
            Assert.Equal(0, missile.Level());
            Assert.False(missile.Cursed);
        }
    }

    [Fact]
    public void Info_InThePack_GivesTheAverageDamage_AndTheStrengthLine()
    {
        var hero = Dungeon.Hero;
        var javelin = new Javelin();
        javelin.Collect();
        var dart = new Dart();
        dart.Collect();

        Assert.Equal(
            "This length of metal is weighted to keep the spike at its tip foremost as it sails through the air." +
            "\n\nAverage damage of this weapon equals to 8 points per hit. " + // 2 + (15 - 2) / 2 = 2 + 6
            "Because of your inadequate strength the accuracy and speed of your attack with this javelin is decreased.", // STR 15 > 10
            javelin.Info());

        hero.STR = 12;
        Assert.Equal(
            "These simple metal spikes are weighted to fly true and sting their prey with a flick of the wrist." +
            "\n\nAverage damage of this weapon equals to 2 points per hit. " + // 1 + (4 - 1) / 2 = 1 + 1
            "Because of your excess strength the damage of your attack with this dart is increased.", // STR 10 < 12
            dart.Info());
    }

    [Fact]
    public void Info_OutOfThePack_SkipsTheStrengthLine_AndWieldedHoldsItAtTheReady()
    {
        var hero = Dungeon.Hero;
        var tomahawk = new Tamahawk(); // STR 17 against 10, but not in the backpack

        const string desc =
            "This throwing axe is not that heavy, but it still requires significant strength to be used effectively." +
            "\n\nAverage damage of this weapon equals to 12 points per hit. "; // 4 + (20 - 4) / 2 = 4 + 8
        Assert.Equal(desc, tomahawk.Info());

        hero.Belongings.Weapon = tomahawk;
        Assert.Equal(desc + "\n\nYou hold the tomahawk at the ready.", tomahawk.Info());
    }

    [Theory]
    [InlineData(HeroClass.Rogue, true)]
    [InlineData(HeroClass.Huntress, true)]
    [InlineData(HeroClass.Warrior, false)]
    [InlineData(HeroClass.Mage, false)]
    public void Actions_OfferEquipOnlyToARogueOrAHuntress(HeroClass heroClass, bool offered)
    {
        var hero = Dungeon.Hero;
        hero.HeroClass = heroClass;
        var darts = new Dart(5);

        Assert.Equal(offered
                ? new[] { Item.AcDrop, Item.AcThrow, EquipableItem.AcEquip }
                : new[] { Item.AcDrop, Item.AcThrow },
            darts.Actions(hero));

        hero.Belongings.Weapon = darts;

        Assert.Equal(
            offered
                ? new[] { Item.AcDrop, Item.AcThrow, EquipableItem.AcUnequip }
                : new[] { Item.AcDrop, Item.AcThrow },
            darts.Actions(hero));
    }

    [Fact]
    public void Equip_AsksForConfirmation_AndChangesNothingYet()
    {
        var hero = PlaceSpy(1);
        var darts = new Dart(8);
        darts.Collect();

        darts.Execute(hero, EquipableItem.AcEquip);

        var window = Assert.Single(View.Windows);
        Assert.Equal("Missile weapon", window.Title);
        Assert.Equal("Do you really want to equip it as a melee weapon?", window.Body);
        Assert.Equal(new[] { "Yes, I know what I'm doing", "No, I changed my mind" }, window.Options);
        Assert.Null(hero.Belongings.Weapon);
        Assert.Contains(darts, hero.Belongings.Backpack.Items);
        Assert.Equal(0f, hero.TimeLeft);

        Assert.False(darts.DoEquip(hero));
    }

    [Fact]
    public void Equip_ConfirmedWithTheFirstOption_WieldsTheStack_OverTheOldWeapon()
    {
        var hero = PlaceSpy(1);
        var dagger = new Dagger();
        hero.Belongings.Weapon = dagger;
        var darts = new Dart(8);
        darts.Collect();

        darts.Execute(hero, EquipableItem.AcEquip);
        View.Windows[0].Select(0);

        Assert.Same(darts, hero.Belongings.Weapon);
        Assert.Equal(8, darts.Quantity());
        Assert.DoesNotContain(darts, hero.Belongings.Backpack.Items);
        Assert.Contains(dagger, hero.Belongings.Backpack.Items);
        Assert.Equal(2f, hero.TimeLeft); // KindOfWeapon.doEquip: the dagger's doUnequip spends 1, then TIME_TO_EQUIP 1
    }

    [Fact]
    public void Equip_DeclinedWithTheSecondOption_KeepsTheStackPacked()
    {
        var hero = PlaceSpy(1);
        var dagger = new Dagger();
        hero.Belongings.Weapon = dagger;
        var darts = new Dart(8);
        darts.Collect();

        darts.Execute(hero, EquipableItem.AcEquip);
        View.Windows[0].Select(1);

        Assert.Same(dagger, hero.Belongings.Weapon);
        Assert.Contains(darts, hero.Belongings.Backpack.Items);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void CurareDartProc_ParalysesTheDefenderForThreeTurns()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat { Sprite = new FakeCharView() };
        var curare = new CurareDart(2);
        hero.RangedWeapon = curare; // as Hero.Shoot sets it

        curare.Proc(hero, rat, 1);

        Assert.True(rat.Paralysed);
        Assert.Equal(3f, CurareDart.Duration);
        Assert.Equal(3f, CooldownOf(rat.GetBuff<Paralysis>())); // CurareDart.DURATION, not Paralysis.Duration(rat)'s 10
        Assert.Equal(2, curare.Quantity());
    }

    [Fact]
    public void JavelinProc_CripplesTheDefenderForCrippleDuration()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat { Sprite = new FakeCharView() };
        var javelin = new Javelin();
        hero.RangedWeapon = javelin;

        javelin.Proc(hero, rat, 5);

        Assert.Equal(10f, CooldownOf(rat.GetBuff<Cripple>()));
    }

    [Fact]
    public void TamahawkProc_BleedsTheDefenderByTheDamage()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat { Sprite = new FakeCharView() };
        var tomahawk = new Tamahawk();
        hero.RangedWeapon = tomahawk;

        tomahawk.Proc(hero, rat, 7);

        var bundle = new Bundle();
        rat.GetBuff<Bleeding>().StoreInBundle(bundle);
        Assert.Equal(7, bundle.GetInt("level"));
    }

    [Fact]
    public void AttackProc_InMeleeWithAnEquippedStack_UsesOneMissileUp()
    {
        var hero = PlaceSpy(1);
        var darts = new Dart(3);
        hero.Belongings.Weapon = darts;
        var rat = new Rat { Sprite = new FakeCharView() };

        Assert.Equal(2, hero.AttackProc(rat, 2));

        Assert.Equal(2, darts.Quantity()); // rangedWeapon is null and the stack holds more than one: detach(null)
        Assert.Same(darts, hero.Belongings.Weapon);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void AttackProc_InMeleeWithTheLastEquippedMissile_UnequipsItForATurn()
    {
        var hero = PlaceSpy(1);
        var dart = new Dart();
        hero.Belongings.Weapon = dart;
        var rat = new Rat { Sprite = new FakeCharView() };

        hero.AttackProc(rat, 2);

        Assert.Null(hero.Belongings.Weapon);
        Assert.DoesNotContain(dart, hero.Belongings.Backpack.Items); // doUnequip(hero, false, false) collects nothing
        Assert.Empty(Dungeon.Level.Heaps);
        Assert.Equal(1f, hero.TimeLeft); // time2equip 1, spent without Next
    }

    [Fact]
    public void Proc_DuringAShot_UsesNothingUp()
    {
        var hero = Dungeon.Hero;
        var darts = new Dart(3);
        hero.Belongings.Weapon = darts;
        hero.RangedWeapon = darts;
        var rat = new Rat { Sprite = new FakeCharView() };

        darts.Proc(hero, rat, 2);

        Assert.Equal(3, darts.Quantity());
        Assert.Same(darts, hero.Belongings.Weapon);
    }

    [Fact]
    public void Throw_AtAnEmptyCell_LandsOneMissile()
    {
        var hero = PlaceSpy(1);
        var darts = new Dart(3);
        darts.Collect();

        darts.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.Equal(2, darts.Quantity());
        var heap = Dungeon.Level.Heaps[TestLevel.At(4, 1)];
        Assert.IsType<Dart>(heap.Peek());
        Assert.NotSame(darts, heap.Peek());
    }

    [Fact]
    public void OnThrow_AtTheThrowersOwnCell_LandsThereWithoutAnAttack()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var dart = new ExposedDart();
        dart.Collect();
        dart.Execute(hero, Item.AcThrow);
        dart.Detach(hero.Belongings.Backpack);

        dart.ThrowAt(hero.Pos);

        Assert.Same(dart, Dungeon.Level.Heaps[hero.Pos].Peek());
        Assert.Empty(view.Statuses);
        Assert.Equal(20, hero.HP);
    }

    [Fact]
    public void Throw_HittingAMob_UsesTheMissileUp()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        PlaceRat(4);
        var darts = new Dart(3);
        darts.Collect();

        darts.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(5, 1)); // the bolt stops on the rat

        Assert.Equal(2, darts.Quantity());
        Assert.Empty(Dungeon.Level.Heaps); // a hit leaves nothing behind
        Assert.Contains(("you hit marsupial rat", LogKind.Info), View.Logs);
        Assert.Null(hero.RangedWeapon);
    }

    [Fact]
    public void Throw_MissingAMob_LandsTheMissileOnItsCell()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var dodger = new Dodger { Pos = TestLevel.At(4, 1), Sprite = new FakeCharView() };
        GameScene.Add(dodger);
        var darts = new Dart(3);
        darts.Collect();

        // Char.hit draws the attack roll, Random.Float(skill) with skill at most 10, then the defense roll
        // Replay both: the defense roll must beat the largest attack roll this seed allows
        Random.Seed(1);
        var attackCeiling = Random.Float(10);
        var defence = Random.Float(1000000);
        Assert.True(defence > attackCeiling);
        Random.Seed(1);

        darts.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(5, 1));

        Assert.Equal(2, darts.Quantity());
        var heap = Dungeon.Level.Heaps[TestLevel.At(4, 1)];
        Assert.IsType<Dart>(heap.Peek()); // miss(cell) lands it where the bolt stopped
        Assert.NotSame(darts, heap.Peek());
        Assert.Equal(10, dodger.HP);
        Assert.Null(hero.RangedWeapon);
    }

    [Fact]
    public void Bundle_RoundTripsAStack()
    {
        var bundle = new Bundle();
        bundle.Put("darts", new CurareDart(4));

        var restored = Assert.IsType<CurareDart>(bundle.Get("darts"));
        Assert.Equal(4, restored.Quantity());
        Assert.Equal(14, restored.Str);
        Assert.True(restored.LevelKnown);
    }

    [Fact]
    public void AttackSkill_WithARangedWeapon_HalvesAtDistanceOne()
    {
        LoadLevel("#####",
            "#...#",
            "#...#",
            "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var beside = new Rat { Pos = TestLevel.At(2, 1) };
        var diagonal = new Rat { Pos = TestLevel.At(2, 2) };
        var twoAway = new Rat { Pos = TestLevel.At(3, 1) };

        Assert.Equal(10, hero.AttackSkill(beside)); // no ranged weapon, no halving

        hero.RangedWeapon = new Dart(); // STR 10 against 10: factor 1
        Assert.Equal(5, hero.AttackSkill(beside)); // (int)(10 * 0.5f * 1)
        Assert.Equal(5, hero.AttackSkill(diagonal)); // Level.distance takes the larger axis gap, so this is 1 too
        Assert.Equal(10, hero.AttackSkill(twoAway)); // distance 2
    }

    [Fact]
    public void AttackSkill_WithARangedWeapon_UsesItsFactorOverTheWieldedWeapons()
    {
        var hero = Dungeon.Hero;
        hero.Pos = TestLevel.At(1, 1);
        var far = new Rat { Pos = TestLevel.At(4, 1) };
        hero.STR = 13;
        hero.Belongings.Weapon = new Dagger(); // STR 10, encumbrance -3: ACU 1.2
        Assert.Equal(12, hero.AttackSkill(far)); // (int)(10 * 1 * 1.2f)

        hero.RangedWeapon = new Shuriken(); // STR 13, encumbrance 0: ACU 1
        Assert.Equal(10, hero.AttackSkill(far));
    }

    [Theory]
    [InlineData(HeroClass.Warrior, 10, typeof(Dart), 0.2963)] // 10 - 10 + 3 = 3: 1 / 1.5^3 = 1 / 3.375
    [InlineData(HeroClass.Mage, 10, typeof(Dart), 1.0)] // no class shift: 0
    [InlineData(HeroClass.Rogue, 13, typeof(Javelin), 0.4444)] // 15 - 13 = 2: 1 / 2.25
    [InlineData(HeroClass.Huntress, 13, typeof(Javelin), 1.0)] // 2 - 2 = 0
    [InlineData(HeroClass.Huntress, 12, typeof(Javelin), 0.6667)] // 3 - 2 = 1: 1 / 1.5
    [InlineData(HeroClass.Warrior, 14, typeof(Sword), 1.0)] // a melee weapon takes no shift: 14 - 14 = 0
    public void AccuracyFactor_ForAMissile_ShiftsEncumbranceByClass(HeroClass heroClass, int str, Type type,
        double expected)
    {
        var hero = Dungeon.Hero;
        hero.HeroClass = heroClass;
        hero.STR = str;
        var weapon = (KindOfWeapon)Activator.CreateInstance(type);

        Assert.NotNull(weapon);
        Assert.Equal(expected, weapon.AccuracyFactor(hero), 4);
    }

    [Theory]
    [InlineData(HeroClass.Huntress, 13, typeof(Javelin), 1.0)] // 15 - 13 - 2 = 0: DLY 1
    [InlineData(HeroClass.Huntress, 12, typeof(Javelin), 1.2)] // 3 - 2 = 1: 1 * 1.2
    [InlineData(HeroClass.Rogue, 13, typeof(Javelin), 1.44)] // 2: 1.2^2
    [InlineData(HeroClass.Warrior, 13, typeof(Javelin), 1.44)] // speed takes no Warrior shift
    [InlineData(HeroClass.Huntress, 13, typeof(Sword), 1.2)] // a melee weapon takes no shift: 14 - 13 = 1
    public void SpeedFactor_ForAHuntresssMissile_LowersEncumbranceByTwo(HeroClass heroClass, int str, Type type,
        double expected)
    {
        var hero = Dungeon.Hero;
        hero.HeroClass = heroClass;
        hero.STR = str;
        var weapon = (KindOfWeapon)Activator.CreateInstance(type);

        Assert.NotNull(weapon);
        Assert.Equal(expected, weapon.SpeedFactor(hero), 4);
    }

    [Theory]
    [InlineData(HeroClass.Rogue, false, true)] // wielded: (false) == (false)
    [InlineData(HeroClass.Rogue, true, false)] // thrown: (true) == (false) fails
    [InlineData(HeroClass.Huntress, false, false)] // wielded: (false) == (true) fails
    [InlineData(HeroClass.Huntress, true, true)] // thrown: (true) == (true)
    [InlineData(HeroClass.Warrior, true, false)] // thrown: (true) == (false) fails
    public void DamageRoll_StrengthBonus_FollowsTheRangedAndHuntressGate(HeroClass heroClass, bool thrown, bool bonus)
    {
        var hero = Dungeon.Hero;
        hero.HeroClass = heroClass;
        hero.STR = 14; // exStr 14 - the Dart's 10 = 4
        var darts = new Dart(5);
        if (thrown)
        {
            hero.Belongings.Weapon = new Dagger(); // wielded, but the thrown dart is the one that rolls
            hero.RangedWeapon = darts;
        }
        else
        {
            hero.Belongings.Weapon = darts;
        }

        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var damage = hero.DamageRoll();
            var next = Random.Int(1000);

            Random.Seed(seed);
            var expected = Random.NormalIntRange(1, 4); // the Dart's Min() and Max()
            if (bonus)
            {
                expected += Random.IntRange(0, 4);
            }

            Assert.Equal(expected, damage);
            Assert.Equal(Random.Int(1000), next);
        }
    }

    [Fact]
    public void AttackDelay_WithARangedShuriken_IsItsHalfTurn_OverTheWieldedSpear()
    {
        var hero = Dungeon.Hero;
        hero.STR = 13;
        hero.Belongings.Weapon = new Spear(); // STR 12, encumbrance -1, dly 1.5
        Assert.Equal(1.5f, hero.AttackDelay());

        hero.RangedWeapon = new Shuriken(); // STR 13, encumbrance 0, dly 0.5
        Assert.Equal(0.5f, hero.AttackDelay());
    }

    [Fact]
    public void AttackProc_WithARangedWeapon_ProcsItInsteadOfTheWieldedWeapon()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat { Sprite = new FakeCharView() };
        var dagger = new Dagger();
        dagger.Upgrade(); // level 1: durability 5 * (16 - 1) = 75
        hero.Belongings.Weapon = dagger;
        hero.RangedWeapon = new Javelin();

        Assert.Equal(3, hero.AttackProc(rat, 3));

        Assert.NotNull(rat.GetBuff<Cripple>()); // javelin's proc ran
        Assert.Equal(75, dagger.Durability()); // dagger's weapon.proc never did
    }

    [Fact]
    public void Shoot_SetSRangedWeaponForTheAttack_AndClearsItAfter()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = PlaceRat(4);
        var dart = new SpyDart();

        Assert.True(hero.Shoot(rat, dart)); // rat's DefenseSkill = 0, sure hit

        Assert.Same(dart, dart.SeenRangedWeapon); // attackSkill() reached the dart through rangedWeapon
        Assert.Null(hero.RangedWeapon);
    }

    [Fact]
    public void Throw_AJavelinThatHits_CripplesTheTarget()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = new Rat
            { Pos = TestLevel.At(4, 1), Sprite = new FakeCharView(), HP = 100, HT = 100 }; // outlives a 2-15 javelin
        GameScene.Add(rat);
        var javelins = new Javelin(2);
        javelins.Collect();

        javelins.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(5, 1));

        Assert.NotNull(rat.GetBuff<Cripple>());
        Assert.Equal(1, javelins.Quantity());
        Assert.Empty(Dungeon.Level.Heaps);
    }

    [Theory]
    [InlineData(HeroClass.Rogue, 13, 0.5)] // Shuriken STR 13, encumbrance 0: TIME_TO_THROW 1 * DLY 0.5
    [InlineData(HeroClass.Rogue, 10, 0.864)] // encumbrance 3: 1 * 0.5 * 1.2^3 = 0.5 * 1.728
    [InlineData(HeroClass.Huntress, 11, 0.5)] // encumbrance 2 - 2 = 0
    public void Throw_AMissile_ScalesTheThrowDelayBySpeedFactor(HeroClass heroClass, int str, double expected)
    {
        var hero = PlaceSpy(1);
        hero.HeroClass = heroClass;
        hero.STR = str;
        var shuriken = new Shuriken(3);
        shuriken.Collect();

        shuriken.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1)); // an empty cell: the shuriken lands, and the callback spends the delay

        Assert.Equal(expected, hero.TimeLeft, 4);
    }
}