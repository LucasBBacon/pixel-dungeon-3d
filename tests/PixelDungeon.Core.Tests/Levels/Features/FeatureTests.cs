using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Tests.View;

namespace PixelDungeon.Core.Tests.Levels.Features;

public class FeatureTests : DungeonFixture
{
    private sealed class Rat : Mob
    {
        protected override bool Act() => false;
    }

    [Fact]
    public void Press_HighGrass_TramplesAndObserves()
    {
        var level = LoadLevel(
            "#####",
            "#.\"#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var observes = View.ObserveCount;
        level.Press(TestLevel.At(2, 1), hero);
        Assert.Equal(Terrain.Grass, level.Map[TestLevel.At(2, 1)]);
        Assert.Contains(TestLevel.At(2, 1), View.UpdatedCells);
        Assert.Equal(observes + 1, View.ObserveCount);
    }

    [Fact]
    public void Press_Door_OpensIt()
    {
        var level = LoadLevel(
            "#####",
            "#.+.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        level.Press(TestLevel.At(2, 1), hero);
        Assert.Equal(Terrain.OpenDoor, level.Map[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void Press_SecretTrap_ClicksDeactivatesAndInterruptsTheHero()
    {
        var level = LoadLevel(
            "#####",
            "#.^.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.CurAction = new HeroAction.Move(TestLevel.At(3, 1));
        level.Press(TestLevel.At(2, 1), hero);
        Assert.True(View.Logged("hidden pressure plate"));
        Assert.Equal(Terrain.InactiveTrap, level.Map[TestLevel.At(2, 1)]);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndTrap);
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Press_VisibleTrap_DeactivatesWithoutTheClick()
    {
        var level = LoadLevel(
            "#####",
            "#.T.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        level.Press(TestLevel.At(2, 1), hero);
        Assert.False(View.Logged("hidden pressure plate"));
        Assert.Equal(Terrain.InactiveTrap, level.Map[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void MobPress_TrapsDeactivate_DoorsOpen_AndPitsKill()
    {
        var level = LoadLevel(
            "######",
            "#.T+x#",
            "####x#",
            "######"
        );
        PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        var rat = new Rat { Pos = TestLevel.At(2, 1), HP = 5, HT = 5, Sprite = view };
        Actor.Add(rat);
        level.MobPress(rat);
        Assert.Equal(Terrain.InactiveTrap, level.Map[TestLevel.At(2, 1)]);

        rat.Pos = TestLevel.At(3, 1);
        level.MobPress(rat);
        Assert.Equal(Terrain.OpenDoor, level.Map[TestLevel.At(3, 1)]);

        rat.Pos = TestLevel.At(4, 1);
        level.MobPress(rat);
        Assert.DoesNotContain(rat, Actor.All());
        Assert.True(view.Died);
    }

    [Fact]
    public void Press_PitUnderTheHero_StartsAFall()
    {
        var level = LoadLevel(
            "###",
            "#x#",
            "#x#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 2));
        hero.CurAction = new HeroAction.Move(TestLevel.At(1, 1));
        level.Press(TestLevel.At(1, 2), hero);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndFalling);
        Assert.Equal(InterlevelMode.Fall, Interlevel.Mode);
        Assert.False(Interlevel.FallIntoPit);
        Assert.Equal(new[] { InterlevelMode.Fall }, View.SwitchedModes);
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Press_PitUnderADeadHero_JustHidesTheSprite()
    {
        var level = LoadLevel(
            "###",
            "#x#",
            "#x#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 2));
        var view = new FakeCharView();
        hero.Sprite = view;
        hero.HP = 0;
        level.Press(TestLevel.At(1, 2), hero);
        Assert.False(view.Visible);
        Assert.Empty(View.SwitchedModes);
    }

    [Fact]
    public void HeroJump_AsksFirst_AndYesResumesWithTheJumpConfirmed()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.LastAction = new HeroAction.Move(TestLevel.At(1, 2));
        Chasm.HeroJump(hero);
        Assert.Single(View.Windows);
        Assert.True(View.Windows[0].IsChoice);
        Assert.False(Chasm.JumpConfirmed);

        View.Windows[0].Select(1);
        Assert.False(Chasm.JumpConfirmed);

        View.Windows[0].Select(0);
        Assert.True(Chasm.JumpConfirmed);
        Assert.Null(hero.LastAction); // resume consumed it
        Chasm.JumpConfirmed = false;
    }

    [Fact]
    public void HeroLand_DealsAThirdToHalfOfMaxHP()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        for (var i = 0; i < 20; i++)
        {
            hero.HP = 20;
            Chasm.HeroLand();
            Assert.InRange(20 - hero.HP, 6, 10);
        }

        Assert.NotEmpty(View.Shakes);
        Assert.True(view.Bursts > 0);
    }

    [Fact]
    public void Sign_Read_ShowsTheTipForTheDepth()
    {
        Dungeon.Depth = 1;
        Sign.Read(0);
        Assert.Single(View.Windows);
        Assert.Single(View.Windows[0].Options);
        Assert.StartsWith("Don't overestimate your strength", View.Windows[0].Body);
    }

    [Fact]
    public void Sign_Read_PastTheTips_BUrnsTheSign()
    {
        var level = LoadLevel(
            "#####",
            "#.!.#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 22;
        Sign.Read(TestLevel.At(2, 1));
        Assert.Empty(View.Windows);
        Assert.Equal(Terrain.Embers, level.Map[TestLevel.At(2, 1)]);
        Assert.Contains((TestLevel.At(2, 1), Terrain.Sign), View.Discovered);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndBurning);
        Assert.True(View.Logged("greenish flames"));
    }
}