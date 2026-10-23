using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Hero;

public class HeroEquipmentTests : DungeonFixture
{
    [Fact]
    public void AttackSkill_BareHandedOrWithAMatchingDagger_ScalesByTheWeaponsAccuracy()
    {
        var hero = Dungeon.Hero;
        Assert.Equal(10, hero.AttackSkill(null)); // bare hands: (int)(10 * 1)

        hero.Belongings.Weapon = new Dagger();
        Assert.Equal(12, hero.AttackSkill(null)); // str 10 against 10: (int)(10 * 1 * 1.2f [Acu])
    }

    [Theory]
    [InlineData(13, 6)] // Sword STR 14: encumbrance 1, factor 1 / 1.5 = 0.66667; (int)(10 * 0.66667) = 6
    [InlineData(11, 2)] // encumbrance 3, factor 1 / 3.375 = 0.29630; (int)(10 * 0.29630) = 2
    public void AttackSkill_WithEncumbrance_DividesBy1Point5PerPoint(int str, int expected)
    {
        var hero = Dungeon.Hero;
        hero.STR = str;
        hero.Belongings.Weapon = new Sword();

        Assert.Equal(expected, hero.AttackSkill(null));
    }

    [Theory]
    [InlineData(typeof(LeatherArmor), 3)] // aEnc 11 - 10 = 1: (int)(5 * 1 / 1.5) = (int)3.33
    [InlineData(typeof(MailArmor), 1)] // aEnc 13 - 10 = 3: (int)(5 * 1 / 3.375) = (int)1.48
    public void DefenseSkill_WithEncumbrance_DividesBy1Point5PerPoint(Type armor, int expected)
    {
        var hero = Dungeon.Hero;
        hero.Belongings.Armor = (Armor)Activator.CreateInstance(armor);

        Assert.Equal(expected, hero.DefenseSkill(null));
    }

    [Fact]
    public void DefenseSkill_ArmorNoHeavierThanStr_HelpsOnlyRogue()
    {
        var hero = Dungeon.Hero;
        Assert.Equal(5, hero.DefenseSkill(null)); // no armor: aEnc 0, (5 - 0) * 1

        hero.STR = 11;
        hero.Belongings.Armor = new LeatherArmor();
        Assert.Equal(5, hero.DefenseSkill(null)); // matching STR: aEnc 11 - 11 = 0

        hero.STR = 10;
        hero.Belongings.Armor = new ClothArmor();
        Assert.Equal(6, hero.DefenseSkill(null)); // aEnc 9 - 10 = -1: (5 - -1) * 1

        hero.STR = 20;
        hero.Belongings.Armor = new PlateArmor();
        Assert.Equal(8, hero.DefenseSkill(null)); // aEnc 17 - 20 = -3: (5 + 3) * 1

        hero.HeroClass = HeroClass.Warrior;
        Assert.Equal(5, hero.DefenseSkill(null)); // other classes ignore light armor: 5 * 1
    }

    [Fact]
    public void DefenseSkill_WhenParalysed_HalvesEvasionAlongsideTheArmor()
    {
        var hero = Dungeon.Hero;
        hero.Paralysed = true; // evasion 1 / 2

        hero.Belongings.Armor = new ClothArmor();
        Assert.Equal(3, hero.DefenseSkill(null)); // a Rogue at aEnc -1: (int)((5 + 1) * 0.5f)

        hero.Belongings.Armor = new LeatherArmor();
        Assert.Equal(1, hero.DefenseSkill(null)); // aEnc 1: (int)(5 * 0.5f / 1.5) = (int)1.67
    }

    [Fact]
    public void Dr_IsTheWornAmorDr_FlooredAtZero()
    {
        var hero = Dungeon.Hero;
        Assert.Equal(0, hero.Dr()); // no armour

        hero.Belongings.Armor = new ClothArmor();
        Assert.Equal(2, hero.Dr()); // 1 * (2 + 0)

        var mail = new MailArmor();
        mail.Upgrade();
        hero.Belongings.Armor = mail;
        Assert.Equal(9, hero.Dr()); // 3 * (2 + 1)

        var degraded = new ClothArmor();
        degraded.Degrade(3);
        hero.Belongings.Armor = degraded;
        Assert.Equal(0, hero.Dr()); // Math.max(1 * (2 - 3), 0)
    }

