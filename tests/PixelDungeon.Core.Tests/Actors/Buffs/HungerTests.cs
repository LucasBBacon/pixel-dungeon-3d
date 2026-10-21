using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class HungerTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#####",
        "#...#",
        "#####"
    ];

    private static readonly string[] Stairs =
    [
        "#####",
        "#<.>#",
        "#####"
    ];

    [Fact]
    public void Live_AttachesAnEmptyHunger()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));

        hero.Live();

        var hunger = hero.GetBuff<Hunger>();
        Assert.NotNull(hunger);
        Assert.Equal(0f, hunger.HungerLevel);
        Assert.NotNull(hero.GetBuff<Regeneration>());
    }

    [Theory]
    [InlineData(HeroClass.Rogue,
        30f)] // turns at 0, 12 and 24 (STEP * 1.2f = 12 apart); the next, at 36, is after the hero's
    [InlineData(HeroClass.Warrior, 40f)] // turns at 0, 10, 20 and 30 (STEP = 10 apart)
    public void Act_BeforeStarving_AddsAStepEachTurnOfTheClassInterval(HeroClass heroClass, float expected)
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HeroClass = heroClass;
        var hunger = Buff.Affect<Hunger>(hero);

        hero.Spend(35f);
        Actor.Process();

        Assert.Equal(expected, hunger.HungerLevel);
    }

    [Fact]
    public void Act_CrossingHungry_WarnsOnce()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-250f);

        hero.Spend(13f); // turns at 0 (250 -> 260, crossing HUNGRY) and 12 (260 -> 270)
        Actor.Process();

        Assert.Equal(270f, hunger.HungerLevel);
        Assert.Single(View.Logs, l => l.Text == "You are hungry.");
        Assert.Contains(("You are hungry.", LogKind.Warning), View.Logs);
        Assert.False(hunger.IsStarving());
    }

    [Fact]
    public void Act_ReachingStarving_LogsAndInterrupts()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-350f);
        hero.CurAction = new HeroAction.Move(TestLevel.At(3, 1));

        hero.Spend(1f); // hunger's turn at 0 (350 -> 360) comes before hero's at 1
        Actor.Process();

        Assert.Equal(360f, hunger.HungerLevel);
        Assert.True(hunger.IsStarving());
        Assert.Contains(("You are starving!", LogKind.Negative), View.Logs);
        Assert.IsType<HeroAction.Move>(hero.LastAction); // Interrupt() keeps canceled move
        Assert.Equal(TestLevel.At(1, 1), hero.Pos); // hero did not walk during turn
    }

    [Fact]
    public void Act_WhileStarving_TakesOneDamageWhenTHeRollIsUnderThreeTenths()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT = 100;
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-Hunger.Starving);
        hero.Spend(6f); // hunger turns at 0, 12, 24 -> hero at 6, 18, 30, never tie

        for (var seed = 1; seed <= 30; seed++)
        {
            Random.Seed(seed);
            var hit = Random.Float() < 0.3f;
            Random.Seed(seed);
            var hp = hero.HP;
            var logs = View.Logs.Count;

            Actor.Process(); // one hunger turn, then hero's

            Assert.Equal(hit ? hp - 1 : hp, hero.HP);
            Assert.Equal(hit, View.Logs.Skip(logs).Contains(("You are starving!", LogKind.Negative)));
            hero.Next(); // Hero.Act idles and stays the current actor until it calls Next(), as ActorTests shows
            hero.Spend(12f);
        }
    }

    [Fact]
    public void Act_WhileStarvingParalysedAtOneHP_TakesNoDamage_ButStillRolls()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = 1;
        hero.Paralysed = true;
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-Hunger.Starving);

        Random.Seed(3);
        var draws = new float[42];
        for (var i = 0; i < draws.Length; i++)
        {
            draws[i] = Random.Float();
        }

        Random.Seed(3);

        hero.Spend(12f * 40 + 6f); // 41 hunger turns (0, 12, ... 480) before the paralysed hero's at 486
        Actor.Process();

        Assert.Equal(1, hero.HP);
        Assert.Equal(draws[41], Random.Float()); // Random.Float() < 0.3f runs before the gate, once per turn
    }

    [Fact]
    public void Act_StarvingToDeath_RecordsTheHungerCause()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 4;
        hero.HP = 1;
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-Hunger.Starving);

        // each starving turns hits with 0.3 chance, missing all 201 turns, likely won't miss
        hero.Spend(12f * 200 + 6f);
        Actor.Process(); // stops at hero's death

        Assert.False(hero.IsAlive());
        Assert.Equal("Starved to death on level 4", Dungeon.ResultDescription);
        Assert.Contains(("You starved to death...", LogKind.Negative), View.Logs);
    }

    [Fact]
    public void Act_OnAADeadHero_LeavesTheLevelAlone()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var hunger = Buff.Affect<Hunger>(hero);
        hero.HP = 0;

        hero.Spend(1f);
        Actor.Process(); // the hunger's turn at 0 finds the hero dead and deactivates; Process then stops

        Assert.Equal(0f, hunger.HungerLevel);
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Satisfy_ClampsBetweenZeroAndStarving()
    {
        var hunger = new Hunger();

        hunger.Satisfy(-1000f);
        Assert.Equal(Hunger.Starving, hunger.HungerLevel);

        hunger.Satisfy(1000f);
        Assert.Equal(0f, hunger.HungerLevel);

        hunger.Satisfy(-50f);
        hunger.Satisfy(20f);
        Assert.Equal(30f, hunger.HungerLevel); // 0 + 50 - 20: energy lowers the level
    }

    [Fact]
    public void IconAndName_FollowTheLevel()
    {
        var hunger = new Hunger();

        hunger.Satisfy(-259f);
        Assert.Equal(BuffIndicator.None, hunger.Icon());
        Assert.Equal("Hungry", hunger.ToString()); // the Java names every level below STARVING "Hungry"

        hunger.Satisfy(-1f); // 260 = HUNGRY
        Assert.Equal(5, hunger.Icon()); // BuffIndicator.HUNGER
        Assert.Equal("Hungry", hunger.ToString());

        hunger.Satisfy(-100f); // 360 = STARVING
        Assert.Equal(6, hunger.Icon()); // BuffIndicator.STARVATION
        Assert.Equal("Starving", hunger.ToString());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Stairs_AddATenthOfStarving(int x)
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(x, 1));
        Dungeon.Depth = 2;
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-100f);

        Assert.False(hero.Handle(TestLevel.At(x, 1)));

        Assert.Equal(136f, hunger.HungerLevel); // satisfy(-STARVING / 10) = satisfy(-36): 100 + 36
        Assert.Single(View.SwitchedModes);
    }

    [Fact]
    public void Stairs_WhileStarving_LeaveHungerAlone()
    {
        LoadLevel(Stairs);
        var hero = PlaceHero(TestLevel.At(3, 1));
        var hunger = Buff.Affect<Hunger>(hero);
        hunger.Satisfy(-355f);
        hero.Spend(1f);
        Actor.Process(); // one step, 355 -> 365: Act adds past STARVING, and only Satisfy clamps
        Assert.Equal(365f, hunger.HungerLevel);

        Assert.False(hero.Handle(TestLevel.At(3, 1)));

        Assert.Equal(365f, hunger.HungerLevel); // without the guard, satisfy(-36) would clamp 401 down to 360
    }

    [Fact]
    public void Hunger_RoundTripsItsLevel()
    {
        var hunger = new Hunger();
        hunger.Satisfy(-123f);

        var bundle = new Bundle();
        hunger.StoreInBundle(bundle);
        var restored = new Hunger();
        restored.RestoreFromBundle(bundle);

        Assert.Equal(123f, bundle.GetFloat("level"));
        Assert.Equal(123f, restored.HungerLevel);
    }

    [Fact]
    public void HeroIsStarving_ReadsHunger_AndIsFalseWithoutIt()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Assert.False(hero.IsStarving()); // the fixture's hero never called Live(), so it has no Hunger

        var hunger = Buff.Affect<Hunger>(hero);
        Assert.False(hero.IsStarving());

        hunger.Satisfy(-Hunger.Starving);
        Assert.True(hero.IsStarving());
    }

    [Fact]
    public void Regeneration_WhileStarving_DoesNotHeal()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT - 1;
        Buff.Affect<Hunger>(hero).Satisfy(-Hunger.Starving);
        var regen = Buff.Affect<Regeneration>(hero);

        regen.ActForTest();

        Assert.Equal(hero.HT - 1, hero.HP);
    }
}