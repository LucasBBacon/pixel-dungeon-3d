using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class AfflictionTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#####",
        "#...#",
        "#####"
    ];

    private sealed class Dummy : Char
    {
        public Dummy()
        {
            HP = HT = 20;
        }

        protected override bool Act() => false;
    }

    private static Dummy NewDummy(int cell) => new() { Pos = cell, Sprite = new FakeCharView() };

    [Fact]
    public void Poison_DamageLeftOverThreePlusOne_EachTick_ThenDetaches()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var d = NewDummy(TestLevel.At(3, 1));
        var view = (FakeCharView)d.Sprite;
        var poison = Buff.Affect<Poison>(d);
        poison.Set(4f);

        hero.Spend(4f); // the poison acts at 0, 1, 2, and 3, all before the hero's turn at 4
        Actor.Process();

        // left 4: (int)(4/3)+1 = 2; left 3: (int)(3/3)+1 = 2; left 2: (int)(2/3)+1 = 1; left 1: (int)(1/3)+1 = 1; then left 0 detaches
        // HP stays above HT / 2 = 10, so every hit shows in WARNING
        Assert.Equal(["2", "2", "1", "1"],
            view.Statuses
                .Where(s => s.Color == StatusColor.Warning)
                .Select(s => s.Text));
        Assert.Equal(14, d.HP); // 20 - (2 + 2 + 1 + 1)
        Assert.Null(d.GetBuff<Poison>());
        Assert.DoesNotContain(poison, Actor.All());
    }

    [Fact]
    public void Poison_OnADeadTarget_Detaches()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var d = NewDummy(TestLevel.At(3, 1));
        d.HP = 0;
        Buff.Affect<Poison>(d).Set(5f);

        hero.Spend(1f);
        Actor.Process();

        Assert.Null(d.GetBuff<Poison>());
    }

    [Fact]
    public void Poison_KillingTheHero_RecordsThePoisonCause()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        Dungeon.Depth = 2;
        hero.HP = 1;
        Buff.Affect<Poison>(hero).Set(1f);

        hero.Spend(1f);
        Actor.Process(); // (int)(1 / 3) + 1 = 1 damage at the poison's turn

        Assert.False(hero.IsAlive());
        Assert.Equal("Died from poison on level 2", Dungeon.ResultDescription);
        Assert.Contains(("You died from poison...", LogKind.Negative), View.Logs);
    }

    [Fact]
    public void Poison_RoundTripsLeft()
    {
        var poison = new Poison();
        poison.Set(7.5f);

        var bundle = new Bundle();
        poison.StoreInBundle(bundle);
        var restored = new Poison();
        restored.RestoreFromBundle(bundle);

        Assert.Equal(7.5f, bundle.GetFloat("left"));
        Assert.Equal(7.5f, restored.Left);
    }

    [Fact]
    public void Durations_WithoutRingOfElements_AreJavaDefaults()
    {
        var d = new Dummy();
        Assert.Equal(1f, Poison.DurationFactor(d));
        Assert.Equal(10f, Paralysis.Duration(d));
        Assert.Equal(10f, Slow.Duration(d));
    }

    [Fact]
    public void Paralysis_SetsParalysed_AndDetachClearsIt()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));

        Buff.Prolong<Paralysis>(d, Paralysis.Duration(d));
        Assert.True(d.Paralysed);

        Buff.Detach<Paralysis>(d);
        Assert.False(d.Paralysed);
        Assert.Null(d.GetBuff<Paralysis>());
    }

    [Fact]
    public void Paralysis_DetachingOneOfTwo_KeepsTheCharParalysed()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));
        var first = Buff.Append<Paralysis>(d, 5f);
        var second = Buff.Append<Paralysis>(d, 5f);

        first.Detach();
        Assert.True(d.Paralysed); // unfreeze still finds second paralyzed

        second.Detach();
        Assert.False(d.Paralysed);
    }

    [Fact]
    public void Paralysis_ExpiresAfterItsDuration()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        var d = NewDummy(TestLevel.At(3, 1));
        Buff.Prolong<Paralysis>(d, 3f);

        hero.Spend(5f); // the buff's turn comes at 3, before the hero's at 5
        Actor.Process();

        Assert.False(d.Paralysed);
        Assert.Null(d.GetBuff<Paralysis>());
    }

    [Fact]
    public void ParalysedHero_SkipsItsActionAndSpendsATick()
    {
        LoadLevel(Room);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.LastAction = new HeroAction.Move(TestLevel.At(3, 1));
        Buff.Prolong<Paralysis>(hero, Paralysis.Duration(hero));

        hero.Resume();

        Assert.Equal(TestLevel.At(1, 1), hero.Pos);
        Assert.Null(hero.CurAction);
        Assert.Equal(1f, hero.TimeLeft); // Actor.TICK
    }

    [Fact]
    public void Slow_DoublesTheTimeSpent()
    {
        LoadLevel(Room);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));

        hero.Spend(1f);
        Assert.Equal(1f, hero.TimeLeft);

        Buff.Prolong<Slow>(hero, Slow.Duration(hero));
        hero.Spend(1f);
        Assert.Equal(3f, hero.TimeLeft); // 1 + 1 / 0.5
    }

    [Fact]
    public void Roots_SetsRooted_AndDetachClearsIt()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));

        Buff.Prolong<Roots>(d, 5f);
        Assert.True(d.Rooted);

        Buff.Detach<Roots>(d);
        Assert.False(d.Rooted);
        Assert.Null(d.GetBuff<Roots>());
    }

    [Fact]
    public void Roots_DoNotAttachToAFlyer()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));
        d.Flying = true;

        var roots = Buff.Prolong<Roots>(d, 5f);

        Assert.False(d.Rooted);
        Assert.Null(d.GetBuff<Roots>());
        Assert.DoesNotContain(roots, Actor.All());
    }

    [Fact]
    public void RootedHero_CannotMove()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.LastAction = new HeroAction.Move(TestLevel.At(3, 1));
        Buff.Prolong<Roots>(hero, 5f);

        hero.Resume();

        Assert.Equal(TestLevel.At(1, 1), hero.Pos);
        Assert.Contains((1f, 1f), View.Shakes); // Hero.GetCloser's rooted branch shakes the camera and refuses
    }

    [Fact]
    public void Restore_ReattachesParalysisAndRoots_WithTheirFlags()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));
        Buff.Prolong<Paralysis>(d, 5f);
        Buff.Prolong<Roots>(d, 5f);

        var bundle = new Bundle();
        d.StoreInBundle(bundle);
        var restored = new Dummy();
        restored.RestoreFromBundle(bundle);

        Assert.True(restored.Paralysed);
        Assert.True(restored.Rooted);
        Assert.NotNull(restored.GetBuff<Paralysis>());
        Assert.NotNull(restored.GetBuff<Roots>());
    }

    [Theory]
    [InlineData(typeof(Poison), "Poisoned", 3)] // BuffIndicator.POISON
    [InlineData(typeof(Paralysis), "Paralysed", 4)] // BuffIndicator.PARALYSIS
    [InlineData(typeof(Slow), "Slowed", 7)] // BuffIndicator.SLOW
    [InlineData(typeof(Roots), "Rooted", 11)] // BuffIndicator.ROOTS
    public void Afflictions_NameAndIcon_MatchTheJava(Type type, string name, int icon)
    {
        var buff = (Buff)Activator.CreateInstance(type);
        Assert.NotNull(buff);
        Assert.Equal(name, buff.ToString());
        Assert.Equal(icon, buff.Icon());
    }

    [Theory]
    [InlineData(typeof(Poison), "poisoned")]
    [InlineData(typeof(Slow), "slowed")]
    [InlineData(typeof(Paralysis), "paralysed")]
    [InlineData(typeof(Roots), "rooted")]
    public void Add_Affliction_ShowsItsStatusText(Type type, string status)
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));

        ((Buff)Activator.CreateInstance(type)).AttachTo(d);

        Assert.Contains((StatusColor.Negative, status), ((FakeCharView)d.Sprite).Statuses);
    }

    [Fact]
    public void Add_Poison_BurstsAPoisonSplash()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));

        Buff.Affect<Poison>(d);

        Assert.Equal(5, ((FakeCharView)d.Sprite).Bursts);
    }

    [Theory]
    [InlineData(typeof(Paralysis), "You are paralysed!", true)]
    [InlineData(typeof(Poison), "You are poisoned!", true)]
    [InlineData(typeof(Roots), "You can't move!", false)]
    public void HeroAdd_Affliction_warnsAndInterruptsAsTheJavaDoes(Type type, string warning, bool interrupts)
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.CurAction = new HeroAction.Move(TestLevel.At(3, 1));

        ((Buff)Activator.CreateInstance(type)).AttachTo(hero);

        Assert.Contains((warning, LogKind.Warning), View.Logs);
        Assert.Equal(interrupts, hero.CurAction == null);
    }

    [Fact]
    public void HeroAdd_Slow_NeitherWarnsNorInterrupts()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.CurAction = new HeroAction.Move(TestLevel.At(3, 1));

        Buff.Prolong<Slow>(hero, Slow.Duration(hero));

        Assert.DoesNotContain(View.Logs, l => l.Kind == LogKind.Warning);
        Assert.NotNull(hero.CurAction);
    }

    [Fact]
    public void Damage_WhileParalysed_SnapsOutWhenTheDamageRollBeatsTheHealthRoll()
    {
        LoadLevel(Room);
        PlaceHero(TestLevel.At(1, 1)); // observes, so the dummy's cell is visible

        for (var seed = 1; seed <= 30; seed++)
        {
            var d = NewDummy(TestLevel.At(3, 1));
            Buff.Prolong<Paralysis>(d, 10f);
            View.Logs.Clear();

            // Char.damage rolls Random.Int(dmg) first, then Random.Int(HP), reading HP before the hit lands.
            Random.Seed(seed);
            var snaps = Random.Int(10) >= Random.Int(20);
            Random.Seed(seed);

            d.Damage(10, this);

            Assert.Equal(10, d.HP); // 20 - 10
            Assert.Equal(!snaps, d.Paralysed);
            Assert.Equal(snaps, View.Logs.Contains(("The pain snapped mob out of paralysis", LogKind.Info)));
        }
    }

    [Fact]
    public void Damage_ForZeroAtOneHp_AlwaysSnapsOut_AndLogsOnlyInSight()
    {
        LoadLevel(Room);
        PlaceHero(TestLevel.At(1, 1));

        // Random.Int(0) and Random.Int(1) both return 0, so 0 >= 0 always snaps out.
        var seen = NewDummy(TestLevel.At(3, 1));
        seen.HP = 1;
        Buff.Prolong<Paralysis>(seen, 10f);
        seen.Damage(0, this);

        Assert.False(seen.Paralysed);
        Assert.Null(seen.GetBuff<Paralysis>());
        Assert.Contains(("The pain snapped mob out of paralysis", LogKind.Info), View.Logs);

        View.Logs.Clear();
        var unseen = NewDummy(TestLevel.At(2, 1));
        unseen.HP = 1;
        Buff.Prolong<Paralysis>(unseen, 10f);
        Dungeon.Visible[unseen.Pos] = false;
        unseen.Damage(0, this);

        Assert.False(unseen.Paralysed);
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Damage_WithoutParalysis_RollsNothing()
    {
        LoadLevel(Room);
        var d = NewDummy(TestLevel.At(3, 1));

        Random.Seed(5);
        var first = Random.Int(1000);
        Random.Seed(5);
        d.Damage(3, this);

        Assert.Equal(first, Random.Int(1000));
    }

    [Fact]
    public void SwarmSplit_FromAPoisonedParent_PoisonsTheCloneForTwo()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####");
        PlaceHero(TestLevel.At(1, 1));
        var swarm = new Swarm { Pos = TestLevel.At(2, 2), Sprite = new FakeCharView() };
        GameScene.Add(swarm);
        swarm.HP = 81; // HP >= damage + 2, as SwarmTests' split case
        Buff.Affect<Poison>(swarm).Set(5f);

        swarm.DefenseProc(Dungeon.Hero, 10);

        var clone = Dungeon.Level.Mobs.OfType<Swarm>().Single(m => m != swarm);
        Assert.Equal(2f, clone.GetBuff<Poison>().Left);
        Assert.Equal(5f, swarm.GetBuff<Poison>().Left);
    }
}