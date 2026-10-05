using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class MobCombatTests : DungeonFixture
{
    private sealed class Dummy : Mob
    {
        public Dummy()
        {
            Name = "dummy";
            HP = HT = 10;
            Exp = 3;
            MaxLvl = 5;
        }

        public AiState StateForTest
        {
            get => State;
            set => State = value;
        }

        public bool AlertedForTest => Alerted;
        public int ExpForTest() => GetExp();
        public void SetDefenseSkillValue(int v) => DefenseSkillValue = v;
        public void SetEnemySeen(bool v) => EnemySeen = v;

        public float TimeLeftForTest => Cooldown();
    }

    private Dummy PlaceMob(int cell)
    {
        var mob = new Dummy { Pos = cell, Sprite = new FakeCharView() };
        GameScene.Add(mob);
        return mob;
    }

    [Fact]
    public void Damage_WakesASleepingMobToWanderingAndAlertsIt()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = mob.SleepingState;

        mob.Damage(1, Dungeon.Hero);

        Assert.Same(mob.WanderingState, mob.StateForTest);
        Assert.True(mob.AlertedForTest);
    }

    [Fact]
    public void Damage_RecoversFromTerror()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        Buff.Prolong<Terror>(mob, Terror.Duration / 2);

        mob.Damage(1, Dungeon.Hero);

        Assert.Null(mob.GetBuff<Terror>());
    }

    [Fact]
    public void DefenseSkill_BeforeTheMobHasSeenAnyone_IsZero()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        // A nonzero DefenseSkillValue: with the brief's original DefenseSkillValue of 0
        // dummy never sees it, an implementation that ignored EnemySeen and returned
        // DefenseSkillValue unconditionally would still read 0 and pass
        // Only a nonzero value actually exercises the "EnemySeen ? DefenseSkillValue : 0" gate
        mob.SetDefenseSkillValue(100);

        // EnemySeen is false until the mob acts, so DefenseSkill returns 0
        // and Char.Hit's defRoll is 0, always lands
        Assert.Equal(0, mob.DefenseSkill(hero));
    }

    [Fact]
    public void DefenseSkill_WhenParalysed_IsZeroEvenIfEnemySeen()
    {
        LoadLevel("#####", "#<..#", "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        // The companion test above only exercises the EnemySeen half of
        // "EnemySeen && !Paralyzed ? DefenseSkillValue : 0"
        // with EnemySeen true and Paralyzed left false, a mutation that dropped "!Paralyzed"
        // entirely (return EnemySeen ? DefenseSkillValue : 0) would still pass
        // every existing test
        // Set both EnemySeen and Paralyzed true so only the Paralysed conjunct can make this read 0
        mob.SetDefenseSkillValue(100);
        mob.SetEnemySeen(true);
        mob.Paralysed = true;

        Assert.Equal(0, mob.DefenseSkill(hero));
    }

    [Fact]
    public void Destroy_RemovesTheMobFromTheLevelAndGrantsExp()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        var before = hero.Exp;
        // QualifiedForNoKilling defaults to false already, so leaving it false here would
        // let the assertion pass even if Destroy never touched the field
        // setting it true first makes the assertion actually exercise Destroy's reset
        Statistics.QualifiedForNoKilling = true;

        mob.Destroy();

        Assert.DoesNotContain(mob, Dungeon.Level.Mobs);
        Assert.Equal(before + 3, hero.Exp);
        Assert.Equal(1, Statistics.EnemiesSlain);
        Assert.False(Statistics.QualifiedForNoKilling);
    }

    [Fact]
    public void Destroy_WhenTheHeroIsDead_GrantsNoExperienceOrStatistics()
    {
        LoadLevel("#####", "#<..#", "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        var before = hero.Exp;
        // The test above only runs with a living hero, so a mutation that moved the
        // experience grant (or the whole Hostile statistics block) outside
        // "if (Dungeon.Hero.IsAlive())" would produce an identical result there and pass
        // kill the hero first so only that guard can suppress both effects.
        hero.HP = 0;

        mob.Destroy();

        Assert.DoesNotContain(mob, Dungeon.Level.Mobs);
        Assert.Equal(before, hero.Exp);
        Assert.Equal(0, Statistics.EnemiesSlain);
    }

    [Fact]
    public void Exp_WhenTheHeroOutLevelsTheMob_IsZero()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));

        hero.Lvl = 5;
        Assert.Equal(3, mob.ExpForTest());

        hero.Lvl = 6;
        Assert.Equal(0, mob.ExpForTest());
    }

    [Fact]
    public void Die_OutsideTheHerosSight_LogsTheDistantDeath()
    {
        LoadLevel(
            "#########",
            "#<......#",
            "#########"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(7, 1));
        Array.Fill(Dungeon.Visible, false);

        mob.Die(Dungeon.Hero);

        Assert.Contains(View.Logs, l => l.Text.Contains("died in the distance"));
    }

    [Fact]
    public void Die_WithinTheHerosSight_DoesNotLogTheDistantDeath()
    {
        // Mirrors the test above but flips visibility, so a mutation dropping either
        // conjunct of "Dungeon.Hero.IsAlive() && !Dungeon.Visible[Pos]" (in particular,
        // dropping the visibility check entirely) goes uncaught without this negative case.
        LoadLevel(
            "#########",
            "#<......#",
            "#########");
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(7, 1));
        Array.Fill(Dungeon.Visible, true);

        mob.Die(Dungeon.Hero);

        Assert.DoesNotContain(View.Logs, l => l.Text.Contains("died in the distance"));
    }

    [Fact]
    public void Add_Amok_SwitchesToHunting()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = mob.SleepingState;

        Buff.Affect<Amok>(mob);

        Assert.Same(mob.HuntingState, mob.StateForTest);
    }

    [Fact]
    public void Add_Terror_SwitchesToFleeing_AndRemovingItReturnsToHunting()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));

        Buff.Prolong<Terror>(mob, Terror.Duration);
        Assert.Same(mob.FleeingState, mob.StateForTest);

        Buff.Detach<Terror>(mob);
        Assert.Same(mob.HuntingState, mob.StateForTest);
    }

    [Fact]
    public void Add_Sleep_SwitchesToSleepingAndPostponesByTheWakeDelay()
    {
        LoadLevel("#####", "#<..#", "#####");
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = mob.HuntingState;

        Buff.Affect<Sleep>(mob);

        Assert.Same(mob.SleepingState, mob.StateForTest);
        // Same(SLEEPING) alone would pass even if Postpone(Sleep.SWS) were dropped entirely
        // pin the schedule push too.
        Assert.Equal(Sleep.SWS, mob.TimeLeftForTest);
    }
}