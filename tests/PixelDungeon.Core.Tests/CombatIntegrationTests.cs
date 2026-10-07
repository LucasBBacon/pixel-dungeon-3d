using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;

namespace PixelDungeon.Core.Tests;

public class CombatIntegrationTests : DungeonFixture
{
    [Fact]
    public void KillingARat_EarnsItsExperience()
    {
        LoadLevel("#####", "#<..#", "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();

        // The rat just became visible, so this first Handle interrupts itself (see
        // HeroCombatTests.Handle_OnAnAdjacentRat_SwingsThroughTheView)
        // prime it as a known visible enemy before the swing loop below relies on Handle acting.
        hero.Handle(rat.Pos);

        for (var i = 0; i < 40 && rat.IsAlive(); i++)
        {
            hero.Handle(rat.Pos);
            hero.OnAttackComplete();
        }

        Assert.False(rat.IsAlive());
        // A single sewers rat (EXP 1) at hero level 1 (needs 10 to level up): the
        // specific outcome, not the level-up branch of the brief's original
        // disjunction, which a level-up would zero out anyway.
        Assert.Equal(1, hero.Exp);
        Assert.Equal(1, hero.Lvl);
        Assert.Equal(1, Statistics.EnemiesSlain);
    }

    [Fact]
    public void DyingToARat_RecordsTheCauseAndShowsGameOver()
    {
        LoadLevel("#####", "#<..#", "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        hero.HP = 1;
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();

        for (var i = 0; i < 40 && hero.IsAlive(); i++)
        {
            rat.Attack(hero);
        }

        Assert.False(hero.IsAlive());
        Assert.Contains("marsupial rat", Dungeon.ResultDescription);
        Assert.Equal(1, View.GameOvers);
    }
}