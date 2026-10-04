using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class BuffsTests : DungeonFixture
{
    private sealed class Dummy : Char
    {
        public Dummy()
        {
            HP = HT = 20;
        }

        protected override bool Act() => false;
    }

    private Dummy PlaceDummy(int cell)
    {
        var d = new Dummy { Pos = cell, Sprite = new FakeCharView() };
        // schedule a bit after "now"
        // mirrors how Actor.Init gives the Hero a -float.Epsilon head start
        // so a buff attached afterward at the same tick - tied at time 0 - wins the Actor.Process() scheduling
        // tie instead of this passive double, whose Act() always returns false and would otherwise halt the 
        // scheduler before the buff ever runs
        Actor.AddDelayed(d, float.Epsilon);
        return d;
    }

    [Fact]
    public void Cripple_halvesSpeed()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var d = PlaceDummy(TestLevel.At(1, 1));
        Assert.Equal(1f, d.Speed());

        Buff.Prolong<Cripple>(d, Cripple.Duration);
        Assert.Equal(0.5f, d.Speed());
    }

    [Fact]
    public void Add_Cripple_ShowsTheCrippledStatus()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        var d = PlaceDummy(TestLevel.At(1, 1));
        var view = (FakeCharView)d.Sprite;

        Buff.Prolong<Cripple>(d, Cripple.Duration);

        Assert.Contains((StatusColor.Negative, "crippled"), view.Statuses);
    }

    [Fact]
    public void Regeneration_HealsOnePerTenTurns_ButNotAtFullHealth()
    {
        LoadLevel("###", "#<#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT - 1;
        Buff.Affect<Regeneration>(hero);

        var regen = hero.GetBuff<Regeneration>();
        Assert.NotNull(regen);

        var before = hero.HP;
        regen.ActForTest();
        Assert.Equal(before + 1, hero.HP);

        regen.ActForTest();
        Assert.Equal(hero.HT, hero.HP); // already full, no overheal
    }

    [Fact]
    public void TerrorRecover_RemovesTerrorOnlyBelowItsDuration()
    {
        LoadLevel("###", "#.#", "###");
        var d = PlaceDummy(TestLevel.At(1, 1));

        Buff.Prolong<Terror>(d, Terror.Duration * 2);
        Terror.Recover(d);
        Assert.NotNull(d.GetBuff<Terror>()); // cooldown 20 >= DURATION 10, stays

        Buff.Detach<Terror>(d);
        Buff.Prolong<Terror>(d, Terror.Duration / 2);
        Terror.Recover(d);
        Assert.Null(d.GetBuff<Terror>()); // cooldown 5 < DURATION 10, removed
    }

    [Theory]
    [InlineData(typeof(Sleep))]
    [InlineData(typeof(Amok))]
    [InlineData(typeof(Cripple))]
    public void FlavourBuff_RoundTripsItsCooldown(Type type)
    {
        LoadLevel("###", "#.#", "###");
        var d = PlaceDummy(TestLevel.At(1, 1));
        var buff = (Buff)Activator.CreateInstance(type);
        buff.AttachTo(d);
        buff.Spend(7f);

        var bundle = new Bundle();
        buff.StoreInBundle(bundle);
        var restored = (Buff)Activator.CreateInstance(type);
        restored.RestoreFromBundle(bundle);

        var a = new Bundle();
        var b = new Bundle();
        buff.StoreInBundle(a);
        restored.StoreInBundle(b);
        Assert.Equal(a.GetFloat("time"), b.GetFloat("time"));
    }

    [Fact]
    public void Terror_RoundTripsItsObject()
    {
        LoadLevel("###", "#.#", "###");
        var d = PlaceDummy(TestLevel.At(1, 1));
        var terror = Buff.Affect<Terror>(d);
        terror.Object = 42;

        var bundle = new Bundle();
        terror.StoreInBundle(bundle);
        var restored = new Terror();
        restored.RestoreFromBundle(bundle);

        Assert.Equal(42, restored.Object);
    }

    [Fact]
    public void Bleeding_RoundTripsItsLevel()
    {
        LoadLevel("###", "#.#", "###");
        var d = PlaceDummy(TestLevel.At(1, 1));
        var bleeding = Buff.Affect<Bleeding>(d);
        bleeding.Set(6);

        var bundle = new Bundle();
        bleeding.StoreInBundle(bundle);
        var restored = new Bleeding();
        restored.RestoreFromBundle(bundle);

        var a = new Bundle();
        restored.StoreInBundle(a);
        Assert.Equal(6, a.GetInt("level"));
    }
}