    [Fact]
    public void DamageRoll_WithAWeapon_RollsTheWeaponInsteadOfBareHands()
    {
        var hero = Dungeon.Hero;
        hero.STR = 14;

        for (var seed = 1; seed <= 20; seed++)
        {
            hero.Belongings.Weapon = null;
            Random.Seed(seed);
            var bare = hero.DamageRoll();
            var afterBare = Random.Int(1000);

            hero.Belongings.Weapon = new Dagger();
            Random.Seed(seed);
            var armed = hero.DamageRoll();
            var afterArmed = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.IntRange(1, 5), bare); // STR 14 > 10: IntRange(1, STR - 9)
            Assert.Equal(Random.Int(1000), afterBare);

            Random.Seed(seed);
            // The dagger's NormalIntRange(1, 8), then the excess-strength bonus IntRange(0, 14 - 10) a Rogue gets in melee
            Assert.Equal(Random.NormalIntRange(1, 8) + Random.IntRange(0, 4), armed);
            Assert.Equal(Random.Int(1000), afterArmed);
        }
    }

    [Theory]
    [InlineData(typeof(LeatherArmor), 0.7692)] // aEnc 11 - 10 = 1: 1.3^-1 = 0.76923
    [InlineData(typeof(MailArmor), 0.4552)] // aEnc 13 - 10 = 3: 1 / 1.3^3 = 1 / 2.197 = 0.45517
    public void Speed_WithEncumbrance_MultipliesBy1Point3ToTheMinusEncumbrance(Type armour, double expected)
    {
        var hero = Dungeon.Hero;
        hero.Belongings.Armor = (Armor)Activator.CreateInstance(armour);

        Assert.Equal(expected, hero.Speed(), 4);
    }

    [Fact]
    public void Speed_LightArmourChangesNothing_AndCrippleCompoundsWithEncumbrance()
    {
        LoadLevel("###", "#.#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));
        Assert.Equal(1f, hero.Speed()); // no armour

        hero.Belongings.Armor = new ClothArmor();
        Assert.Equal(1f, hero.Speed()); // aEnc -1: lighter armor gives no bonus

        Buff.Prolong<Cripple>(hero, Cripple.Duration);
        Assert.Equal(0.5f, hero.Speed()); // Char.speed(): 1 * 0.5

        hero.Belongings.Armor = new LeatherArmor();
        Assert.Equal(0.3846, hero.Speed(), 4); // aEnc 1: 0.5 * 1.3^-1 = 0.38462
    }

    [Fact]
    public void Equip_PlateOverCloth_SpendsThePlatesTimeAtThePlatesSpeed()
    {
        LoadLevel("###", "#.#", "###");
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.Belongings.Armor = new ClothArmor(); // STR 9: no encumbrance at the hero's 10
        var plate = new PlateArmor(); // STR 17: encumbrance 7
        plate.Collect();

        plate.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(plate, hero.Belongings.Armor);
        // The cloth's doUnequip spends while the cloth is still worn: 2 / 1 = 2.
        // Armor.doEquip wears the plate before its own spend, so that spend runs at speed 1.3^-7:
        // 2 / 1.3^-7 = 2 * 6.2748517 = 12.5497. Together 14.5497.
        Assert.Equal(14.5497, hero.TimeLeft, 3);
    }

    [Fact]
    public void AttackDelay_IsTheWeaponsSpeedFactor_OrOneBareHanded()
    {
        var hero = Dungeon.Hero;
        Assert.Equal(1f, hero.AttackDelay()); // bare hands

        hero.Belongings.Weapon = new Knuckles();
        Assert.Equal(0.5f, hero.AttackDelay()); // DLY 0.5, STR 10 against 10

        hero.STR = 13;
        hero.Belongings.Weapon = new Sword();
        Assert.Equal(1.2, hero.AttackDelay(), 4); // STR 14 - 13 = 1: DLY 1 * 1.2^1

        hero.STR = 10;
        hero.Belongings.Weapon = new Spear();
        Assert.Equal(2.16, hero.AttackDelay(), 4); // STR 12 - 10 = 2: DLY 1.5 * 1.2^2 = 1.5 * 1.44
    }

    [Fact]
    public void ActAttack_WithKnuckles_SpendsHalfATurn()
    {
        LoadLevel("#####", "#<..#", "#####");
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        hero.Belongings.Weapon = new Knuckles();
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();

        // The first Handle only notices the newly visible rat and interrupts itself, as in
        // HeroCombatTests.Handle_OnAnAdjacentRat_SwingsThroughTheView, the second swings.
        hero.Handle(rat.Pos);
        hero.Handle(rat.Pos);

        Assert.Contains(rat.Pos, view.Attacks);
        Assert.Equal(0.5f, hero.TimeLeft); // actAttack: spend(attackDelay()), the knuckleduster's DLY 0.5
    }

    [Fact]
    public void AttackProc_ProcsTheWieldedWeapon_AndReturnsTheDamage()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat();
        Assert.Equal(7, hero.AttackProc(rat, 7)); // bare hands: nothing to proc

        var dagger = new Dagger();
        dagger.Upgrade(); // level 1: durability 5 * (16 - 1) = 75
        hero.Belongings.Weapon = dagger;

        Assert.Equal(7, hero.AttackProc(rat, 7)); // no subclass branch adds damage
        Assert.Equal(74, dagger.Durability()); // Weapon.proc() used the dagger
    }

    [Fact]
    public void DefenseProc_ProcsTheWornArmour_AndReturnsTheDamage()
    {
        var hero = Dungeon.Hero;
        var rat = new Rat();
        Assert.Equal(7, hero.DefenseProc(rat, 7)); // no armor

        var cloth = new ClothArmor();
        cloth.Upgrade(); // level 1: durability 6 * (16 - 1) = 90
        hero.Belongings.Armor = cloth;

        Assert.Equal(7, hero.DefenseProc(rat, 7)); // no glyph to change it
        Assert.Equal(89, cloth.Durability()); // Armor.proc() used the armor
    }

    [Fact]
    public void DefenseProc_TenthHitTaken_IdentifiesTheWornArmour()
    {
        var hero = Dungeon.Hero;
        var leather = new LeatherArmor();
        hero.Belongings.Armor = leather;
        var rat = new Rat();

        for (var i = 0; i < 9; i++)
        {
            hero.DefenseProc(rat, 1);
        }

        Assert.False(leather.LevelKnown);

        hero.DefenseProc(rat, 1); // hit 10

        Assert.True(leather.LevelKnown);
        Assert.Contains(
            ("you are now familiar enough with your leather armor to identify it. It is leather armor :11.",
                LogKind.Warning),
            View.Logs);
    }
}