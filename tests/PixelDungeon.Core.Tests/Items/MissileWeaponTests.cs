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
}