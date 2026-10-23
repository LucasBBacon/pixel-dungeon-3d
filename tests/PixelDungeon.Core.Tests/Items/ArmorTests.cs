using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class ArmorTests : DungeonFixture
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

    [Theory]
    [InlineData(typeof(ClothArmor), 1, 9, 2)] // STR 7 + 1*2; DR 1 * (2 + 0)
    [InlineData(typeof(LeatherArmor), 2, 11, 4)] // STR 7 + 2*2; DR 2 * 2
    [InlineData(typeof(MailArmor), 3, 13, 6)] // STR 7 + 3*2; DR 3 * 2
    [InlineData(typeof(ScaleArmor), 4, 15, 8)] // STR 7 + 4*2; DR 4 * 2
    [InlineData(typeof(PlateArmor), 5, 17, 10)] // STR 7 + 5*2; DR 5 * 2
    public void Stats_AtLevelZero_MatchTheJava(Type type, int tier, int str, int dr)
    {
        var armor = (Armor)Activator.CreateInstance(type);

        Assert.NotNull(armor);
        Assert.Equal(tier, armor.Tier);
        Assert.Equal(str, armor.Str); // the constructor sets STR = typicalSTR()
        Assert.Equal(str, armor.TypicalStr());
        Assert.Equal(dr, armor.Dr()); // tier * (2 + effectiveLevel())
        Assert.Equal(dr, armor.TypicalDr()); // tier * 2
    }

    [Theory]
    [InlineData(typeof(ClothArmor), "cloth armor", ItemSpriteSheet.ArmorCloth,
        "This lightweight armor offers basic protection.")]
    [InlineData(typeof(LeatherArmor), "leather armor", ItemSpriteSheet.ArmorLeather,
        "Armor made from tanned monster hide. Not as light as cloth armor but provides better protection.")]
    [InlineData(typeof(MailArmor), "mail armor", ItemSpriteSheet.ArmorMail,
        "Interlocking metal links make for a tough but flexible suit of armor.")]
    [InlineData(typeof(ScaleArmor), "scale armor", ItemSpriteSheet.ArmorScale,
        "The metal scales sewn onto a leather vest create a flexible, yet protective armor.")]
    [InlineData(typeof(PlateArmor), "plate armor", ItemSpriteSheet.ArmorPlate,
        "Enormous plates of metal are joined together into a suit that provides unmatched protection to any adventurer strong enough to bear its staggering weight.")]
    public void NameImageAndDesc_AreTheJavas(Type type, string name, int image, string desc)
    {
        var armor = (Armor)Activator.CreateInstance(type);

        Assert.NotNull(armor);
        Assert.Equal(name, armor.Name());
        Assert.Equal(image, armor.Image());
        Assert.Equal(desc, armor.Desc());
    }

    [Fact]
    public void Actions_OfferEquipOrUnequip_ByWhetherTheArmorIsWorn()
    {
        var hero = Dungeon.Hero;
        var cloth = new ClothArmor();

        Assert.Equal(new[]
            {
                Item.AcDrop,
                Item.AcThrow,
                EquipableItem.AcEquip
            },
            cloth.Actions(hero));
        Assert.False(cloth.IsEquipped(hero));

        hero.Belongings.Armor = cloth;

        Assert.Equal(new[]
            {
                Item.AcDrop,
                Item.AcThrow,
                EquipableItem.AcUnequip
            },
            cloth.Actions(hero));
        Assert.True(cloth.IsEquipped(hero));
    }

    [Fact]
    public void Equip_WithNothingWorn_WearsItShowsItsTierAndSpendsTwoTurns()
    {
        var hero = PlaceSpy(2);
        hero.STR = 17; // strong enough for plate, so its weight never changes Speed()
        var view = new FakeCharView();
        hero.Sprite = view;
        var plate = new PlateArmor();
        plate.Collect();

        plate.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(plate, hero.Belongings.Armor);
        Assert.DoesNotContain(plate, hero.Belongings.Backpack.Items);
        Assert.Equal(5, hero.Tier()); // belongings.armor.tier
        Assert.Equal(new[] { 5 }, view.ArmorTiers); // updateArmor() once the slot is set
        Assert.True(plate.CursedKnown); // wearing reveals the curse state
        Assert.Equal(0, view.Bursts); // no curse: no shadow burst,
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndCursed); // no curse sound,
        Assert.Empty(View.Logs); // and no warning
        Assert.False(hero.Ready);
        Assert.Equal(2f, hero.TimeLeft); // time2equip: 2 / speed 1
    }

    [Fact]
    public void Equip_OerWornAmor_PacksTheOldOneAndSpendsFourTurns()
    {
        var hero = PlaceSpy(2);
        hero.STR = 13; // carries both armors, so neither changes Speed()
        var view = new FakeCharView();
        hero.Sprite = view;
        var cloth = new ClothArmor();
        hero.Belongings.Armor = cloth;
        var mail = new MailArmor();
        mail.Collect();

        mail.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(mail, hero.Belongings.Armor);
        Assert.Contains(cloth, hero.Belongings.Backpack.Items);
        Assert.DoesNotContain(mail, hero.Belongings.Backpack.Items);
        Assert.Equal(new[] { 0, 3 }, view.ArmorTiers); // the cloth's doUnequip shows no armor, then the mail's tier 3
        // the cloth comes off through doUnequip(hero, true, false): Spend(2 / speed) and no Next()
        // the mail then spends SpendAndNext(2 / speed). Spend accumulates: 2 + 2
        Assert.Equal(4f, hero.TimeLeft);
    }

    [Fact]
    public void Equip_WhileCrippled_SpendsTwoOverSpeedForEachArmor()
    {
        var hero = PlaceSpy(2);
        hero.STR = 13;
        Buff.Prolong<Cripple>(hero, Cripple.Duration); // Char.speed() halves: 1 * 0.5
        hero.Belongings.Armor = new ClothArmor();
        var mail = new MailArmor();
        mail.Collect();

        mail.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(mail, hero.Belongings.Armor);
        Assert.Equal(8f, hero.TimeLeft); // 2 / 0.5 for the cloth, then 2 / 0.5 for the mail
    }

    [Fact]
    public void Equip_Cursed_WearsItWithABurstTheCurseSoundAndTheConstrictWarning()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var cloth = new ClothArmor { Cursed = true, LevelKnown = true };
        cloth.Collect();

        cloth.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(cloth, hero.Belongings.Armor); // cursed armor still goes on
        Assert.True(cloth.CursedKnown);
        Assert.Equal(6, view.Bursts); // ShadowParticle.CURSE, 6
        Assert.Contains((Assets.SndCursed, 1f), View.Sounds);
        // Armor formats toString(), not name(); with the level known that is "%s :%d".
        Assert.Contains(("your cloth armor :9 constricts around you painfully", LogKind.Negative), View.Logs);
        Assert.Equal(new[] { 1 }, view.ArmorTiers);
    }

    [Fact]
    public void Equip_OverCursedArmor_KeepsItAndRepacksTheNewOne()
    {
        var hero = PlaceSpy(2);
        var view = new FakeCharView();
        hero.Sprite = view;
        var cloth = new ClothArmor { Cursed = true };
        hero.Belongings.Armor = cloth;
        var leather = new LeatherArmor();
        leather.Collect();

        leather.Execute(hero, EquipableItem.AcEquip);

        Assert.Same(cloth, hero.Belongings.Armor);
        Assert.Contains(leather, hero.Belongings.Backpack.Items); // doEquip's else branch: collect(backpack)
        Assert.Contains(("You can't remove cursed cloth armor!", LogKind.Warning), View.Logs);
        Assert.Empty(view.ArmorTiers); // updateArmor() runs only on success
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void Unequip_PacksTheArmorShowsNoTierAndSpendsTwoTurns()
    {
        var hero = PlaceSpy(2);
        var view = new FakeCharView();
        hero.Sprite = view;
        var cloth = new ClothArmor(); // STR 9 against the hero's 10
        hero.Belongings.Armor = cloth;

        cloth.Execute(hero, EquipableItem.AcUnequip);

        Assert.Null(hero.Belongings.Armor);
        Assert.Contains(cloth, hero.Belongings.Backpack.Items);
        Assert.Equal(0, hero.Tier());
        Assert.Equal(new[] { 0 }, view.ArmorTiers);
        Assert.False(hero.Ready);
        Assert.Equal(2f, hero.TimeLeft); // doUnequip(hero, true) is single: SpendAndNext(2 / speed)
    }

    [Fact]
    public void Drop_WornArmor_UnequipsThenDrops_ForTwoAndAHalfTurns()
    {
        var hero = PlaceSpy(2);
        var view = new FakeCharView();
        hero.Sprite = view;
        var cloth = new ClothArmor();
        hero.Belongings.Armor = cloth;

        cloth.Execute(hero, Item.AcDrop);

        Assert.Null(hero.Belongings.Armor);
        Assert.DoesNotContain(cloth, hero.Belongings.Backpack.Items);
        Assert.Same(cloth, Dungeon.Level.Heaps[hero.Pos].Peek());
        Assert.Equal(new[] { 0 }, view.ArmorTiers);
        // doUnequip(hero, false, false) spends 2 / speed with no Next(); Item.doDrop's
        // SpendAndNext(TIME_TO_DROP) then adds 0.5 and ends the action.
        Assert.Equal(2.5f, hero.TimeLeft);
    }

    [Fact]
    public void Upgrade_LowersStrByOne_AndAddsTheTierToDr()
    {
        var scale = new ScaleArmor(); // tier 4: STR 15, DR 8

        scale.Upgrade(); // Armor.upgrade() is upgrade(false)
        Assert.Equal(1, scale.Level());
        Assert.Equal(14, scale.Str); // 15 - 1
        Assert.Equal(12, scale.Dr()); // 4 * (2 + 1)

        scale.Upgrade(false);
        Assert.Equal(13, scale.Str); // 15 - 2
        Assert.Equal(16, scale.Dr()); // 4 * (2 + 2)

        var cloth = new ClothArmor(); // tier 1: STR 9, DR 2
        cloth.Upgrade(2); // Item.upgrade(n) calls the virtual upgrade() twice
        Assert.Equal(7, cloth.Str); // 9 - 2
        Assert.Equal(4, cloth.Dr()); // 1 * (2 + 2)
    }

    [Fact]
    public void Degrade_RaisesStrByOne_AndDrCanFallBelowZero()
    {
        var plate = new PlateArmor(); // tier 5: STR 17, DR 10

        plate.Degrade();
        Assert.Equal(-1, plate.Level());
        Assert.Equal(18, plate.Str); // 17 + 1
        Assert.Equal(5, plate.Dr()); // 5 * (2 - 1)

        var cloth = new ClothArmor();
        cloth.Degrade(3);
        Assert.Equal(12, cloth.Str); // 9 + 3
        Assert.Equal(-1, cloth.Dr()); // 1 * (2 - 3): DR() does not floor it; Hero.dr() does
    }

    [Fact]
    public void Dr_WhenBroken_CountsTheLevelAsZero()
    {
        var mail = new MailArmor();
        mail.Upgrade(); // level 1: STR 12, DR 9, durability 6 * (16 - 1) = 90
        for (var i = 0; i < 90; i++)
        {
            mail.Use();
        }

        Assert.True(mail.IsBroken());
        Assert.Equal(6, mail.Dr()); // effectiveLevel() is 0 while broken: 3 * (2 + 0)
        Assert.Equal(12, mail.Str); // breaking does not touch STR
    }

    [Fact]
    public void maxDurability_IsSixPerLevelBelowSixteen()
    {
        var cloth = new ClothArmor();

        Assert.Equal(96, cloth.MaxDurability(0)); // 6 * (16 - 0)
        Assert.Equal(102, cloth.MaxDurability(-1)); // 6 * (16 + 1)
        Assert.Equal(6, cloth.MaxDurability(15)); // 6 * (16 - 15)
        Assert.Equal(6, cloth.MaxDurability(16)); // 6 * 1
        Assert.Equal(6, cloth.MaxDurability(30)); // 6 * 1
        Assert.Equal(96, cloth.Durability()); // Item's constructor filled durability through the override
    }

    [Fact]
    public void SafeUpgrade_WithoutAGlyph_UpgradesAndDrawsNothing()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var mail = new MailArmor();
            mail.SafeUpgrade();
            var next = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.Int(1000), next); // upgrade(glyph != null) is upgrade(false)
            Assert.Equal(1, mail.Level());
            Assert.Equal(12, mail.Str); // 13 - 1
        }
    }

    [Fact]
    public void Upgrade_WithInscribe_DrawsGlyphsRandomsRoll_AndWithoutDrawsNothing()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            var inscribed = new MailArmor();
            inscribed.Upgrade(true);
            var afterInscribe = Random.Int(1000);

            Random.Seed(seed);
            var plain = new MailArmor();
            plain.Upgrade(false);
            var afterPlain = Random.Int(1000);

            Random.Seed(seed);
            Assert.Equal(Random.Int(1000), afterPlain); // upgrade(false) on armor without a glyph draws nothing

            Random.Seed(seed);
            Random.Chances(ArmorRolls.GlyphChances); // inscribe() -> Glyph.random()
            Assert.Equal(Random.Int(1000), afterInscribe);

            Assert.Equal(12, inscribed.Str); // both lower STR: 13 - 1
            Assert.Equal(12, plain.Str);
        }
    }

    [Fact]
    public void Random_RollsLikeAWeapon_ThenKeepsTheGlyphRoll()
    {
        var glyphRollsHit = 0;

        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            var scale = new ScaleArmor();
            scale.Random();
            var next = Random.Int(1000);

            Random.Seed(seed);
            var level = ArmorRolls.ReplayArmorRandom(out var inscribed);

            Assert.Equal(level, scale.Level());
            Assert.Equal(15 - level, scale.Str); // typical 15, one less per upgrade and one more per degrade
            Assert.Equal(level < 0, scale.Cursed);
            Assert.Equal(Random.Int(1000), next); // the glyph roll, and inscribe()'s own roll when it hit, were drawn

            if (inscribed)
            {
                glyphRollsHit++;
            }
        }

        // Each seed hits Random.Int(10) == 0 with chance 1 in 10; missing it in all 200 is 0.9^200, below 1e-9.
        Assert.True(glyphRollsHit > 0);
    }

    [Fact]
    public void Proc_TenthHiWhileUnknown_IdentifiesWithTheJavaMessage()
    {
        var leather = new LeatherArmor();
        var rat = new Rat();

        for (var i = 0; i < 9; i++)
        {
            leather.Proc(rat, Dungeon.Hero, 1);
        }

        Assert.False(leather.LevelKnown);
        Assert.Empty(View.Logs);

        leather.Proc(rat, Dungeon.Hero, 1); // hit 10: --hitsToKnow reaches 0

        Assert.True(leather.LevelKnown);
        // GLog.w with name() and then toString(), which now shows the strength: "%s :%d"
        Assert.Equal(
            ("you are now familiar enough with your leather armor to identify it. It is leather armor :11.",
                LogKind.Warning),
            Assert.Single(View.Logs));
    }

    [Fact]
    public void Proc_WithTheLevelKnown_CountsNoHits()
    {
        var cloth = new ClothArmor { LevelKnown = true };
        var rat = new Rat();

        for (var i = 0; i < 15; i++)
        {
            cloth.Proc(rat, Dungeon.Hero, 1);
        }

        var bundle = new Bundle();
        cloth.StoreInBundle(bundle);
        Assert.Equal(10, bundle.GetInt("unfamiliarity")); // still HITS_TO_KNOW
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Proc_ReturnsTheDamage_AndUsesTheArmor()
    {
        var cloth = new ClothArmor();
        cloth.Upgrade(); // level 1: durability 6 * (16 - 1) = 90

        Assert.Equal(7, cloth.Proc(new Rat(), Dungeon.Hero, 7)); // no glyph to change it
        Assert.Equal(89, cloth.Durability());
    }

    [Fact]
    public void Bundle_RoundTripsUnfamiliarity()
    {
        var cloth = new ClothArmor();
        var rat = new Rat();
        for (var i = 0; i < 3; i++)
        {
            cloth.Proc(rat, Dungeon.Hero, 1);
        }

        var bundle = new Bundle();
        bundle.Put("item", cloth);
        Assert.Equal(7, bundle.GetBundle("item").GetInt("unfamiliarity")); // HITS_TO_KNOW 10 - 3 hits

        var restored = Assert.IsType<ClothArmor>(bundle.Get("item"));
        var again = new Bundle();
        restored.StoreInBundle(again);
        Assert.Equal(7, again.GetInt("unfamiliarity"));
    }

    [Fact]
    public void Bundle_ZeroUnfamiliarity_RestoresTen()
    {
        var bundle = new Bundle();
        new ClothArmor().StoreInBundle(bundle);
        bundle.Put("unfamiliarity", 0);

        var restored = new ClothArmor();
        restored.RestoreFromBundle(bundle);
        var again = new Bundle();
        restored.StoreInBundle(again);

        Assert.Equal(10, again.GetInt("unfamiliarity")); // 0 restores to HITS_TO_KNOW
    }

    [Fact]
    public void ToString_KnownLevel_AddsTheStrength_AndBrokenPrefixesIt()
    {
        var leather = new LeatherArmor();
        Assert.Equal("leather armor", leather.ToString()); // level unknown: Item.toString()

        leather.LevelKnown = true;
        Assert.Equal("leather armor :11", leather.ToString()); // "%s :%d"

        leather.LevelKnown = false;
        leather.Upgrade(); // level 1: STR 10, durability 90
        for (var i = 0; i < 90; i++)
        {
            leather.Use();
        }

        leather.LevelKnown = true;

        Assert.True(leather.IsBroken());
        Assert.Equal("broken leather armor+1 :10", leather.ToString()); // "broken %s :%d"
    }

    [Fact]
    public void Info_UnknownLevel_GivesTypicalStats_AndWarnsWhenTooHeavy()
    {
        Assert.Equal("Interlocking metal links make for a tough but flexible suit of armor." +
                     "\n\nTypical mail armor provides damage absorption up to 6 points per attack " + // typicalDR 3 * 2
                     " and requires 13 points of strength. " + // the Java's doubled space; typicalSTR 7 + 3*2
                     "Probably this armor is too heavy for you. ", // 13 > the hero's 10
            new MailArmor().Info());
    }

    [Fact]
    public void Info_UnknownLevelLightEnough_OmitsTheWarning()
    {
        Assert.Equal(
            "This lightweight armor offers basic protection." +
            "\n\nTypical cloth armor provides damage absorption up to 2 points per attack " +
            " and requires 9 points of strength. ", // 9 <= 10: no warning
            new ClothArmor().Info());
    }

    [Fact]
    public void Info_KnownNotWornTooHeavy_WarnsThatWearingItWillSlowYou()
    {
        var leather = new LeatherArmor();
        leather.Identify();

        Assert.Equal(
            "Armor made from tanned monster hide. Not as light as cloth armor but provides better protection." +
            "\n\nThis leather armor provides damage absorption up to 4 points per attack. " + // max(DR() 2 * 2, 0)
            "\n\nBecause of your inadequate strength wearing this armor " + // STR 11 > 10, not worn
            "will decrease your movement speed and defense skill. ",
            leather.Info());
    }

    [Fact]
    public void Info_KnownWornTooHeavy_SaysSpeedAndDefenseAreDecreased()
    {
        var scale = new ScaleArmor();
        scale.Identify();
        Dungeon.Hero.Belongings.Armor = scale;

        Assert.Equal(
            "The metal scales sewn onto a leather vest create a flexible, yet protective armor." +
            "\n\nThis scale armor provides damage absorption up to 8 points per attack. " + // 4 * 2
            "\n\nBecause of your inadequate strength your " + // STR 15 > 10, worn
            "movement speed and defense skill is decreased. " +
            "\n\nYou are wearing the scale armor.",
            scale.Info());
    }

    [Fact]
    public void Info_WornAndCursed_SaysItCannotBeRemoved()
    {
        var cloth = new ClothArmor { Cursed = true };
        Dungeon.Hero.Belongings.Armor = cloth;

        Assert.Equal(
            "This lightweight armor offers basic protection." +
            "\n\nTypical cloth armor provides damage absorption up to 2 points per attack " +
            " and requires 9 points of strength. " +
            "\n\nYou are wearing the cloth armor, and because it is cursed, you are powerless to remove it.",
            cloth.Info());
    }

    [Fact]
    public void Info_KnownCurseNotWorn_WarnsOfMalevolentMagic()
    {
        var plate = new PlateArmor { Cursed = true, CursedKnown = true };

        Assert.Equal(
            "Enormous plates of metal are joined together into a suit that provides " +
            "unmatched protection to any adventurer strong enough to bear its staggering weight." +
            "\n\nTypical plate armor provides damage absorption up to 10 points per attack " + // 5 * 2
            " and requires 17 points of strength. " + // 7 + 5*2
            "Probably this armor is too heavy for you. " +
            "\n\nYou can feel a malevolent magic lurking within the plate armor.",
            plate.Info());
    }

    [Fact]
    public void Info_KnownWithNegativeDr_ShowsZero()
    {
        var cloth = new ClothArmor();
        cloth.Degrade(3); // STR 12, DR() 1 * (2 - 3) = -1
        cloth.LevelKnown = true;

        Assert.Equal(
            "This lightweight armor offers basic protection." +
            "\n\nThis cloth armor provides damage absorption up to 0 points per attack. " + // Math.max(DR(), 0)
            "\n\nBecause of your inadequate strength wearing this armor " + // STR 12 > 10
            "will decrease your movement speed and defense skill. ",
            cloth.Info());
    }

    [Fact]
    public void Price_DoublesPerTier_AndConsidersState()
    {
        Assert.Equal(10, new ClothArmor().Price()); // 10 * (1 << 0)
        Assert.Equal(20, new LeatherArmor().Price()); // 10 * (1 << 1)
        Assert.Equal(40, new MailArmor().Price()); // 10 * (1 << 2)
        Assert.Equal(80, new ScaleArmor().Price()); // 10 * (1 << 3)
        Assert.Equal(160, new PlateArmor().Price()); // 10 * (1 << 4)

        Assert.Equal(5,
            new ClothArmor { Cursed = true, CursedKnown = true }.Price()); // considerState halves a known curse

        var leather = new LeatherArmor();
        leather.Upgrade(2);
        leather.LevelKnown = true;
        Assert.Equal(60, leather.Price()); // known level 2: 20 * (2 + 1)

        var mail = new MailArmor();
        mail.Degrade();
        mail.LevelKnown = true;
        Assert.Equal(20, mail.Price()); // known level -1: 40 / (1 - -1)
    }
}