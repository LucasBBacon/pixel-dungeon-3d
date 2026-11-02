using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class SightAndStealthTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#####",
        "#...#",
        "#####"
    ];

    private sealed class TestMob : Mob
    {
        public AiState StateForTest
        {
            get => State;
            set => State = value;
        }

        public int TargetForTest
        {
            get => Target;
            set => Target = value;
        }

        public void ActForTest() => Act();
    }

    private static TestMob NewMob(int cell) => new() { Pos = cell, Sprite = new FakeCharView() };

    // ---- Invisibility ----

    [Fact]
    public void Invisibility_EachBuffAdsToInvisible_AndOnlyTheLastDetachRemovesTheState()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;

        var first = Buff.Append<Invisibility>(hero, Invisibility.Duration);
        var second = Buff.Append<Invisibility>(hero, Invisibility.Duration);
        Assert.Equal(2, hero.Invisible);
        Assert.Equal(2, view.Statuses.Count(s => s == (StatusColor.Positive, "invisible")));

        first.Detach();
        Assert.Equal(1, hero.Invisible);
        Assert.DoesNotContain((CharState.Invisible, false), view.States);

        second.Detach();
        Assert.Equal(0, hero.Invisible);
        Assert.Equal((CharState.Invisible, false), view.States[^1]);
    }

    [Fact]
    public void Invisibility_AHuntingMobBesideTheInvisibleHero_DoesNotAttack()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = NewMob(TestLevel.At(2, 1));
        GameScene.Add(mob);
        mob.StateForTest = mob.HuntingState;
        mob.TargetForTest = hero.Pos;
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);
        var before = hero.HP;

        mob.ActForTest();

        // Mob.Act's enemyInFOV needs Invisible <= 0
        // without it Hunting cannot attack, step onto the hero's occupied cell fails
        // so it spends a turn and wanders
        Assert.Empty(((FakeCharView)mob.Sprite).Attacks);
        Assert.Equal(before, hero.HP);
        Assert.Same(mob.WanderingState, mob.StateForTest);
    }

    [Fact]
    public void InvisibilityDispel_WithAVisibleEnemy_DetachesTheHeroInvisibility()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        GameScene.Add(NewMob(TestLevel.At(3, 1)), 10f);
        Actor.Process();
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);
        Assert.Equal(1, hero.VisibleEnemies());

        Invisibility.Dispel();

        Assert.Null(hero.GetBuff<Invisibility>());
        Assert.Equal(0, hero.Invisible);
    }

    [Fact]
    public void InvisibilityDispel_WithNoVisibleEnemy_KeepsIt()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);

        Invisibility.Dispel();

        Assert.NotNull(hero.GetBuff<Invisibility>());
    }

    [Fact]
    public void HeroOnAttackComplete_WhileInvisibleBesideAnEnemy_DispelsIt()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        Dungeon.Observe();
        hero.Handle(rat.Pos);
        Buff.Affect<Invisibility>(hero, Invisibility.Duration);

        hero.Handle(rat.Pos);
        hero.OnAttackComplete();

        Assert.Null(hero.GetBuff<Invisibility>());
    }

    // ---- Levitation ----

    [Fact]
    public void Levitation_SetsFlyingAndFreesARootedChar()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        Buff.Prolong<Roots>(hero, 5f);
        Assert.True(hero.Rooted);

        Buff.Affect<Levitation>(hero, Levitation.Duration);

        Assert.True(hero.Flying);
        Assert.False(hero.Rooted);
        Assert.Null(hero.GetBuff<Roots>());
        Assert.Contains((CharState.Levitating, true), view.States);
    }

    [Fact]
    public void Levitation_EndingOverAChasm_DropsTheHero()
    {
        LoadLevel("#####",
            "#.x.#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(2, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        Buff.Affect<Levitation>(hero, Levitation.Duration);
        hero.Spend(25f); // buff's turn at 20 comes first

        Actor.Process();

        Assert.False(hero.Flying);
        Assert.Null(hero.GetBuff<Levitation>());
        Assert.Equal(new[] { InterlevelMode.Fall }, View.SwitchedModes);
        Assert.Equal((CharState.Levitating, false), view.States[^1]);
    }

    // ---- Mind Vision + Blindness ----

    [Fact]
    public void MindVision_RevealsTheBlockAroundEveryMob_AndTheCellsWithinTwo()
    {
        LoadLevel(
            "#########",
            "#.#.....#",
            "#########"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        GameScene.Add(NewMob(TestLevel.At(6, 1)));
        Dungeon.Observe();
        Assert.False(Dungeon.Visible[TestLevel.At(6, 1)]);

        Buff.Affect<MindVision>(hero, MindVision.Duration);
        Dungeon.Observe();

        Assert.True(Dungeon.Visible[TestLevel.At(5, 1)]);
        Assert.True(Dungeon.Visible[TestLevel.At(6, 1)]);
        Assert.True(Dungeon.Visible[TestLevel.At(7, 1)]);
        Assert.True(Dungeon.Visible[TestLevel.At(3, 1)]); // distance 2: sense reaches past the wall
        Assert.False(Dungeon.Visible[TestLevel.At(4, 1)]); // neither within 2 of the hero nor beside the mob
    }

    [Fact]
    public void MindVision_Detaching_ObservesAgain()
    {
        LoadLevel(
            "#########",
            "#.#.....#",
            "#########");
        var hero = PlaceHero(TestLevel.At(1, 1));
        GameScene.Add(NewMob(TestLevel.At(6, 1)));
        var vision = Buff.Affect<MindVision>(hero, MindVision.Duration);
        Dungeon.Observe();
        var observes = View.ObserveCount;

        vision.Detach();

        Assert.Equal(observes + 1, View.ObserveCount);
        Assert.False(Dungeon.Visible[TestLevel.At(6, 1)]);
    }

    [Fact]
    public void Blindness_LeavesOnlyTheBlockAroundTheChar_UntilItEnds()
    {
        LoadLevel(
            "#######",
            "#.....#",
            "#######");
        var hero = PlaceHero(TestLevel.At(2, 1));
        Assert.True(Dungeon.Visible[TestLevel.At(5, 1)]);

        var blindness = Buff.Affect<Blindness>(hero, 3f);
        Dungeon.Observe();

        Assert.True(Dungeon.Visible[TestLevel.At(1, 1)]);
        Assert.True(Dungeon.Visible[TestLevel.At(3, 1)]);
        Assert.False(Dungeon.Visible[TestLevel.At(4, 1)]);
        Assert.False(Dungeon.Visible[TestLevel.At(5, 1)]);

        blindness.Detach(); // Blindness.Detach observes again

        Assert.True(Dungeon.Visible[TestLevel.At(5, 1)]);
    }

    // ---- Rage and Gases Immunity ----

    [Fact]
    public void DungeonFindPath_UnderRage_PathsThroughCellsToAvoid()
    {
        LoadLevel("######",
            "#.T..#",
            "######"
        );
        var mob = NewMob(TestLevel.At(1, 1));
        var noOneInView = new bool[Level.Length];

        // toxic trap is AVOID and not PASSABLE, so plain path to (4, 1) is blocked
        Assert.Equal(-1, Dungeon.FindPath(mob,
            TestLevel.At(1, 1),
            TestLevel.At(4, 1),
            Level.Passable,
            noOneInView));

        Buff.Affect<Rage>(mob, 5f);

        Assert.Equal(TestLevel.At(2, 1), Dungeon.FindPath(mob,
            TestLevel.At(1, 1),
            TestLevel.At(4, 1),
            Level.Passable,
            noOneInView));
    }

    [Fact]
    public void GasesImmunity_ProtectsTheHeroFromParalysisAndToxicGas_WhileItLasts()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Buff.Affect<GasesImmunity>(hero, GasesImmunity.Duration);

        Assert.Contains(typeof(Paralysis), hero.Immunities());
        Assert.Contains(typeof(ToxicGas), hero.Immunities());
        Assert.False(new Paralysis().AttachTo(hero));
        hero.Damage(5, new ToxicGas());
        Assert.Equal(20, hero.HP);

        Buff.Detach<GasesImmunity>(hero);

        Assert.Empty(hero.Immunities());
    }

    // ---- sprite states, texts and warnings ----

    [Fact]
    public void CharAdd_ShowsMindVisionsAndInvisibilitysTexts_AndAddsTheLevitatingAndInvisibleStates()
    {
        var mob = NewMob(TestLevel.At(2, 1));
        var view = (FakeCharView)mob.Sprite;

        Buff.Affect<MindVision>(mob, 1f);
        Buff.Affect<Levitation>(mob, 1f);
        Buff.Affect<Invisibility>(mob, 1f);

        Assert.Equal(
            new[]
            {
                (StatusColor.Positive, "mind"), (StatusColor.Positive, "vision"), (StatusColor.Positive, "invisible")
            },
            view.Statuses);
        Assert.Equal(new[] { (CharState.Levitating, true), (CharState.Invisible, true) }, view.States);
    }

    [Fact]
    public void UpdateSpriteState_ReaddsLevitatingAndInvisible()
    {
        var mob = NewMob(TestLevel.At(2, 1));
        Buff.Affect<Levitation>(mob, 1f);
        Buff.Affect<Invisibility>(mob, 1f);
        var fresh = new FakeCharView();
        mob.Sprite = fresh;

        mob.UpdateSpriteState();

        Assert.Equal(2, fresh.States.Count); // the buff set's order is unspecified
        Assert.Contains((CharState.Levitating, true), fresh.States);
        Assert.Contains((CharState.Invisible, true), fresh.States);
    }

    [Fact]
    public void HeroAdd_Blindness_WarnsWithoutInterrupting()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.CurAction = new HeroAction.Move(TestLevel.At(3, 1));

        Buff.Affect<Blindness>(hero, 3f);

        Assert.Contains(("You are blinded!", LogKind.Warning), View.Logs);
        Assert.NotNull(hero.CurAction);
    }

    [Theory]
    [InlineData(typeof(Invisibility), "Invisible")]
    [InlineData(typeof(Levitation), "Levitating")]
    [InlineData(typeof(MindVision), "Mind vision")]
    [InlineData(typeof(GasesImmunity), "Immune to gases")]
    [InlineData(typeof(Rage), "Blinded with rage")]
    [InlineData(typeof(Blindness), "Blinded")]
    public void ToString_ForEachBuff_IsTheJavasText(Type type, string text)
    {
        Assert.Equal(text, Activator.CreateInstance(type).ToString());
    }

    [Fact]
    public void Durations_AndTheMindVisionDistance_AreTheJavasConstants()
    {
        Assert.Equal(15f, Invisibility.Duration);
        Assert.Equal(20f, Levitation.Duration);
        Assert.Equal(20f, MindVision.Duration);
        Assert.Equal(5f, GasesImmunity.Duration);
        Assert.Equal(2, new MindVision().Distance);
    }
}