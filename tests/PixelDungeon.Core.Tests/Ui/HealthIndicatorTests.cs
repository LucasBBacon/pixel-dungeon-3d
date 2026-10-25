using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Items;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Tests.Ui;

public class HealthIndicatorTests : DungeonFixture
{
    [Fact]
    public void SetTarget_ALivingChar_BecomesTheTarget()
    {
        var rat = new Rat();

        HealthIndicator.SetTarget(rat);

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void SetTarget_ADeadChar_ClearsTheTarget()
    {
        HealthIndicator.SetTarget(new Rat());

        HealthIndicator.SetTarget(new Rat { HP = 0 });

        Assert.Null(HealthIndicator.Target);
    }

    [Fact]
    public void SetTarget_Null_ClearsTheTarget()
    {
        HealthIndicator.SetTarget(new Rat());

        HealthIndicator.SetTarget(null);

        Assert.Null(HealthIndicator.Target);
    }

    [Fact]
    public void SetTarget_ATargetDiesLater_StaysTheTargetForTheViewToHide()
    {
        var rat = new Rat();
        HealthIndicator.SetTarget(rat);

        rat.HP = 0; // HealthIndicator.update() hides the bar of dead target, only target(ch) clears it

        Assert.Same(HealthIndicator.Target, rat);
    }

    [Fact]
    public void AttackIndicatorTarget_SetsTheHealthTarget()
    {
        var rat = new Rat();

        AttackIndicator.Target(rat);

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void QuickSlotTarget_AnotherChar_SetsHealthTarget()
    {
        var rat = new Rat();

        QuickSlot.Target(new TestItem(), rat);

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void QuickSlotTarget_TheHero_LeavesTheHealthTargetAlone()
    {
        var rat = new Rat();
        HealthIndicator.SetTarget(rat);

        QuickSlot.Target(new TestItem(), Dungeon.Hero);

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void QuickSlotTarget_Nobody_ClearsTheHealthTarget()
    {
        HealthIndicator.SetTarget(new Rat());

        QuickSlot.Target(new TestItem(), null);

        Assert.Null(HealthIndicator.Target);
    }

    [Fact]
    public void Target_TheLastCallFromEitherIndicatorWins()
    {
        var first = new Rat();
        var second = new Rat();

        AttackIndicator.Target(first);
        QuickSlot.Target(new TestItem(), second);
        Assert.Same(second, HealthIndicator.Target);

        AttackIndicator.Target(first);
        Assert.Same(first, HealthIndicator.Target);
    }

    [Fact]
    public void OnAttackComplete_MakesTheEnemyTheHealthTarget()
    {
        LoadLevel("#####", "#...#", "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();
        hero.Handle(rat
            .Pos); // first sight of the rat interrupts, as in HeroItemTests.OnAttackComplete_MakesTheEnemyTheAttackTarget
        hero.Handle(rat.Pos);
        Assert.Null(HealthIndicator.Target); // AttackIndicator.UpdateState picks a target without calling target()

        hero.OnAttackComplete();

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void Throw_AtAMob_MakesItTheHealthTarget()
    {
        LoadLevel("#######", "#.....#", "#######");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var rat = new Rat { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        var item = new TestItem();
        item.Collect();

        item.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(5, 1)); // the throw's Ballistica stops on the rat

        Assert.Same(rat, HealthIndicator.Target);
    }

    [Fact]
    public void Throw_AtAnEmptyCell_ClearsTheHealthTarget()
    {
        LoadLevel("#######", "#.....#", "#######");
        var hero = PlaceHero(TestLevel.At(1, 1));
        HealthIndicator.SetTarget(new Rat());
        var item = new TestItem();
        item.Collect();

        item.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.Null(HealthIndicator.Target);
        Assert.Null(QuickSlot.LastTarget);
    }

    [Fact]
    public void DungeonReset_ClearsTheHealthTarget()
    {
        HealthIndicator.SetTarget(new Rat());

        Dungeon.Reset();

        Assert.Null(HealthIndicator.Target);
    }
}