using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
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
}