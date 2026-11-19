using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class InventoryScrollTests : DungeonFixture
{
    private SpyHero PlaceReader()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static T Carried<T>() where T : Scroll, new()
    {
        var scroll = new T();
        scroll.Collect();
        return scroll;
    }

    [Theory]
    [InlineData(typeof(ScrollOfIdentify), BagMode.Unidentified, "Select an item to identify")]
    [InlineData(typeof(ScrollOfUpgrade), BagMode.Upgradable, "Select an item to upgrade")]
    [InlineData(typeof(ScrollOfEnchantment), BagMode.Enchantable, "Select an enchantable item")]
    public void Read_OpensTheSelectionInTheScrollModeWithItsTitle(Type type, BagMode mode, string title)
    {
        var hero = PlaceReader();
        var scroll = (Scroll)Activator.CreateInstance(type);
        Assert.NotNull(scroll);
        scroll.Collect();
        
        scroll.Execute(hero);

        var selection = Assert.Single(View.ItemSelections);
        Assert.Equal(mode, selection.Mode);
        Assert.Equal(title, selection.Title);
    }

    [Fact]
    public void Read_AnUnknownIdentify_LearnsItAndOpensTheSelection_WithoutSpendingATurn()
    {
        var hero = PlaceReader();
        var scroll = Carried<ScrollOfIdentify>();
        
        scroll.Execute(hero);
        
        Assert.True(scroll.IsKnown());
        Assert.Single(View.ItemSelections);
        Assert.Null(hero.Belongings.GetItem(typeof(ScrollOfIdentify)));
        Assert.Equal(0f, hero.TimeLeft);
        Assert.Equal(0, ((FakeCharView)hero.Sprite).Reads);
    }

    [Fact]
    public void ChoosingAnItem_IdentifiesIt_ThenReadsTheScroll()
    {
        var hero = PlaceReader();
        var item = new TestItem();
        item.Collect();
        var scroll = Carried<ScrollOfIdentify>();
        scroll.Execute(hero);

        View.ChooseItem(item);

        Assert.True(item.LevelKnown);
        Assert.True(item.CursedKnown);
        Assert.Contains(("It is test item", LogKind.Info), View.Logs);
        Assert.Contains((0xFF4488CCu, 5), ((FakeCharView)hero.Sprite).BurstLog); // the Identification effect
        Assert.Equal(1, ((FakeCharView)hero.Sprite).Reads);
        Assert.Equal(1f, hero.TimeLeft);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndRead);
    }

    [Fact]
    public void CancellingAScrollLearntByReading_AsksToConfirm_AndYesSpendsTheTurn()
    {
        var hero = PlaceReader();
        var scroll = Carried<ScrollOfUpgrade>();
        scroll.Execute(hero);

        View.ChooseItem(null);

        var request = Assert.Single(View.Windows);
        Assert.Equal("Scroll of Upgrade", request.Title);
        Assert.Equal("Do you really want to cancel this scroll usage? It will be consumed anyway.", request.Body);
        Assert.Equal(new[] { "Yes, I'm positive", "No, I changed my mind" }, request.Options);
        Assert.True(request.IgnoresBack); // the Java's onBackPressed() does nothing
        Assert.Equal(0f, hero.TimeLeft);

        request.Select(0);

        Assert.Equal(1f, hero.TimeLeft);
        Assert.Null(hero.Belongings.GetItem(typeof(ScrollOfUpgrade))); // consumed anyway
        Assert.Single(View.ItemSelections);
    }

    [Fact]
    public void CancellingAScrollLearntByReading_AndAnsweringNo_OpensTheSelectionAgain()
    {
        var hero = PlaceReader();
        var scroll = Carried<ScrollOfUpgrade>();
        scroll.Execute(hero);
        View.ChooseItem(null);

        Assert.Single(View.Windows).Select(1);

        Assert.Equal(2, View.ItemSelections.Count);
        Assert.Equal(BagMode.Upgradable, View.ItemSelections[1].Mode);
        Assert.Equal("Select an item to upgrade", View.ItemSelections[1].Title);
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void CancellingAKnownScroll_PutsItBackInThePack()
    {
        var hero = PlaceReader();
        var scroll = Carried<ScrollOfIdentify>();
        scroll.SetKnown();
        scroll.Execute(hero);

        View.ChooseItem(null);

        Assert.Empty(View.Windows);
        Assert.Same(scroll, hero.Belongings.GetItem(typeof(ScrollOfIdentify)));
        Assert.Equal(0f, hero.TimeLeft);
    }

    [Fact]
    public void Upgrade_ChoosingACursedItem_UncursesAndUpgradesIt()
    {
        var hero = PlaceReader();
        var view = (FakeCharView)hero.Sprite;
        var weapon = new TestWeapon { Cursed = true };
        weapon.Collect();
        var scroll = Carried<ScrollOfUpgrade>();
        scroll.Execute(hero);

        View.ChooseItem(weapon);

        Assert.Equal(1, weapon.Level());
        Assert.False(weapon.Cursed);
        Assert.Contains((0xFF440044u, 10), view.BurstLog); // ScrollOfRemoveCurse.uncurse found a curse
        Assert.Contains((0xFFFFDD44u, 3), view.BurstLog);  // ScrollOfUpgrade.upgrade(curUser)
        Assert.Contains(("your test weapon certainly looks better now", LogKind.Positive), View.Logs);
        Assert.Equal(1f, hero.TimeLeft);
    }

    [Fact]
    public void Upgrade_ChoosingABrokenItem_RepairsItWithoutRaisingItsLevel()
    {
        var hero = PlaceReader();
        var weapon = new TestWeapon();
        weapon.Upgrade();
        for (var uses = weapon.MaxDurability(); uses > 0; uses--)
        {
            weapon.Use();
        }
        Assert.True(weapon.IsBroken());
        weapon.Collect();
        var scroll = Carried<ScrollOfUpgrade>();
        scroll.Execute(hero);

        View.ChooseItem(weapon);

        Assert.False(weapon.IsBroken());
        Assert.Equal(1, weapon.Level());
    }

    [Fact]
    public void Enchantment_ChoosingAWeapon_UncursesAndRepairsIt_KeepingTheEnchantmentRoll()
    {
        var hero = PlaceReader();
        var weapon = new TestWeapon { Cursed = true };
        weapon.Collect();
        var scroll = Carried<ScrollOfEnchantment>();
        scroll.Execute(hero);

        Random.Seed(8);
        Random.Chances(WeaponRolls.EnchantmentChances); // Weapon.Enchant() keeps Enchantment.random()'s one draw
        var nextDraw = Random.Int(1000);
        Random.Seed(8);

        View.ChooseItem(weapon);

        Assert.Equal(nextDraw, Random.Int(1000));
        Assert.False(weapon.Cursed);
        Assert.Equal(weapon.MaxDurability(), weapon.Durability());
        Assert.Contains(("your test weapon glows in the dark", LogKind.Warning), View.Logs);
        Assert.Contains((0xFFFFFFFFu, 5), ((FakeCharView)hero.Sprite).BurstLog);
    }

    [Fact]
    public void Enchantment_ChoosingArmour_KeepsTheGlyphRoll()
    {
        var hero = PlaceReader();
        var armor = new ClothArmor { Cursed = true };
        armor.Collect();
        var scroll = Carried<ScrollOfEnchantment>();
        scroll.Execute(hero);

        Random.Seed(8);
        Random.Chances(ArmorRolls.GlyphChances); // Armor.Inscribe() keeps Glyph.random()'s one draw
        var nextDraw = Random.Int(1000);
        Random.Seed(8);

        View.ChooseItem(armor);

        Assert.Equal(nextDraw, Random.Int(1000));
        Assert.False(armor.Cursed);
        Assert.Contains(("your cloth armor glows in the dark", LogKind.Warning), View.Logs);
    }
}