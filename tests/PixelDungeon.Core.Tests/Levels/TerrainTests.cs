using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests.Levels;

public class TerrainTests
{
    [Fact]
    public void Flags_MatchTheJavaTable()
    {
        Assert.Equal(Terrain.LosBlocking | Terrain.Solid | Terrain.Unstitchable, Terrain.Flags[Terrain.Wall]);
        Assert.Equal(Terrain.Passable | Terrain.LosBlocking | Terrain.Flammable | Terrain.Solid | Terrain.Unstitchable,
            Terrain.Flags[Terrain.Door]);
        Assert.Equal(Terrain.Flags[Terrain.Wall] | Terrain.Secret | Terrain.Unstitchable,
            Terrain.Flags[Terrain.SecretDoor]);
        Assert.Equal(Terrain.Avoid | Terrain.Pit | Terrain.Unstitchable, Terrain.Flags[Terrain.Chasm]);
        Assert.Equal(Terrain.Passable | Terrain.Liquid | Terrain.Unstitchable, Terrain.Flags[Terrain.Water]);
        Assert.Equal(Terrain.Avoid, Terrain.Flags[Terrain.ToxicTrap]);
        Assert.Equal(Terrain.Passable | Terrain.Secret, Terrain.Flags[Terrain.SecretToxicTrap]);
        Assert.Equal(Terrain.Passable | Terrain.LosBlocking | Terrain.Flammable, Terrain.Flags[Terrain.HighGrass]);
        Assert.Equal(0, Terrain.Flags[100]);
    }

    [Fact]
    public void Flags_WaterTiles_AllEqualWater()
    {
        for (var i = Terrain.WaterTiles; i < Terrain.WaterTiles + 16; i++)
        {
            Assert.Equal(Terrain.Flags[Terrain.Water], Terrain.Flags[i]);
        }
    }

    [Fact]
    public void Discover_MapsEverySecretToItsVisibleForm()
    {
        Assert.Equal(Terrain.Door, Terrain.Discover(Terrain.SecretDoor));
        Assert.Equal(Terrain.FireTrap, Terrain.Discover(Terrain.SecretFireTrap));
        Assert.Equal(Terrain.ParalyticTrap, Terrain.Discover(Terrain.SecretParalyticTrap));
        Assert.Equal(Terrain.ToxicTrap, Terrain.Discover(Terrain.SecretToxicTrap));
        Assert.Equal(Terrain.PoisonTrap, Terrain.Discover(Terrain.SecretPoisonTrap));
        Assert.Equal(Terrain.AlarmTrap, Terrain.Discover(Terrain.SecretAlarmTrap));
        Assert.Equal(Terrain.LightningTrap, Terrain.Discover(Terrain.SecretLightningTrap));
        Assert.Equal(Terrain.GrippingTrap, Terrain.Discover(Terrain.SecretGrippingTrap));
        Assert.Equal(Terrain.SummoningTrap, Terrain.Discover(Terrain.SecretSummoningTrap));
        Assert.Equal(Terrain.Empty, Terrain.Discover(Terrain.Empty));
    }
}