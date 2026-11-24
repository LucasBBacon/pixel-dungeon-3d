using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class BombTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########",
    ];

    private static int C(int x) => TestLevel.At(x, 1);

    private sealed class ExposedBomb : Bomb
    {
        public void ThrowAt(int cell) => OnThrow(cell);
    }

    private sealed class Dummy : Char
    {
        public Dummy(int ht)
        {
            HP = HT = ht;
        }

        protected override bool Act() => false;
    }

    private static Dummy PlaceDummy(int cell, int ht = 100)
    {
        var dummy = new Dummy(ht) { Pos = cell };
        Actor.OccupyCell(dummy);
        return dummy;
    }

    private static int SeedWithDamage(int low, int high, int dr, out int expected)
    {
        for (var seed = 1; seed <= 200; seed++)
        {
            Random.Seed(seed);
            expected = Random.Int(low, high) - Random.Int(dr);
            if (expected > 0)
            {
                return seed;
            }
        }

        throw new InvalidOperationException("no seed");
    }

    [Fact]
    public void Bomb_Stats_MatchTheJava()
    {
        var bomb = new Bomb();

        Assert.Equal("bomb", bomb.Name());
        Assert.Equal(ItemSpriteSheet.Bomb, bomb.Image());
        Assert.Equal(Item.AcThrow, bomb.DefaultAction);
        Assert.True(bomb.Stackable);
        Assert.False(bomb.IsUpgradable());
        Assert.True(bomb.IsIdentified());
        bomb.Quantity(3);
        Assert.Equal(30, bomb.Price());
        Assert.Equal(
            "This is a relatively small bomb, filled with black powder. Conveniently, its fuse is lit automatically when the bomb is thrown.",
            bomb.Info());
    }

    [Fact]
    public void Bomb_DepthOne_DamagesTwoToElevenMinusDrRoll()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        Dungeon.Depth = 1;
        var dummy = PlaceDummy(C(5));
        var seed = SeedWithDamage(2, 12, dummy.Dr(), out var expected);

        Random.Seed(seed);
        new ExposedBomb().ThrowAt(C(5));

        Assert.Equal(100 - expected, dummy.HP);
        Assert.Contains(View.Sounds, s => s is { Id: Assets.SndBlast, Pitch: 1f });
        Assert.Contains((C(5), 0xFFEE7722u, 30), View.Splashes);
        Assert.Contains((C(5), 0xFF222222u, 4), View.Splashes);
        Assert.Contains((C(4), 0xFF222222u, 4), View.Splashes);
        Assert.DoesNotContain(View.Splashes, s => s.Cell == C(3));
    }

    [Fact]
    public void Bomb_Survivor_IsParalysedForTwo()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));
        var dummy = PlaceDummy(C(5));
        var seed = SeedWithDamage(2, 12, dummy.Dr(), out _);

        Random.Seed(seed);
        new ExposedBomb().ThrowAt(C(5));

        Assert.True(dummy.IsAlive());
        Assert.Equal(2f, CooldownOf(dummy.GetBuff<Paralysis>()));
    }

    [Fact]
    public void Bomb_DestroysFlammableTerrainInTheThreeByThree_AndObserves()
    {
        LoadLevel(
            "#########",
            "#.\",.....#",
            "#########");
        PlaceHero(C(1));
        Assert.False(Dungeon.Visible[C(5)]); // high grass at x = 2 blocks view
        Assert.False(Level.Flammable[C(5)]);

        new ExposedBomb().ThrowAt(C(3)); // grass at x = 3 and high grass at x = 2 in blast

        Assert.Equal(Terrain.Embers, Dungeon.Level.Map[C(2)]);
        Assert.Equal(Terrain.Embers, Dungeon.Level.Map[C(3)]);
        Assert.Contains(C(2), View.UpdatedCells);
        Assert.Contains(C(3), View.UpdatedCells);
        Assert.Empty(View.Discovered); // there is no DiscoverTile
        Assert.True(Dungeon.Visible[C(5)]); // Observe ran, view opened
    }

    [Fact]
    public void Bomb_OnBareFloor_ChangesNoTerrain_AndDoesNotObserve()
    {
        LoadLevel(Corridor);
        PlaceHero(C(1));

        new ExposedBomb().ThrowAt(C(5));

        Assert.Empty(View.UpdatedCells);
    }

    [Fact]
    public void Bomb_KillsTheThrower_RecordsTheBombCause()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(C(3));
        hero.Sprite = new FakeCharView();
        hero.HP = hero.HT = 1;
        Dungeon.Depth = 3;
        var seed = SeedWithDamage(4, 16, hero.Dr(), out _);

        Random.Seed(seed);
        new ExposedBomb().ThrowAt(C(3));

        Assert.False(hero.IsAlive());
        Assert.Equal(TextUtils.Format(ResultDescriptions.Bomb, 3), Dungeon.ResultDescription);
        Assert.True(View.Logged("You killed yourself with a bomb..."));
    }

    [Fact]
    public void Bomb_IntoAPit_FallsAsAnItem()
    {
        LoadLevel(
            "#########",
            "#...x...#",
            "#########");
        PlaceHero(C(1));
        Assert.True(Level.Pit[C(4)]);

        new ExposedBomb().ThrowAt(C(4));

        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndBlast);
        Assert.Empty(View.Splashes);
        Assert.Contains(Dungeon.DroppedItems.Values, items => items.Any(i => i is Bomb));
    }
    
    [Fact]
    public void Bomb_RandomQuantity_IsOneToThree()
    {
        for (var seed = 1; seed <= 50; seed++)
        {
            Random.Seed(seed);
            var bomb = new Bomb();
            Assert.Same(bomb, bomb.Random());
            Random.Seed(seed);
            Assert.Equal(Random.IntRange(1, 3), bomb.Quantity());
        }
    }
}