using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class GasPotionTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########"
    ];

    private static int C(int x) => TestLevel.At(x, 1);

    private Hero PlaceHeroInCorridor(int x)
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(x));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    // ---- Toxic Gas, Paralytic Gas, Liquid Flame and Frost ----

    [Fact]
    public void ToxicGas_ShatterInSight_LearnsItSplashesAndSeedsAThousand()
    {
        PlaceHeroInCorridor(1);
        var potion = new PotionOfToxicGas();

        potion.Shatter(C(4));

        Assert.True(potion.IsKnown());
        Assert.Equal((C(4), Potion.SplashColor(potion.Image()), 5), Assert.Single(View.Splashes));
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndShatter);
        var gas = Assert.IsType<ToxicGas>(Dungeon.Level.Blobs[typeof(ToxicGas)]);
        Assert.Equal(1000, gas.Cur[C(4)]);
        Assert.Same(gas, Assert.Single(View.AddedBlobs));
        Assert.Contains(gas, Actor.All());
        Assert.Empty(View.Logs); // no "splashes harmlessly"
    }

    [Fact]
    public void ToxicGas_ShatterOutOfSight_SeedsTheGasWithoutLearningIt()
    {
        LoadLevel(
            "#########",
            "#.#.....#",
            "#########");
        PlaceHero(C(1));
        var potion = new PotionOfToxicGas();

        potion.Shatter(C(5));

        Assert.False(potion.IsKnown());
        Assert.Empty(View.Splashes);
        Assert.Equal(1000, Dungeon.Level.Blobs[typeof(ToxicGas)].Cur[C(5)]);
    }

    [Fact]
    public void ParalyticGas_ShatterInSight_LearnsItAndSeedsAThousand()
    {
        PlaceHeroInCorridor(1);
        var potion = new PotionOfParalyticGas();

        potion.Shatter(C(4));

        Assert.True(potion.IsKnown());
        Assert.Single(View.Splashes);
        Assert.Equal(1000, Dungeon.Level.Blobs[typeof(ParalyticGas)].Cur[C(4)]);
    }

    [Fact]
    public void ToxicGas_Drunk_ReleasesTheGasOnTheHero()
    {
        var hero = PlaceHeroInCorridor(3);
        var potion = new PotionOfToxicGas();
        potion.Collect();

        potion.Execute(hero);

        Assert.True(potion.IsKnown());
        Assert.Equal(1000, Dungeon.Level.Blobs[typeof(ToxicGas)].Cur[hero.Pos]);
    }

    [Fact]
    public void LiquidFlame_ShatterInSight_LearnsItAndSplashes_ButStartsNoFireYet()
    {
        PlaceHeroInCorridor(1);
        var potion = new PotionOfLiquidFlame();

        potion.Shatter(C(4));

        Assert.True(potion.IsKnown());
        Assert.Single(View.Splashes);
        Assert.Empty(Dungeon.Level.Blobs);
    }

    [Fact]
    public void Frost_Shatter_DoesNothingUntilFreezingExists()
    {
        PlaceHeroInCorridor(1);
        var potion = new PotionOfFrost();

        potion.Shatter(C(4));

        Assert.False(potion.IsKnown());
        Assert.Empty(View.Splashes);
        Assert.Empty(View.Logs);
    }

    // ---- purification ----

    [Fact]
    public void Purity_ClearingGasWithTheHeroInReach_LearnsItAndLogsFreshnessAsPositive()
    {
        PlaceHeroInCorridor(2); // distance 2 from the shatter at (4,1)
        var toxic = Blob.Seed<ToxicGas>(C(3), 40);
        toxic.Seed(C(7), 40); // three cells away: out of reach
        var paralytic = Blob.Seed<ParalyticGas>(C(5), 30);
        var potion = new PotionOfPurity();

        potion.Shatter(C(4));

        Assert.Equal(0, toxic.Cur[C(3)]);
        Assert.Equal(40, toxic.Cur[C(7)]);
        Assert.Equal(40, toxic.Volume);
        Assert.Equal(0, paralytic.Cur[C(5)]);
        Assert.Equal(0, paralytic.Volume);
        Assert.Equal(new[] { (EffectKind.Discover, C(3)), (EffectKind.Discover, C(5)) }, View.Effects);
        Assert.Single(View.Splashes);
        Assert.True(potion.IsKnown());
        Assert.Contains(("You feel uncommon freshness in the air.", LogKind.Positive), View.Logs);
    }

    [Fact]
    public void Purity_ClearingGasBeyondTheHerosReach_LearnsItWithoutAMessage()
    {
        PlaceHeroInCorridor(1); // distance 4 from the shatter at (5,1)
        Blob.Seed<ToxicGas>(C(5), 40);
        var potion = new PotionOfPurity();

        potion.Shatter(C(5));

        Assert.True(potion.IsKnown());
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Purity_WithNoGasAndTheHeroInReach_ShattersHarmlesslyThenLearnsIt()
    {
        PlaceHeroInCorridor(3);
        var potion = new PotionOfPurity();
        var name = potion.Name();
        var color = name[..^" potion".Length];

        potion.Shatter(C(4));

        Assert.Equal(
            new[]
            {
                ("The flask shatters and " + color + " liquid splashes harmlessly", LogKind.Info),
                ("You feel uncommon freshness in the air.", LogKind.Info),
            },
            View.Logs);
        Assert.True(potion.IsKnown());
    }

    [Fact]
    public void Purity_WithNoGasAndTheHeroOutOfReach_OnlyShattersHarmlessly()
    {
        PlaceHeroInCorridor(1);
        var potion = new PotionOfPurity();

        potion.Shatter(C(5));

        Assert.Single(View.Logs);
        Assert.False(potion.IsKnown());
    }

    [Fact]
    public void Purity_ReachesTwoCellsThroughOpenGround_NotThroughWalls()
    {
        LoadLevel(
            "#######",
            "#..#..#",
            "#######");
        PlaceHero(C(1));
        var toxic = Blob.Seed<ToxicGas>(C(4), 40); // two cells from (2,1), but behind the wall at (3,1)
        var potion = new PotionOfPurity();

        potion.Shatter(C(2));

        Assert.Equal(40, toxic.Cur[C(4)]);
        Assert.Empty(View.Effects);
    }

    [Fact]
    public void Purity_Drunk_GivesGasImmunity_AndWarnsThatSmellsAreGone()
    {
        var hero = PlaceHeroInCorridor(3);
        var potion = new PotionOfPurity();
        potion.Collect();

        potion.Execute(hero);

        Assert.Equal(GasesImmunity.Duration, CooldownOf(hero.GetBuff<GasesImmunity>()));
        Assert.Contains(("You've stopped sensing any smells!", LogKind.Warning), View.Logs);
        Assert.True(potion.IsKnown());
        Assert.Empty(View.Splashes); // drinking Purification never shatters it
    }
}