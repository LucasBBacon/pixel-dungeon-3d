using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class ScrollTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#######",
    ];

    private static readonly Type[] ScrollClasses =
    [
        typeof(ScrollOfIdentify),
        typeof(ScrollOfMagicMapping),
        typeof(ScrollOfRecharging),
        typeof(ScrollOfRemoveCurse),
        typeof(ScrollOfTeleportation),
        typeof(ScrollOfChallenge),
        typeof(ScrollOfTerror),
        typeof(ScrollOfLullaby),
        typeof(ScrollOfPsionicBlast),
        typeof(ScrollOfMirrorImage),
        typeof(ScrollOfUpgrade),
        typeof(ScrollOfEnchantment),
    ];

    private static readonly string[] Runes =
    [
        "KAUNAN",
        "SOWLIO",
        "LAGUZ",
        "YNGVI",
        "GYFU",
        "RAIDO",
        "ISAZ",
        "MANNAZ",
        "NAUDIZ",
        "BERKANAN",
        "ODAL",
        "TIWAZ"
    ];
    private static readonly int[] Images =
    [
        ItemSpriteSheet.ScrollKaunan,
        ItemSpriteSheet.ScrollSowilo,
        ItemSpriteSheet.ScrollLaguz,
        ItemSpriteSheet.ScrollYngvi,
        ItemSpriteSheet.ScrollGyfu,
        ItemSpriteSheet.ScrollRaido,
        ItemSpriteSheet.ScrollIsaz,
        ItemSpriteSheet.ScrollMannaz,
        ItemSpriteSheet.ScrollNaudiz,
        ItemSpriteSheet.ScrollBerkanan,
        ItemSpriteSheet.ScrollOdal,
        ItemSpriteSheet.ScrollTiwaz
    ];

    private Hero PlaceReader(string[] rows, int x)
    {
        LoadLevel(rows);
        var hero = PlaceHero(TestLevel.At(x, 1));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static T Carried<T>() where T : Scroll, new()
    {
        var scroll = new T();
        scroll.Collect();
        return scroll;
    }

    private static string[] ScrollNames() =>
        [.. ScrollClasses.Select(t => ((Scroll)Activator.CreateInstance(t)).Name())];
    
    // ---- the table ----

    [Fact]
    public void InitLabels_DrawsOneRunePerScrollInTheJavasOrder_WithTheImageAtTheSameIndex()
    {
        Random.Seed(9);
        Scroll.InitLabels();
        var nextDraw = Random.Int(1000);
        
        Random.Seed(9);
        var runesLeft = new List<string>(Runes);
        var imagesLeft = new List<int>(Images);
        foreach (var type in ScrollClasses)
        {
            var scroll = (Scroll)Activator.CreateInstance(type);
            var index = Random.Int(runesLeft.Count);
            Assert.NotNull(scroll);
            Assert.Equal("scroll \"" + runesLeft[index] + "\"", scroll.Name());
            Assert.Equal(imagesLeft[index], scroll.Image());
            runesLeft.RemoveAt(index);
            imagesLeft.RemoveAt(index);
        }
        
        Assert.Equal(Random.Int(1000), nextDraw); // exactly twelve draws
    }

    [Fact]
    public void DungeonReset_BuildsTheScrollTableBeforeThePotionTable()
    {
        Random.Seed(21);
        Scroll.InitLabels();
        Potion.InitColors();
        var scrollNames = ScrollNames();
        var potionName = new PotionOfHealing().Name();
        
        Random.Seed(21);
        Dungeon.Reset();
        
        Assert.Equal(scrollNames, ScrollNames());
        Assert.Equal(potionName, new PotionOfHealing().Name());
    }

    [Fact]
    public void DungeonInit_BuildsAFreshAScrollTable_AndTheRogueKnowsMagicMapping()
    {
        new ScrollOfIdentify().SetKnown();

        Dungeon.Init();
        
        Assert.False(new ScrollOfIdentify().IsKnown());
        Assert.Equal(new[] {typeof(ScrollOfMagicMapping)}, Scroll.GetKnown());
    }

    [Fact]
    public void AllKnown_OnlyOnceEveryScrollIsKnown_AndKnownAndUnknownFollowTheTable()
    {
        foreach (var type in ScrollClasses.Take(10))
        {
            ((Scroll)Activator.CreateInstance(type)).SetKnown();
        }
        Assert.False(Scroll.AllKnown());
        Assert.Equal(ScrollClasses.Skip(10), Scroll.GetUnknown());

        new ScrollOfUpgrade().SetKnown(); // 11th, handler knows the last one too
        
        Assert.True(Scroll.AllKnown());
        Assert.Equal(ScrollClasses, Scroll.GetKnown());
    }
    
    // ---- The Base ----

    [Fact]
    public void NameAndInfo_HideAnUnknownScroll_AndShowAKnownOne()
    {
        var scroll = new ScrollOfTeleportation();
        var name = scroll.Name();
        Assert.StartsWith("scroll \"", name);
        var rune = name.Substring("scroll \"".Length, name.Length - "scroll \"".Length - 1);
        Assert.Contains(rune, Runes);
        Assert.Equal(
            "This parchment is covered with indecipherable writing, and bears a title of rune " + rune + 
            ". Who knows what it will do when read aloud?", scroll.Info());
        Assert.False(scroll.IsIdentified());
        
        scroll.SetKnown();
        
        Assert.Equal("Scroll of Teleportation", scroll.Name());
        Assert.Equal(
            "The spell on this parchment instantly transports the reader to a random location on the dungeon level. " +
            "It can be used to escape a dangerous situation, but the unlucky reader might find himself in an even more dangerous place.",
            scroll.Info());
        Assert.True(scroll.IsIdentified());
        Assert.True(new ScrollOfTeleportation().IsKnown());
    }

    [Fact]
    public void Identify_KnowsTheClass_AndRunsTheItemIdentifyToo()
    {
        var scroll = new ScrollOfLullaby();
        
        Assert.Same(scroll, scroll.Identify());
        
        Assert.True(scroll.IsKnown());
        Assert.True(scroll.LevelKnown);
        Assert.True(scroll.CursedKnown);
    }

    [Fact]
    public void Defaults_StackReadByDefault_AndCannotBeUpgraded()
    {
        var scroll = new ScrollOfRecharging();
        
        Assert.True(scroll.Stackable);
        Assert.Equal(Scroll.AcRead, scroll.DefaultAction);
        Assert.Equal(new[] {Item.AcDrop, Item.AcThrow, Scroll.AcRead}, scroll.Actions(Dungeon.Hero));
        Assert.False(scroll.IsUpgradable());
    }

    [Theory]
    [InlineData(typeof(ScrollOfIdentify), 30)]
    [InlineData(typeof(ScrollOfMagicMapping), 25)]
    [InlineData(typeof(ScrollOfRecharging), 40)]
    [InlineData(typeof(ScrollOfRemoveCurse), 30)]
    [InlineData(typeof(ScrollOfTeleportation), 40)]
    [InlineData(typeof(ScrollOfChallenge), 15)]
    [InlineData(typeof(ScrollOfTerror), 50)]
    [InlineData(typeof(ScrollOfLullaby), 50)]
    [InlineData(typeof(ScrollOfPsionicBlast), 80)]
    [InlineData(typeof(ScrollOfMirrorImage), 15)]
    [InlineData(typeof(ScrollOfUpgrade), 15)]
    [InlineData(typeof(ScrollOfEnchantment), 15)]
    public void Price_IsFifteenEachWhileUnknown_AndTheClassPriceOnceKnown(Type type, int knownPrice)
    {
        var scroll = (Scroll)Activator.CreateInstance(type);
        Assert.NotNull(scroll);
        scroll.Quantity(2);
        Assert.Equal(30, scroll.Price());
        
        scroll.SetKnown();
        
        Assert.Equal(knownPrice * 2, scroll.Price());
    }

    [Fact]
    public void Read_WhileBlinded_WarnsAndLeavesTheScrollInThePack()
    {
        var hero = PlaceReader(Room, 1);
        var scroll = Carried<ScrollOfMagicMapping>();
        Buff.Affect<Blindness>(hero, 3f);

        scroll.Execute(hero);

        Assert.Contains(("You can't read a scroll while blinded", LogKind.Warning), View.Logs);
        Assert.Same(scroll, hero.Belongings.GetItem(typeof(ScrollOfMagicMapping)));
        Assert.Equal(0, ((FakeCharView)hero.Sprite).Reads);
    }

    [Fact]
    public void Read_DetachesOneSpendsATurnAndPlaysTheReadAnimation()
    {
        LoadLevel(Room);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var scrolls = new ScrollOfRecharging();
        scrolls.Quantity(2);
        scrolls.Collect();
        
        scrolls.Execute(hero);
        
        Assert.Equal(1, scrolls.Quantity());
        Assert.Equal(1f, hero.TimeLeft);
        Assert.False(hero.Ready);
        Assert.Equal(1, view.Reads);
        Assert.True(scrolls.IsKnown());
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndRead);
    }
    
    // ---- Each Scroll ----

    [Fact]
    public void MagicMapping_MapsTheLevelAndRevealsEverySecret_SparklingOnlyWhereTheHeroSees()
    {
        LoadLevel(
            "#########",
            "#...S.S.#",
            "#########"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var scroll = Carried<ScrollOfMagicMapping>();
        
        scroll.Execute(hero);
        
        Assert.Equal(Terrain.Door, Dungeon.Level.Map[TestLevel.At(4, 1)]);
        Assert.Equal(Terrain.Door, Dungeon.Level.Map[TestLevel.At(6, 1)]);
        Assert.True(Dungeon.Level.Mapped[TestLevel.At(7, 1)]);
        Assert.Contains((EffectKind.Discover, TestLevel.At(4, 1)), View.Effects); // in sight
        Assert.DoesNotContain((EffectKind.Discover, TestLevel.At(6, 1)), View.Effects); // hidden behind first secret door
        Assert.Equal(new[] {(TestLevel.At(4, 1), Terrain.SecretDoor)}, View.Discovered);
        Assert.Contains(("You are now aware of the level layout.", LogKind.Info), View.Logs);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndSecret);
        Assert.True(scroll.IsKnown());
        Assert.Equal(1, ((FakeCharView)hero.Sprite).Reads);
    }

    [Fact]
    public void MagicMapping_WithNoSecretInSight_PlaysNoSecretSound()
    {
        var hero = PlaceReader(Room, 1);
        var scroll = Carried<ScrollOfMagicMapping>();
        
        scroll.Execute(hero);
        
        Assert.True(Dungeon.Level.Mapped[TestLevel.At(5, 1)]);
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndSecret);
    }

    [Fact]
    public void Recharging_WithNoWands_SaysNothingHappens_AndBurstEnergy()
    {
        var hero = PlaceReader(Room, 1);
        var scroll = Carried<ScrollOfRecharging>();
        
        scroll.Execute(hero);

        Assert.Contains(("a surge of energy courses through your pack, but nothing happens", LogKind.Info), View.Logs);
        Assert.Contains((0xFFFFFFAAu, 15), ((FakeCharView)hero.Sprite).BurstLog);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void RemoveCurse_WithNothingCursed_SaysNothingHappens()
    {
        var hero = PlaceReader(Room, 1);
        var scroll = Carried<ScrollOfRemoveCurse>();

        scroll.Execute(hero);

        Assert.Contains(("Your pack glows with a cleansing light, but nothing happens.", LogKind.Info), View.Logs);
        Assert.DoesNotContain((0xFF449944u, 19), ((FakeCharView)hero.Sprite).BurstLog);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void Teleportation_MovesTheHeroToACellItCouldNotSee_AndSaysSo()
    {
        LoadLevel(
            "#######",
            "#.#...#",
            "#######"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var scroll = Carried<ScrollOfTeleportation>();
        
        scroll.Execute(hero);

        Assert.Contains(hero.Pos, new[] { TestLevel.At(3, 1), TestLevel.At(4, 1), TestLevel.At(5, 1) });
        Assert.Equal(new[] {hero.Pos}, view.Placed);
        Assert.Equal(1, view.FadeIns);
        Assert.Contains(("In a blink of an eye you were teleported to another location of the level.", LogKind.Info), View.Logs);
        Assert.True(Dungeon.Visible[hero.Pos]);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void Teleportation_WhenNoRespawnCellIsFound_WarnsAfterElevenTries()
    {
        var level = LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var tries = 0;
        level.RespawnCells = () =>
        {
            tries++;
            return -1;
        };
        var scroll = Carried<ScrollOfTeleportation>();
        
        scroll.Execute(hero);
        
        Assert.Equal(11, tries);
        Assert.Equal(TestLevel.At(1, 1), hero.Pos);
        Assert.Contains(("Strong magic aura of this place prevents you from teleporting!", LogKind.Warning), View.Logs);
        Assert.True(scroll.IsKnown());
    }
    
    // ---- hooks ----

    [Fact]
    public void HeroSearch_RevealingASecretInSight_ShowsTheDiscoverEffect()
    {
        var hero = PlaceReader(
            [
                "#####",
                "#.S.#",
                "#####"
            ], 1
        );

        hero.Search(true);
        
        Assert.Equal(Terrain.Door, Dungeon.Level.Map[TestLevel.At(2, 1)]);
        Assert.Contains((EffectKind.Discover, TestLevel.At(2, 1)), View.Effects);
    }

    [Theory]
    [InlineData(typeof(ScrollOfUpgrade), true, LogKind.Positive)]
    [InlineData(typeof(ScrollOfUpgrade), false, LogKind.Info)]
    [InlineData(typeof(ScrollOfEnchantment), true, LogKind.Positive)]
    [InlineData(typeof(PotionOfStrength), true, LogKind.Positive)]
    [InlineData(typeof(PotionOfMight), true, LogKind.Positive)]
    [InlineData(typeof(PotionOfHealing), true, LogKind.Info)]
    public void PickingUp_AKnownUpgradeEnchantmentStrengthOrMight_IsLoggedAsPositive(Type type, bool known,
        LogKind kind)
    {
        var hero = PlaceReader(Room, 1);
        var item = (Item)Activator.CreateInstance(type);
        Assert.NotNull(item);
        if (known)
        {
            item.Identify();
        }

        Dungeon.Level.Drop(item, hero.Pos);

        hero.Handle(hero.Pos);
        
        Assert.Equal(kind, View.Logs.Single(l => l.Text.StartsWith("You now have")).Kind);
    }

    [Fact]
    public void BelongingsUncurseEquipped_UncursesOnlyTheWornItems()
    {
        var hero = PlaceReader(Room, 1);
        hero.Belongings.Weapon = new Dagger { Cursed = true };
        hero.Belongings.Armor = new ClothArmor { Cursed = true };
        var packed = new TestItem { Cursed = true };
        packed.Collect();

        hero.Belongings.UncurseEquipped();
        
        Assert.False(hero.Belongings.Weapon.Cursed);
        Assert.False(hero.Belongings.Armor.Cursed);
        Assert.True(packed.Cursed);
        Assert.Contains((0xFF440044u, 10), ((FakeCharView)hero.Sprite).BurstLog);
    }
}