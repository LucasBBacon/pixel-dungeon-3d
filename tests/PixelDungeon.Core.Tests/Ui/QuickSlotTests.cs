using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Ui;

public class QuickSlotTests : DungeonFixture
{
    [Fact]
    public void SaveAndRestore_ForAnItem_RestoresBothSlotsToThatInstance()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = item;
        QuickSlot.SecondaryValue = item;
        var bundle = new Bundle();
        bundle.Put("item", item);

        QuickSlot.PrimaryValue = null;
        QuickSlot.SecondaryValue = null;
        var restored = (TestItem)bundle.Get("item");

        Assert.Same(restored, QuickSlot.PrimaryValue);
        Assert.Same(restored, QuickSlot.SecondaryValue);
    }

    [Fact]
    public void Save_ForAnUnslottedItem_WritesNoKeys()
    {
        var bundle = new Bundle();
        QuickSlot.Save(bundle, new TestItem());
        Assert.Empty(bundle.Fields());
    }

    [Fact]
    public void Compress_movesTheSecondaryIntoAnEmptyPrimary()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = null;
        QuickSlot.SecondaryValue = item;
        QuickSlot.Compress();
        Assert.Same(item, QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }

    [Fact]
    public void Compress_ClearsADuplicateSecondary()
    {
        var item = new TestItem();
        QuickSlot.PrimaryValue = item;
        QuickSlot.SecondaryValue = item;
        QuickSlot.Compress();
        Assert.Same(item, QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }

    [Fact]
    public void UpdateQuickslot_RefreshesOnlyWhenTheItemOrItsClassIsSlotted()
    {
        var stack = new StackItem(2);
        var single = new TestItem();

        stack.UpdateQuickslot();
        single.UpdateQuickslot();
        Assert.Equal(0, View.QuickSlotRefreshes);

        QuickSlot.PrimaryValue = typeof(StackItem);
        QuickSlot.SecondaryValue = single;
        stack.UpdateQuickslot();
        single.UpdateQuickslot();
        Assert.Equal(2, View.QuickSlotRefreshes);
    }

    [Fact]
    public void Target_IgnoresTheHero_AndRemembersAnyoneElse()
    {
        var rat = new Rat();
        QuickSlot.Target(new TestItem(), Dungeon.Hero);
        Assert.Null(QuickSlot.LastTarget);
        QuickSlot.Target(new TestItem(), rat);
        Assert.Same(rat, QuickSlot.LastTarget);
    }

    [Fact]
    public void DungeonInit_ClearsTheSecondSlot_AndSlotsTheDartClassFirst()
    {
        QuickSlot.PrimaryValue = new TestItem();
        QuickSlot.SecondaryValue = typeof(StackItem);
        Dungeon.Init();
        Assert.Same(typeof(Dart), QuickSlot.PrimaryValue); // initRogue's QuickSlot.primaryValue = Dart.class
        Assert.Null(QuickSlot.SecondaryValue);
    }

    [Fact]
    public void SaveAndRestore_ForAClass_KeepsItOnlyWhileTheHeroHoldsOne()
    {
        QuickSlot.PrimaryValue = typeof(StackItem);

        var empty = new Bundle();
        QuickSlot.Save(empty);
        Assert.False(empty.Contains("quickslot"));

        new StackItem(2).Collect();
        var bundle = new Bundle();
        QuickSlot.Save(bundle);
        QuickSlot.PrimaryValue = null;
        QuickSlot.SecondaryValue = new TestItem();

        QuickSlot.Restore(bundle);

        Assert.Same(typeof(StackItem), QuickSlot.PrimaryValue);
        Assert.Null(QuickSlot.SecondaryValue);
    }

    // ---- UseTargeting ----

    private static readonly string[] OpenRoom =
    [
        "#######",
        "#.....#",
        "#.....#",
        "#######"
    ];

    private static readonly string[] WalledRoom =
    [
        "########",
        "#..##..#",
        "########"
    ];

    private sealed class Dummy : Char
    {
        public Dummy()
        {
            HP = HT = 20;
        }

        protected override bool Act() => false;
    }

    private Hero PlaceIn(string[] rows)
    {
        LoadLevel(rows);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static Rat AddRat(int x, int y)
    {
        var rat = new Rat
        {
            Pos = TestLevel.At(x, y),
            Sprite = new FakeCharView()
        };
        GameScene.Add(rat);
        return rat;
    }

    private static void SeeEnemies(Hero hero) =>
        typeof(Hero)
            .GetMethod("CheckVisibleMobs", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(hero, null);

    [Fact]
    public void UseTargeting_AVisibleLastTargetInTheLineOfFire_IsKept()
    {
        var hero = PlaceIn(OpenRoom);
        var target = AddRat(5, 1);
        AddRat(2, 2); // a nearer enemy, also in line
        SeeEnemies(hero);

        Assert.True(QuickSlot.UseTargeting());
        Assert.Same(target, QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_ALastTargetBehindAnotherEnemy_SwitchesToTheEnemyInFront()
    {
        var hero = PlaceIn(OpenRoom);
        var front = AddRat(3, 1);
        var behind = AddRat(5, 1);
        SeeEnemies(hero);
        QuickSlot.LastTarget = behind;

        // Ballistica.cast(hero, behind, false, true) stops on the front rat's cell, so the behind rat is dropped
        // Whichever order VisibleEnemy lists them in, the front rat is the only one the line reaches
        Assert.True(QuickSlot.UseTargeting());
        Assert.Same(front, QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_ALastTargetBlockedByANonEnemy_IsDropped_AndNothingIsTargeted()
    {
        var hero = PlaceIn(OpenRoom);
        Actor.Add(new Dummy
        {
            Pos = TestLevel.At(3, 1),
            Sprite = new FakeCharView()
        });
        var target = AddRat(5, 1);
        SeeEnemies(hero);
        QuickSlot.LastTarget = target;

        Assert.False(QuickSlot.UseTargeting());
        Assert.Null(QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_WithNoLastTarget_TargetAVisibleEnemyInLine()
    {
        var hero = PlaceIn(OpenRoom);
        var rat = AddRat(4, 2);
        SeeEnemies(hero);

        Assert.True(QuickSlot.UseTargeting());
        Assert.Same(rat, QuickSlot.LastTarget);
        Assert.Null(HealthIndicator.Target); // targeting aims the slot, only target() moves the health bar
    }

    [Fact]
    public void UseTargeting_WithNoEnemiesInSight_TargetsNothing()
    {
        var hero = PlaceIn(WalledRoom);
        var rat = AddRat(5, 1);
        SeeEnemies(hero);
        Assert.False(Dungeon.Visible[rat.Pos]); // the wall at (3, 1) hides it

        Assert.False(QuickSlot.UseTargeting());
        Assert.Null(QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_AHiddenLastTarget_IsNotTargeted_ButStaysRemembered()
    {
        var hero = PlaceIn(WalledRoom);
        var rat = AddRat(5, 1);
        SeeEnemies(hero);
        QuickSlot.LastTarget = rat;

        Assert.False(QuickSlot.UseTargeting());
        Assert.Same(rat, QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_ADeadLastTarget_GivesWayToAVisibleEnemy()
    {
        var hero = PlaceIn(OpenRoom);
        var dead = new Rat { Pos = TestLevel.At(5, 1), HP = 0 };
        var live = AddRat(4, 2);
        SeeEnemies(hero);
        QuickSlot.LastTarget = dead;

        Assert.True(QuickSlot.UseTargeting());
        Assert.Same(live, QuickSlot.LastTarget);
    }

    [Fact]
    public void UseTargeting_ADeadLastTargetAndNoEnemy_TargetsNothing()
    {
        var hero = PlaceIn(OpenRoom);
        var dead = new Rat { Pos = TestLevel.At(5, 1), HP = 0 };
        SeeEnemies(hero);
        QuickSlot.LastTarget = dead;

        Assert.False(QuickSlot.UseTargeting());
        Assert.Same(dead, QuickSlot.LastTarget); // the Java leaves it, the view draws nothing while this returns false
    }

    [Fact]
    public void UseTargeting_ALastTargetNoLongerAmongTheActors_IsDropped()
    {
        var hero = PlaceIn(OpenRoom);
        var rat = AddRat(4, 1);
        SeeEnemies(hero);
        Actor.Remove(rat); // still alive, visible and in line, but no longer in Actor.All()
        QuickSlot.LastTarget = rat;

        Assert.False(QuickSlot.UseTargeting()); // Deviation, Java drops lastTarget but leaves its targeting flag on
        Assert.Null(QuickSlot.LastTarget);
    }
}