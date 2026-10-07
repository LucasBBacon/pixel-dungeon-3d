using PixelDungeon.Core.Actors.Mobs;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class BestiaryTests : DungeonFixture
{
    [Fact]
    public void Mob_AtDepthOne_IsAlwaysARat()
    {
        for (var i = 0; i < 100; i++)
        {
            Assert.IsType<Rat>(Bestiary.Mob(1));
        }
    }

    [Fact]
    public void Mob_AtDepthTwo_IsARatOrGnoll()
    {
        var seen = new HashSet<Type>();
        for (var i = 0; i < 200; i++)
        {
            seen.Add(Bestiary.Mob(2).GetType());
        }

        Assert.Equal([typeof(Rat), typeof(Gnoll)], seen);
    }

    [Fact]
    public void Mob_AtDepthThree_CanYieldCrabAndSwarm()
    {
        var seen = new HashSet<Type>();
        for (var i = 0; i < 5000; i++)
        {
            seen.Add(Bestiary.Mob(3).GetType());
        }

        Assert.Contains(typeof(Crab), seen);
        Assert.Contains(typeof(Swarm), seen);
    }

    [Fact]
    public void Mob_AtDepthFour_ExcludesSkeletonAndThiefUntilFuture()
    {
        // Documented deviation: Java's depth-4 table also carries
        // Skeleton (0.01) and Thief (0.01) out of 6.04 total weight. Neither
        // class is ported yet, so the registry drops them from the roll.
        var seen = new HashSet<Type>();
        for (var i = 0; i < 5000; i++)
        {
            seen.Add(Bestiary.Mob(4).GetType());
        }

        Assert.Equal(
            [typeof(Rat), typeof(Gnoll), typeof(Crab), typeof(Swarm)],
            seen);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(18)]
    [InlineData(25)]
    public void Mob_AtUnportedDepth_ReturnsNull(int depth)
    {
        Assert.Null(Bestiary.Mob(depth));
    }

    [Fact]
    public void Mutable_PromotesARatToAnAlbinoOnTheOneInThirtyRoll()
    {
        var seen = new HashSet<Type>();
        for (var i = 0; i < 5000; i++)
        {
            seen.Add(Bestiary.Mutable(1).GetType());
        }

        Assert.Equal([typeof(Rat), typeof(Albino)], seen);
    }

    [Fact]
    public void IsBoss_IsFalseForEveryPortedMob()
    {
        Assert.False(Bestiary.IsBoss(new Rat()));
        Assert.False(Bestiary.IsBoss(new Crab()));
        Assert.False(Bestiary.IsBoss(new Swarm()));
    }

    // table names unported classes as strings, because C# cannot typeof() a class that doesn't exist yet
    // A typo would silently drop that mob from its depth for future features,
    // so every name must be either registered or on this list
    // transcribed by hand from Bestiary.java (a second, independent reading; see the
    // task-7 report for how this list compares to the task brief's own transcription).
    private static readonly HashSet<string> NotYetPorted =
    [
        // Depth-table entries (mobClass' switch):
        "Skeleton", "Thief", "Goo", "Shaman", "Tengu", "Bat", "Brute", "Spinner",
        "Elemental", "Monk", "DM300", "Warlock", "Golem", "Succubus", "King",
        "Eye", "Scorpio", "Yog",
        // mutable()'s promotion targets (not in the depth table itself, but
        // named in Bestiary.java and not yet ported):
        "Bandit", "Shielded", "Senior", "Acidic"
    ];

    [Fact]
    public void EveryNameInTheTable_IsEitherRegisteredOrKnownUnported()
    {
        foreach (var name in Bestiary.TableNamesForTest())
        {
            Assert.True(Bestiary.IsRegisteredForTest(name) || NotYetPorted.Contains(name),
                $"'{name}' is neither a registered mob nor known unsupported class, likely a typo");
        }
    }
}