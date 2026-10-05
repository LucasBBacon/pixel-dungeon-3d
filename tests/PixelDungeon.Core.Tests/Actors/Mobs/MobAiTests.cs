using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class MobAiTests : DungeonFixture
{
    private sealed class Dummy : Mob
    {
        public Dummy()
        {
            Name = "dummy";
            HP = HT = 10;
            DefenseSkillValue = 0;
        }

        public override int AttackSkill(Char target) => 100; // never misses
        public override int DamageRoll() => 1;

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

        public bool AlertedForTest
        {
            get => Alerted;
            set => Alerted = value;
        }

        public bool ActForTest() => Act();
    }

    private Dummy PlaceMob(int cell)
    {
        var mob = new Dummy { Pos = cell, Sprite = new FakeCharView() };
        _ = mob.Id(); // Actor.Add only records ids that are already assigned and Id() assigns lazily
        GameScene.Add(mob);
        return mob;
    }

    [Fact]
    public void Sleeping_WIthNoEnemyInSight_StaysAsleepAndSpendsATick()
    {
        // a wall between, because Char.Act() recomputes Level.FieldOfView from
        // the acting mob's position, poking the array before the act would be undone
        LoadLevel(
            "#####",
            "#<#.#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(3, 1));
        mob.StateForTest = mob.SleepingState;

        mob.ActForTest();

        Assert.Same(mob.SleepingState, mob.StateForTest);
    }

    [Fact]
    public void Sleeping_whenTheWakeRollSucceeds_HuntsAndTargetsTheHero()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = mob.SleepingState;
        Dungeon.Observe();

        // distance 1 + stealth 0 + flying 0 => Random.Int(1) is always 0, so it always wakes
        mob.ActForTest();

        Assert.Same(mob.HuntingState, mob.StateForTest);
        Assert.Equal(hero.Pos, mob.TargetForTest);
    }

    // Distance(hero, mob) is Chebyshev = 6, Hero.Stealth() = 0
    // so wandering's wake roll is Random.Int(6 / 2 + 0) = Random.Int(3)
    // In fixture's Random.Seed(1) the first draws always succeeds the roll regardless of
    // justAlerted, which made the positive test vacuous (passed even with "justAlerted" deleted from Wandering.Act)
    // reseeding to a draw that fails the roll on its own, verified empirically, not just by arithmetic, makes both
    // tests actually exercises the justAlerted short-circuit
    [Fact]
    public void Wandering_WhenNotAltered_AndWakeRollFails_StaysWandering()
    {
        LoadLevel(
            "#########",
            "#<......#",
            "#########"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(7, 1));
        mob.StateForTest = mob.WanderingState;
        Dungeon.Observe();

        Random.Seed(2); // first draw fails Random.Int(3)
        mob.AlertedForTest = false;

        mob.ActForTest();

        Assert.Same(mob.WanderingState, mob.StateForTest);
    }

    [Fact]
    public void Wandering_WhenJustAlerted_HuntsImmediately()
    {
        LoadLevel(
            "#########",
            "#<......#",
            "#########"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(7, 1));
        mob.StateForTest = mob.WanderingState;
        Dungeon.Observe();

        // only Mob.Damage sets Alerted, Beckon doesn't, it calls Notice()
        Random.Seed(2); // first draw fails Random.Int(3)
        mob.AlertedForTest = true;

        mob.ActForTest(); // justAlerted is true, wake roll skipped

        Assert.Same(mob.HuntingState, mob.StateForTest);
    }

    [Fact]
    public void Hunting_WhenAdjacent_Attacks()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT;
        var mob = PlaceMob(TestLevel.At(2, 1));
        var mobView = (FakeCharView)mob.Sprite;
        mob.StateForTest = mob.HuntingState;
        mob.TargetForTest = hero.Pos;
        Dungeon.Observe();

        var before = hero.HP;
        var actResult = mob.ActForTest(); // visible fight, doAttack must gate on the sprite, no direct attack
        mob.OnAttackComplete(); // view calling back, this what actually lands hit

        // pin all three facts the comment above used to only claim
        // animation gate (false, not the inverted !visible), exactly one swing recorded by view
        // (not a direct Attack() call hidden inside DoAttack), and the exact single-hit HP drop
        // not "less than before", which a swapped visible/invisible branch in DoAttack would 
        // still satisfy by landing this hit immediately and a second one via OnAttackComplete above
        Assert.False(actResult);
        Assert.Single(mobView.Attacks);
        Assert.Equal(hero.Pos, mobView.Attacks[0]);
        // TODO: armour / barkskin
        Assert.Equal(before - 1, hero.HP);
    }

    [Fact]
    public void Hunting_WhenThePathFails_FallsBackToWandering()
    {
        LoadLevel(
            "#####",
            "#<#.#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(3, 1)); // walled off
        mob.StateForTest = mob.HuntingState;
        mob.TargetForTest = TestLevel.At(1, 1);

        mob.ActForTest();

        Assert.Same(mob.WanderingState, mob.StateForTest);
    }

    [Fact]
    public void Passive_NeverActsOnAnEnemy()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = mob.PassiveState;
        Dungeon.Observe();

        mob.ActForTest();

        Assert.Same(mob.PassiveState, mob.StateForTest);
    }

    [Fact]
    public void ChooseEnemy_UnderTerror_PicksTheTerrorSource()
    {
        LoadLevel(
            "#######",
            "#<....#",
            "#######"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = PlaceMob(TestLevel.At(3, 1));
        var other = PlaceMob(TestLevel.At(5, 1));

        var terror = Buff.Affect<Terror>(mob);
        terror.Object = other.Id();
        mob.StateForTest = mob.FleeingState;
        Dungeon.Observe();

        mob.ActForTest();

        Assert.Equal(other.Pos, mob.TargetForTest);
    }

    [Fact]
    public void Beckon_WhenNotHunting_SetsWanderingAndTheTarget()
    {
        LoadLevel(
            "#########",
            "#<......#",
            "#########"
        );
        var mob = PlaceMob(TestLevel.At(7, 1));
        mob.StateForTest = mob.SleepingState;

        mob.Beckon(TestLevel.At(3, 1));

        Assert.Equal(mob.WanderingState, mob.StateForTest);
        Assert.Equal(TestLevel.At(3, 1), mob.TargetForTest);
    }

    [Theory]
    [InlineData("Sleeping")]
    [InlineData("Wandering")]
    [InlineData("Hunting")]
    [InlineData("Fleeing")]
    [InlineData("Passive")]
    public void Mob_RoundTripsItsStateAndTarget(string tag)
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var mob = PlaceMob(TestLevel.At(2, 1));
        mob.StateForTest = tag switch
        {
            "Sleeping" => mob.SleepingState,
            "Wandering" => mob.WanderingState,
            "Hunting" => mob.HuntingState,
            "Fleeing" => mob.FleeingState,
            _ => mob.PassiveState,
        };
        mob.TargetForTest = 99;

        var bundle = new Bundle();
        mob.StoreInBundle(bundle);
        Assert.Equal(tag, bundle.GetString("state"));

        var restored = new Dummy();
        restored.RestoreFromBundle(bundle);
        Assert.Equal(99, restored.TargetForTest);
        Assert.Equal(tag, RestoredTag(restored));
    }

    private static string RestoredTag(Dummy mob)
    {
        var bundle = new Bundle();
        mob.StoreInBundle(bundle);
        return bundle.GetString("state");
    }
}