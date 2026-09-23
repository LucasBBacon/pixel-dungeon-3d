using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;

namespace PixelDungeon.Core.Tests.Actors;

public class BuffTests : DungeonFixture
{
    private sealed class Dummy : Char
    {
        public HashSet<Type> ImmuneTo = [];

        protected override bool Act()
        {
            Spend(10);
            return false;
        }

        public override HashSet<Type> Immunities() => ImmuneTo;
    }

    private sealed class Sting : Buff
    {
    }

    private sealed class Haze : FlavorBuff
    {
    }

    [Fact]
    public void Append_AttachesAndSchedules_AndDetachRemoves()
    {
        var ch = new Dummy();
        Actor.Add(ch);
        var sting = Buff.Append<Sting>(ch);

        Assert.Same(ch, sting.Target);
        Assert.Same(sting, ch.GetBuff<Sting>());
        Assert.Null(ch.GetBuff<Haze>());

        sting.Detach();
        Assert.Null(ch.GetBuff<Sting>());
        Assert.DoesNotContain(sting, Actor.All());
    }

    [Fact]
    public void Affect_ReturnsTheExistingBuff()
    {
        var ch = new Dummy();
        var first = Buff.Affect<Sting>(ch);
        var second = Buff.Affect<Sting>(ch);

        Assert.Same(first, second);
        Assert.Single(ch.GetBuffs<Sting>());
    }

    [Fact]
    public void AttachTo_RespectsImmunities()
    {
        var ch = new Dummy();
        ch.ImmuneTo.Add(typeof(Sting));

        Assert.False(new Sting().AttachTo(ch));
        Assert.Null(ch.GetBuff<Sting>());
    }

    [Fact]
    public void FlavourBuff_DetachesWhenItActs()
    {
        var ch = new Dummy();
        Actor.Add(ch);

        Buff.Affect<Haze>(ch, 5f);
        Assert.NotNull(ch.GetBuff<Haze>());

        Actor.Process(); // ch acts at 0, spends 10, returns false and stays current, Haze is due at 5
        Assert.NotNull(ch.GetBuff<Haze>());

        ch.Next();
        Actor.Process(); // Haze acts at 5 and detaches itself, then ch acts at 10
        Assert.Null(ch.GetBuff<Haze>());
    }

    [Fact]
    public void RemovingTheChar_DetachesItsBuffs()
    {
        var ch = new Dummy();
        Actor.Add(ch);
        Buff.Append<Sting>(ch);

        Actor.Remove(ch);

        Assert.Empty(ch.Buffs());
    }
}