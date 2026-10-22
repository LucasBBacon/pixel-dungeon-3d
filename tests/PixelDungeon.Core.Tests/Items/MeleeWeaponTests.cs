using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class MeleeWeaponTests : DungeonFixture
{
    [Theory]
    [InlineData(typeof(Dagger), 10, 10, 1,
        8)] // tier 1, ACU 1.2f: STR 8 + 1*2; max0 (1 - 1 + 10) / 1.2f * 1f = 8.33f -> 8
    [InlineData(typeof(Knuckles), 10, 10, 1, 5)] // tier 1, DLY 0.5f: 10 / 1f * 0.5f = 5
    [InlineData(typeof(Quarterstaff), 12, 12, 2, 12)] // tier 2: STR 8 + 2*2; (4 - 2 + 10) / 1f * 1f = 12
    [InlineData(typeof(Spear), 12, 12, 2, 18)] // tier 2, DLY 1.5f: 12 / 1f * 1.5f = 18
    [InlineData(typeof(Mace), 14, 14, 3, 12)] // tier 3, DLY 0.8f: (9 - 3 + 10) / 1f * 0.8f = 12.8f -> 12
    [InlineData(typeof(Sword), 14, 14, 3, 16)] // tier 3: STR 8 + 3*2; (9 - 3 + 10) = 16
    [InlineData(typeof(Longsword), 16, 16, 4, 22)] // tier 4: STR 8 + 4*2; (16 - 4 + 10) = 22
    [InlineData(typeof(BattleAxe), 16, 16, 4, 18)] // tier 4, ACU 1.2f: 22 / 1.2f = 18.33f -> 18
    [InlineData(typeof(WarHammer), 18, 18, 5, 24)] // tier 5, ACU 1.2f: 30 / 1.2f = 24.999998f -> 24 (double gives 25)
    [InlineData(typeof(Glaive), 18, 18, 5, 30)] // tier 5: STR 8 + 5*2; (25 - 5 + 10) = 30
    [InlineData(typeof(ShortSword), 10, 11, 1,
        12)] // tier 1: typical 10, but the constructor sets STR = 11; max0() is overridden to 12
    public void Stats_AtLevelZero_MatchTheJava(Type type, int typicalStr, int str, int min, int max)
    {
        var weapon = (MeleeWeapon)Activator.CreateInstance(type);

        Assert.NotNull(weapon);
        Assert.Equal(typicalStr, weapon.TypicalStr());
        Assert.Equal(str, weapon.Str);
        Assert.Equal(min, weapon.Min());
        Assert.Equal(max, weapon.Max());
    }

    [Theory]
    [InlineData(typeof(Dagger), "dagger", ItemSpriteSheet.Dagger,
        "A simple iron dagger with a well worn wooden handle.")]
    [InlineData(typeof(Knuckles), "knuckleduster", ItemSpriteSheet.Knuckleduster,
        "A piece of iron shaped to fit around the knuckles.")]
    [InlineData(typeof(Quarterstaff), "quarterstaff", ItemSpriteSheet.Quarterstaff,
        "A staff of hardwood, its ends are shod with iron.")]
    [InlineData(typeof(Spear), "spear", ItemSpriteSheet.Spear, "A slender wooden rod tipped with sharpened iron.")]
    [InlineData(typeof(Mace), "mace", ItemSpriteSheet.Mace,
        "The iron head of this weapon inflicts substantial damage.")]
    [InlineData(typeof(Sword), "sword", ItemSpriteSheet.Sword,
        "The razor-sharp length of steel blade shines reassuringly.")]
    [InlineData(typeof(Longsword), "longsword", ItemSpriteSheet.LongSword,
        "This towering blade inflicts heavy damage by investing its heft into every cut.")]
    [InlineData(typeof(BattleAxe), "battle axe", ItemSpriteSheet.BattleAxe,
        "The enormous steel head of this battle axe puts considerable heft behind each stroke.")]
    [InlineData(typeof(WarHammer), "war hammer", ItemSpriteSheet.WarHammer,
        "Few creatures can withstand the crushing blow of this towering mass of lead and steel, but only the strongest of adventurers can use it effectively.")]
    [InlineData(typeof(Glaive), "glaive", ItemSpriteSheet.Glaive,
        "A polearm consisting of a sword blade on the end of a pole.")]
    [InlineData(typeof(ShortSword), "short sword", ItemSpriteSheet.ShortSword,
        "It is indeed quite short, just a few inches longer, than a dagger.")]
    public void NameImageAndDesc_AreTheJavas(Type type, string name, int image, string desc)
    {
        var weapon = (MeleeWeapon)Activator.CreateInstance(type);

        Assert.NotNull(weapon);
        Assert.Equal(name, weapon.Name());
        Assert.Equal(image, weapon.Image());
        Assert.Equal(desc, weapon.Desc());
    }

    [Fact]
    public void WarHammerMax_UsesFloatArithmetic_NotDouble()
    {
        // (int)((5*5 - 5 + 10) / 1.2f * 1f): 30 / 1.2f = 24.999998f, truncated to 24. The same formula in double gives 25.
        Assert.Equal(24, new WarHammer().Max());
    }

    [Fact]
    public void Upgrade_LowersStrByOne_AndRaisesMinByOneAndMaxByTier()
    {
        var glaive = new Glaive(); // tier 5, Str 18, min 5, max 30

        glaive.Upgrade(); // MeleeWeapon.upgrade() is upgrade(false)
        Assert.Equal(1, glaive.Level());
        Assert.Equal(17, glaive.Str); // 18 - 1
        Assert.Equal(6, glaive.Min()); // 5  + 1
        Assert.Equal(35, glaive.Max()); // 30 + 1 * 5

        glaive.Upgrade(false);
        Assert.Equal(16, glaive.Str); // 18 - 2
        Assert.Equal(7, glaive.Min()); // 5  + 2
        Assert.Equal(40, glaive.Max()); // 30 + 2 * 5

        var dagger = new Dagger(); // tier 1: STR 10, min 1, max 8
        dagger.Upgrade(2); // Item.upgrade(n) calls the virtual upgrade() twice
        Assert.Equal(8, dagger.Str); // 10 - 2
        Assert.Equal(3, dagger.Min()); // 1 + 2
        Assert.Equal(10, dagger.Max()); // 8 + 2 * 1
    }

    [Fact]
    public void Degrade_RaisesStrByOne_AndLowersMinByOneAndMaxByTier()
    {
        var glaive = new Glaive(); // tier 5: STR 18, min 5, max 30

        glaive.Degrade();
        Assert.Equal(-1, glaive.Level());
        Assert.Equal(19, glaive.Str); // 18 + 1
        Assert.Equal(4, glaive.Min()); // 5 - 1
        Assert.Equal(25, glaive.Max()); // 30 - 1 * 5

        var mace = new Mace(); // tier 3: STR 14, min 3, max 12
        mace.Degrade(2);
        Assert.Equal(16, mace.Str); // 14 + 2
        Assert.Equal(1, mace.Min()); // 3 - 2
        Assert.Equal(6, mace.Max()); // 12 - 2 * 3
    }

    [Fact]
    public void SafeUpgrade_WithoutAnEnchantment_UpgradesAndDrawsNothing()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var sword = new Sword();
            sword.SafeUpgrade();
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.Int(1000), next); // upgrade(enchantment != null) is upgrade(false)
            Assert.Equal(1, sword.Level());
            Assert.Equal(13, sword.Str); // 14 - 1
        }
    }

    [Fact]
    public void MinAndMax_WhenBroken_FallBackToTheBaseValues()
    {
        var sword = new Sword();
        sword.Upgrade(); // level 1: STR 13, durability 5 * (16 - 1) = 75
        for (var i = 0; i < 75; i++)
        {
            sword.Use();
        }

        Assert.True(sword.IsBroken());
        Assert.Equal(3, sword.Min()); // min0() = tier, not 3 + 1
        Assert.Equal(16, sword.Max()); // max0(), not 16 + 1 * 3
    }

    [Fact]
    public void Random_RollsLikeAWeapon_ThenKeepsTheEnchantRoll()
    {
        var enchantRollsHit = 0;

        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            var sword = new Sword();
            sword.Random();
            var next = Random.Int(1000);

            Random.Seed(seed);
            var level = WeaponRolls.ReplayWeaponRandom();
            if (Random.Int(10 + level) == 0)
            {
                Random.Chances(WeaponRolls.EnchantmentChances); // enchant -> Enchantment.random()
                enchantRollsHit++;
            }

            Assert.Equal(level, sword.Level());
            Assert.Equal(14 - level, sword.Str); // typical 14, one less per upgrade and one more per degrade
            Assert.Equal(level < 0, sword.Cursed);
            Assert.Equal(Random.Int(1000), next); // the enchant roll, and enchant()'s own roll, were drawn
        }

        // each seed hits the enchant roll with chance 1 in 13 at least
        Assert.True(enchantRollsHit > 0);
    }

    [Fact]
    public void Actions_ShortSwordAboveLevelZero_OmitTheDeferredReforge()
    {
        var sword = new ShortSword();
        sword.Upgrade(); // the Java adds AC_REFORGE once level() > 0

        Assert.Equal(10, sword.Str); // 11 - 1
        Assert.Equal(2, sword.Min()); // 1 + 1
        Assert.Equal(13, sword.Max()); // 12 + 1 * 1
        Assert.Equal(new[]
            {
                Item.AcDrop,
                Item.AcThrow,
                EquipableItem.AcEquip
            },
            sword.Actions(Dungeon.Hero));
        Assert.Equal("REFORGE", ShortSword.AcReforge);
    }

    [Fact]
    public void Bundle_RestoresStrThroughTheLevelReplay()
    {
        var dagger = new Dagger();
        dagger.Upgrade(2);

        var bundle = new Bundle();
        bundle.Put("item", dagger);
        var restored = Assert.IsType<Dagger>(bundle.Get("item"));

        Assert.Equal(2, restored.Level());
        Assert.Equal(8, restored.Str); // 10 - 2, replayed through upgrade()
        Assert.Equal(3, restored.Min()); // 1 + 2
        Assert.Equal(10, restored.Max()); // 8 + 2 * 1
    }
}