using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Blobs;

public class GasTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########"
    ];

    private sealed class Dummy : Char
    {
        public readonly HashSet<Type> Immune = new();

        public Dummy(int ht)
        {
            HP = HT = ht;
        }

        protected override bool Act() => false;

        public override HashSet<Type> Immunities() => Immune;
    }

    private static int C(int x) => TestLevel.At(x, 1);

    private static Dummy PlaceDummy(int cell, int ht)
    {
        var dummy = new Dummy(ht) { Pos = cell };
        Actor.AddDelayed(dummy, 100f);
        return dummy;
    }

    [Fact]
    public void ToxicGas_OnTheHeroCell_DealsHtPlusLevelDamageOverFort()
    {
        LoadLevel(Corridor);
        Dungeon.Depth = 3; // levelDamage = 5 + 3 & 5 = 20
        var hero = PlaceHero(C(4));
        hero.Spend(0.5f); // the gas's turn at 0 comes first
        GameScene.Add(Blob.Seed<ToxicGas>(C(4), 50));

        Actor.Process();

        // (HT 20 + 20) / 40 = 1
        Assert.Equal(19, hero.HP);
    }

    [Fact]
    public void ToxicGas_WithARemainder_AddsOnePointWhenTheRollFallsUnderIt()
    {
        LoadLevel(Corridor);
        Dungeon.Depth = 1;
        var hero = PlaceHero(C(1));
        hero.Spend(0.5f);
        var dummy = PlaceDummy(C(5), 59);
        GameScene.Add(Blob.Seed<ToxicGas>(C(5), 50));

        // (59 + 10) % 40 = 29
        // Nothing draws before this roll in the gas's turn
        Random.Seed(4);
        var bonus = Random.Int(40) < 29;
        Random.Seed(4);

        Actor.Process();

        Assert.Equal(59 - (1 + (bonus ? 1 : 0)), dummy.HP); // (59 + 10) / 40 = 1
    }

    [Fact]
    public void ToxicGas_OnACharImmuneToIt_dealsNothing()
    {
        LoadLevel(Corridor);
        Dungeon.Depth = 3;
        var hero = PlaceHero(C(1));
        hero.Spend(0.5f);
        var dummy = PlaceDummy(C(5), 20);
        dummy.Immune.Add(typeof(ToxicGas));
        GameScene.Add(Blob.Seed<ToxicGas>(C(5), 50));

        Actor.Process();

        Assert.Equal(20, dummy.HP);
    }

    [Fact]
    public void ToxicGas_KillingTheHero_RecordsTheGasCause()
    {
        LoadLevel(Corridor);
        Dungeon.Depth = 3;
        var hero = PlaceHero(C(4));
        hero.HP = 1;
        hero.Spend(0.5f);
        GameScene.Add(Blob.Seed<ToxicGas>(C(4), 50));

        Actor.Process();

        Assert.False(hero.IsAlive());
        Assert.Equal("Died from toxic gas on level 3", Dungeon.ResultDescription);
        Assert.Contains(("You died from a toxic gas..", LogKind.Negative), View.Logs);
        Assert.Equal(1, View.GameOvers);
    }

    [Fact]
    public void ToxicGas_OverParalyticGas_ZeroesTheWeakerGasPerCell_AsTheJavaDoes()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1)); // no gas on this cell before the spread, so no damage and no roll
        hero.Spend(0.5f);
        var paralytic = Blob.Seed<ParalyticGas>(C(2), 50); // in Level.Blobs, never scheduled here
        paralytic.Seed(C(6), 10);
        var toxic = Blob.Seed<ToxicGas>(C(2), 30);
        toxic.Seed(C(6), 30);
        GameScene.Add(toxic);

        Actor.Process();

        // (1,1) sees 30 on two open cells, 30 / 2 - 1 = 14 -> (2,1), (3,1), (5,1), (6,1)
        // see 30 among three, 9 -> (7,1) sees 30 among two, 14 -> (4,1) sees nothing -> Volume 64.
        Assert.Equal(new[] { 14, 9, 9, 0, 9, 9, 14 }, Enumerable.Range(1, 7).Select(x => toxic.Cur[C(x)]));
        // against cells from before spread
        // At (2,1) paralytic 50 >= toxic 30, so 30 comes off the toxic Volume
        // but only the old array is zeroed and the new cloud keeps its 9
        // at (6,1) toxic 30 > paralytic 10, so that paralytic cell is zeroed and 10 comes off its Volume
        Assert.Equal(34, toxic.Volume);
        Assert.Equal(50, paralytic.Cur[C(2)]);
        Assert.Equal(0, paralytic.Cur[C(6)]);
        Assert.Equal(50, paralytic.Volume);
    }

    [Fact]
    public void ParalyticGas_OnACharsCell_ProlongsParalysisByItsDuration()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(1));
        hero.Spend(0.5f);
        var dummy = PlaceDummy(C(5), 20);
        GameScene.Add(Blob.Seed<ParalyticGas>(C(5), 50));

        Actor.Process();

        var paralysis = dummy.GetBuff<Paralysis>();
        Assert.NotNull(paralysis);
        Assert.True(dummy.Paralysed);
        // Prolong postponed it to the gas's turn (0) plus Paralysis.Duration (10)
        // the clock now stands at the hero's 0.5.
        Assert.Equal(9.5f, CooldownOf(paralysis));
    }

    [Fact]
    public void Gases_DescribeTheirTiles()
    {
        Assert.Equal("A greenish cloud of toxic gas is swirling here.", new ToxicGas().TileDesc());
        Assert.Equal("A cloud of paralytic gas is swirling here.", new ParalyticGas().TileDesc());
    }
}