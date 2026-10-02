using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Hero;

public class HeroTests : DungeonFixture
{
    private sealed class SpyHero : PixelDungeon.Core.Actors.Hero.Hero
    {
        public float TimeLeft => Cooldown();
    }

    private sealed class Rat : Mob
    {
        protected override bool Act() => false;
    }

    private sealed class CountingDoom : Core.Actors.Hero.Hero.IDoom
    {
        public int Deaths;
        public void OnDeath() => Deaths++;
    }

    private static readonly string[] Stairs =
    [
        "#####",
        "#<.>#",
        "#####"
    ];

    [Fact]
    public void Handle_PicksAscendDescendOrMoveByCell_AndClearsReady()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(2, 1));
        hero.Ready = true;

        Assert.True(hero.Handle(TestLevel.At(1, 1)));
        Assert.IsType<HeroAction.Ascend>(hero.CurAction);
        Assert.Equal(TestLevel.At(1, 1), hero.Pos);
        Assert.False(hero.Ready);

        hero.Pos = TestLevel.At(2, 1);
        Assert.True(hero.Handle(TestLevel.At(3, 1)));
        Assert.IsType<HeroAction.Descend>(hero.CurAction);
        Assert.Equal(TestLevel.At(3, 1), hero.Pos);

        hero.Pos = TestLevel.At(1, 1);
        Actor.FreeCell(TestLevel.At(2, 1)); // Process() rebuilds occupancy per turn, test currently moves hero by hand
        Assert.True(hero.Handle(TestLevel.At(2, 1)));
        Assert.IsType<HeroAction.Move>(hero.CurAction);
        Assert.Null(hero.LastAction);

        Assert.False(hero.Handle(-1));
    }

    [Fact]
    public void Move_IntoAWall_DoesNothingAndBecomesReady()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(2, 1));
        Assert.False(hero.Handle(TestLevel.At(2, 0)));
        Assert.Equal(TestLevel.At(2, 1), hero.Pos);
        Assert.True(hero.Ready);
        Assert.Null(hero.CurAction);
        Assert.Equal(1, View.ReadyCount);
    }

    [Fact]
    public void Move_OneStep_TellsTheSprite_PlaysAStep_AndSpendsOneTurn()
    {
        LoadLevel(
            "#####",
            "#..~#",
            "#####"
        );
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;

        hero.Handle(TestLevel.At(2, 1));
        Assert.Equal((TestLevel.At(1, 1), TestLevel.At(2, 1)), view.Moves[0]);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndStep);
        Assert.Equal(1f, hero.TimeLeft);

        hero.Handle(TestLevel.At(3, 1));
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndWater);
    }

    [Fact]
    public void Move_FarTarget_NeedsVisitedOrMappedCells()
    {
        var level = LoadLevel(
            "########",
            "#......#",
            "########"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        Array.Fill(level.Visited, false);
        Array.Fill(level.Mapped, false);

        Assert.False(hero.Handle(TestLevel.At(5, 1)));
        Assert.Equal(TestLevel.At(1, 1), hero.Pos);

        Array.Fill(level.Mapped, true);
        Assert.True(hero.Handle(TestLevel.At(5, 1)));
        Assert.Equal(TestLevel.At(2, 1), hero.Pos);
        Assert.IsType<HeroAction.Move>(hero.CurAction);
    }

    [Fact]
    public void Search_Intentional_RevealsASecretDoor_AndSpendsTwoOrFourTurns()
    {
        var level = LoadLevel(
            "#####",
            "#.S.#",
            "#####"
        );
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;

        Assert.True(hero.Search(true));
        Assert.Equal(Terrain.Door, level.Map[TestLevel.At(2, 1)]);
        Assert.Contains((TestLevel.At(2, 1), Terrain.SecretDoor), View.Discovered);
        Assert.Contains(TestLevel.At(2, 1), View.UpdatedCells);
        Assert.Contains(View.Effects, e => e.Kind == EffectKind.CheckedCell);
        Assert.True(View.Logged("You noticed something"));
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndSecret);
        Assert.Contains((StatusColor.Default, "search"), view.Statuses);
        Assert.Contains(TestLevel.At(1, 1), view.Operations);
        Assert.True(hero.TimeLeft is 2f or 4f);
        Assert.False(hero.Ready);
    }

    [Fact]
    public void Search_Unintentional_UsesAwareness()
    {
        var level = LoadLevel(
            "#####",
            "#.S.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Awareness = 0f;
        Assert.False(hero.Search(false));
        Assert.Equal(Terrain.SecretDoor, level.Map[TestLevel.At(2, 1)]);
        hero.Awareness = 1f;
        Assert.True(hero.Search(false));
        Assert.Equal(Terrain.Door, level.Map[TestLevel.At(2, 1)]);
        Assert.Empty(View.Effects);
    }

    [Fact]
    public void Rest_SpendsATurn_AndRestUntilHealthyStopsAtFullHP()
    {
        LoadLevel(Stairs);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;

        hero.Rest(false);
        Assert.Equal(1f, hero.TimeLeft);
        Assert.Contains((StatusColor.Default, "..."), view.Statuses);
        Assert.False(hero.RestoreHealth);

        hero.HP = hero.HT - 1;
        hero.Rest(true);
        Assert.True(hero.RestoreHealth);
        Assert.False(hero.Ready);
        hero.Resume(); // acts with no action and HP belows HT keeps resting
        Assert.True(hero.RestoreHealth);
        Assert.False(hero.Ready);

        hero.HP = hero.HT;
        hero.Resume(); // full HP ends the rest
        Assert.True(hero.Ready);
    }

    [Fact]
    public void ANewVisibleHostileMob_InterruptsTheCurrentAction()
    {
        var level = LoadLevel(
            "#######",
            "#.....#",
            "#######"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var rat = new Rat { Pos = TestLevel.At(4, 1), HP = 1, HT = 1 };
        level.Mobs.Add(rat);
        Actor.Add(rat);

        hero.LastAction = new HeroAction.Move(TestLevel.At(3, 1));
        hero.RestoreHealth = true;
        hero.Resume();
        Assert.Null(hero.CurAction);
        Assert.IsType<HeroAction.Move>(hero.LastAction);
        Assert.False(hero.RestoreHealth);
        Assert.Equal(1, hero.VisibleEnemies());

        hero.Resume(); // the rat is now known, so the move proceeds
        Assert.Equal(TestLevel.At(2, 1), hero.Pos);
    }

    [Fact]
    public void Ascend_AtDepthOne_ShowsTheLeaveMessage_AndDeeperSwitchesLevel()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 1;
        Assert.False(hero.Handle(TestLevel.At(1, 1)));
        Assert.Single(View.Windows);
        Assert.Equal("One does not simply leave Pixel Dungeon.", View.Windows[0].Body);
        Assert.True(hero.Ready);
        Assert.Empty(View.SwitchedModes);

        Dungeon.Depth = 2;
        Assert.False(hero.Handle(TestLevel.At(1, 1)));
        Assert.Equal(InterlevelMode.Ascend, Interlevel.Mode);
        Assert.Equal(new[] { InterlevelMode.Ascend }, View.SwitchedModes);
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Descend_AtTheExit_SwitchesLevel()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(3, 1));
        Assert.False(hero.Handle(TestLevel.At(3, 1)));
        Assert.Equal(InterlevelMode.Descend, Interlevel.Mode);
        Assert.Equal(new[] { InterlevelMode.Descend }, View.SwitchedModes);
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Move_TowardAPit_AsksFirst_AndAConfirmedJumpFalls()
    {
        LoadLevel(
            "####",
            "#.x#",
            "#.x#",
            "####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));

        Assert.False(hero.Handle(TestLevel.At(2, 1)));
        Assert.Single(View.Windows);
        Assert.Equal(TestLevel.At(1, 1), hero.Pos);
        Assert.True(hero.Ready);
        Assert.IsType<HeroAction.Move>(hero.LastAction);

        View.Windows[0].Select(0);
        Assert.Equal(TestLevel.At(2, 1), hero.Pos);
        Assert.Equal(new[] { InterlevelMode.Fall }, View.SwitchedModes);
        Assert.False(Chasm.JumpConfirmed);
    }

    [Fact]
    public void ArrivingOnASign_ReadsIt()
    {
        LoadLevel(
            "#####",
            "#.!.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 1;
        hero.Handle(TestLevel.At(2, 1));
        Assert.Empty(View.Windows);
        hero.Handle(TestLevel.At(2, 1));
        Assert.Single(View.Windows);
        Assert.True(hero.Ready);
    }

    [Fact]
    public void EarnXp_LevelsUp()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        hero.EarnExp(10);
        Assert.Equal(2, hero.Lvl);
        Assert.Equal(0, hero.Exp);
        Assert.Equal(25, hero.HT);
        Assert.Equal(25, hero.HP);
        Assert.Equal(11, hero.AttackSkill(null));
        Assert.Equal(6, hero.DefenseSkill(null));
        Assert.True(View.Logged("Welcome to level 2"));
        Assert.Contains((StatusColor.Positive, "level up!"), view.Statuses);
        Assert.Equal(15, hero.MaxExp());
        Assert.Equal(1 - Math.Pow(0.85, 1.5), hero.Awareness, 3);
    }

    [Fact]
    public void DamageRoll_FollowsStrength()
    {
        var hero = new PixelDungeon.Core.Actors.Hero.Hero();
        Assert.Equal(1, hero.DamageRoll());
        hero.STR = 14;
        for (var i = 0; i < 50; i++)
        {
            Assert.InRange(hero.DamageRoll(), 1, 5);
        }

        hero.Weakened = true;
        Assert.Equal(12, hero.Str());
    }

    [Fact]
    public void Die_RevealsTheLevel_AndCallsTheDoom()
    {
        var level = LoadLevel(
            "#####",
            "#.S.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var doom = new CountingDoom();
        hero.Damage(100, doom);
        Assert.False(hero.IsAlive());
        Assert.Equal(1, doom.Deaths);
        Assert.Equal(Terrain.Door, level.Map[TestLevel.At(2, 1)]);
        Assert.True(level.Visited[TestLevel.At(3, 1)]);
        Assert.DoesNotContain(hero, Actor.All());
    }

    [Fact]
    public void AFatalFall_RecordsTheResult()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(2, 1));
        Dungeon.Depth = 3;
        hero.HP = 1;
        Chasm.HeroLand();
        Assert.False(hero.IsAlive());
        Assert.Equal("Fell to death on level 3", Dungeon.ResultDescription);
        Assert.True(View.Logged("You fell to death..."));
    }

    [Fact]
    public void Bundle_RoundTripsTheHero()
    {
        var hero = new PixelDungeon.Core.Actors.Hero.Hero
        {
            Pos = 40, STR = 12, Lvl = 3, Exp = 4, HeroClass = HeroClass.Huntress, SubClass = HeroSubClass.Sniper
        };
        var bundle = new Bundle();
        bundle.Put("hero", hero);
        var restored = (PixelDungeon.Core.Actors.Hero.Hero)bundle.Get("hero");

        Assert.Equal(40, restored.Pos);
        Assert.Equal(12, restored.STR);
        Assert.Equal(3, restored.Lvl);
        Assert.Equal(4, restored.Exp);
        Assert.Equal(HeroClass.Huntress, restored.HeroClass);
        Assert.Equal(HeroSubClass.Sniper, restored.SubClass);
        Assert.Equal(10, restored.AttackSkill(null));
        Assert.Equal(1 - Math.Pow(0.90, 1), restored.Awareness, 3);
        Assert.Equal("sniper", restored.ClassName());
    }
}