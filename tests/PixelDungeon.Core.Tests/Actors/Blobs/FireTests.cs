using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Blobs;

public class FireTests : DungeonFixture
{
    private static readonly string[] Floor =
    [
        "#########",
        "#.,.....#",
        "#########"
    ];

    private sealed class Dummy : Char
    {
        public Dummy(int ht)
        {
            HP = HT = ht;
        }

        protected override bool Act() => false;
    }

    private static int C(int x) => TestLevel.At(x, 1);

    private static void Tick(Actor actor) =>
        typeof(Actor)
            .GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(actor, null);

    private Fire SeedFire(int cell, int amount)
    {
        var fire = Blob.Seed<Fire>(cell, amount);
        GameScene.Add(fire);
        return fire;
    }
    
    [Fact]
    public void Fire_SeededTwoOnFloor_BurnsTwoTicksAndLeavesTheFloor()
    {
        LoadLevel(Floor);
        PlaceHero(C(7));
        var fire = SeedFire(C(1), 2);
        
        Assert.Equal(2, fire.Cur[C(1)]);
        Tick(fire);
        Assert.Equal(1, fire.Cur[C(1)]);
        Tick(fire);
        Assert.Equal(0, fire.Cur[C(1)]);
        
        Assert.Equal(Terrain.Empty, Dungeon.Level.Map[C(1)]);
    }

    [Fact]
    public void Fire_GrassNextToFire_CatchesAtFourAndBurnsToEmbersOnTheFifthTick()
    {
        LoadLevel(Floor);
        PlaceHero(C(7));
        Assert.True(Dungeon.Visible[C(2)]);
        var fire = SeedFire(C(1), 2);
        
        Tick(fire);
        Assert.Equal(4, fire.Cur[C(2)]);
        for (var i = 0; i < 3; i++)
        {
            Tick(fire);
        }
        Assert.Equal(1, fire.Cur[C(2)]);
        Assert.Equal(Terrain.Grass, Dungeon.Level.Map[C(2)]);

        Tick(fire); // fifth tick
        
        Assert.Equal(Terrain.Embers,  Dungeon.Level.Map[C(2)]);
        Assert.Contains(C(2), View.UpdatedCells);
        Assert.Contains((C(2), Terrain.Grass), View.Discovered);
    }

    [Fact]
    public void Fire_FirstEvolveOnFloorBesideOneGrass_Volume5()
    {
        LoadLevel(Floor);
        PlaceHero(C(7));
        var fire = SeedFire(C(1), 2);
        
        Tick(fire);
        
        Assert.Equal(5, fire.Volume);
    }

    [Fact]
    public void Fire_SeedOnABurningCell_DoesNotAccumulate()
    {
        var fire = new Fire();
        
        fire.Seed(C(3), 2);
        fire.Seed(C(3), 4);
        
        Assert.Equal(2, fire.Cur[C(3)]);
        Assert.Equal(2, fire.Volume);
    }

    [Fact]
    public void Fire_HighGrass_BurnsToEmbers()
    {
        LoadLevel(
            "#########",
            "#.\".....#",
            "#########"
        );
        PlaceHero(C(7));
        var fire = SeedFire(C(2), 1);

        Tick(fire);
        
        Assert.Equal(Terrain.Embers, Dungeon.Level.Map[C(2)]);
    }
    
    [Fact]
    public void Fire_DoorBesidesWater_FloodsIt()
    {
        LoadLevel(
            "#########",
            "#.+~....#",
            "#########"
        );
        PlaceHero(C(7));
        var fire = SeedFire(C(2), 1);
        
        Tick(fire);
        
        var tile = Dungeon.Level.Map[C(2)];
        Assert.InRange(tile, Terrain.WaterTiles, Terrain.WaterTiles + 15);
    }

    [Fact]
    public void Fire_DoorAwayFromWater_LeavesEmbers()
    {
        LoadLevel(
            "#########",
            "#.+.....#",
            "#########"
        );
        PlaceHero(C(7));
        var fire = SeedFire(C(2), 1);
        
        Tick(fire);
        
        Assert.Equal(Terrain.Embers, Dungeon.Level.Map[C(2)]);
    }
    
    [Fact]
    public void Fire_OpenDoorAwayFromWater_LeavesEmbers()
    {
        LoadLevel(
            "#########",
            "#./.....#",
            "#########"
        );
        PlaceHero(C(7));
        var fire = SeedFire(C(2), 1);
        
        Tick(fire);
        
        Assert.Equal(Terrain.Embers, Dungeon.Level.Map[C(2)]);
    }

    [Fact]
    public void Fire_CharacterOnTheCell_IsSetBurning()
    {
        LoadLevel(Floor);
        PlaceHero(C(7));
        var dummy = new Dummy(20) { Pos = C(4) };
        Actor.OccupyCell(dummy);
        var fire = SeedFire(C(4), 2);
        
        Tick(fire);

        Assert.NotNull(dummy.GetBuff<Burning>());
    }

    [Fact]
    public void Fire_TileDesc_IsVerbatim()
    {
        Assert.Equal("A fire is raging here.", new Fire().TileDesc());
    }

    [Fact]
    public void Fire_Bundle_RoundTrips()
    {
        var fire = new Fire();
        fire.Seed(C(3), 4);
        var bundle = new Bundle();
        
        bundle.Put("fire", fire);
        var restored = (Fire)bundle.Get("fire");
        
        Assert.Equal(4, restored.Cur[C(3)]);
        Assert.Equal(4, restored.Volume);
    }
}