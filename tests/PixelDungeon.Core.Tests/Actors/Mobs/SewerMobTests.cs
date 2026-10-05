using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class SewerMobTests : DungeonFixture
{
    [Theory]
    [InlineData(typeof(Rat), 8, 8, 1, "marsupial rat")]
    [InlineData(typeof(Albino), 15, 8, 1, "albino rat")]
    [InlineData(typeof(Gnoll), 12, 11, 2, "gnoll scout")]
    [InlineData(typeof(Crab), 15, 12, 4, "sewer crab")]
    public void StatBlock_MatchesJava(Type type, int ht, int attackSkill, int dr, string name)
    {
        var mob = (Mob)Activator.CreateInstance(type);
        Assert.NotNull(mob);
        Assert.Equal(ht, mob.HT);
        Assert.Equal(ht, mob.HP);
        Assert.Equal(attackSkill, mob.AttackSkill(null));
        Assert.Equal(dr, mob.Dr());
        Assert.Equal(name, mob.Name);
    }

    [Theory]
    [InlineData(typeof(Rat), 1, 5)]
    [InlineData(typeof(Albino), 1, 5)]
    [InlineData(typeof(Gnoll), 2, 5)]
    [InlineData(typeof(Crab), 3, 6)]
    public void DamageRoll_StaysWithinTheRange(Type type, int min, int max)
    {
        var mob = (Mob)Activator.CreateInstance(type);
        Assert.NotNull(mob);
        for (var i = 0; i < 200; i++)
        {
            Assert.InRange(mob.DamageRoll(), min, max);
        }
    }

    [Fact]
    public void Crab_IsFastAndParries()
    {
        var crab = new Crab();
        Assert.Equal(2f, crab.Speed());
        Assert.Equal("parried", crab.DefenseVerb());
    }

    [Fact]
    public void Crab_WhenCrippled_slowsToNormalSpeed()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var crab = new Crab { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        Actor.Add(crab);

        Buff.Prolong<Cripple>(crab, Cripple.Duration);

        Assert.Equal(1f, crab.Speed());
    }

    [Fact]
    public void Albino_OnHit_InflictsBleedingExactlyOnTheOneInTwoRoll()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var albino = new Albino { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        Actor.Add(albino);

        const int seed = 7;
        const int rolls = 20;

        Random.Seed(seed);
        var expectedHits = new bool[rolls];
        for (var i = 0; i < rolls; i++)
        {
            expectedHits[i] = Random.Int(2) == 0;
        }

        // guard against a seed that happens to produce an all-true or all-false
        // sequence, which would make the replay unable to distinguish a correct
        // 1-in-2 roll from a mutant that always/never fires
        Assert.Contains(true, expectedHits);
        Assert.Contains(false, expectedHits);

        Random.Seed(seed);
        for (var i = 0; i < rolls; i++)
        {
            Buff.Detach<Bleeding>(hero);

            albino.AttackProc(hero, 4);

            var bled = hero.GetBuff<Bleeding>() != null;
            Assert.True(bled == expectedHits[i],
                $"roll {i}: expected bleeding={expectedHits[i]} but got {bled}");
        }
    }

    [Fact]
    public void Albino_InheritsRatsCombatNumbers()
    {
        var albino = new Albino();
        var rat = new Rat();
        Assert.Equal(rat.AttackSkill(null), albino.AttackSkill(null));
        Assert.Equal(rat.Dr(), albino.Dr());
    }
}