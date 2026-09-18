using PixelDungeon.Core;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests;

public class StatisticsTest
{
    [Fact]
    public void Reset_ClearsEveryCounter()
    {
        Statistics.DeepestFloor = 7;
        Statistics.Duration = 12.5f;
        Statistics.AmuletObtained = true;
        Statistics.Reset();
        Assert.Equal(0, Statistics.DeepestFloor);
        Assert.Equal(0f, Statistics.Duration);
        Assert.False(Statistics.AmuletObtained);
        Assert.False(Statistics.QualifiedForNoKilling);
    }

    [Fact]
    public void Bundle_RoundTripsWithJavaKeys()
    {
        Statistics.Reset();
        Statistics.GoldCollected = 3;
        Statistics.DeepestFloor = 9;
        Statistics.EnemiesSlain = 4;
        Statistics.Duration = 1.5f;
        Statistics.AmuletObtained = true;

        var bundle = new Bundle();
        Statistics.StoreInBundle(bundle);
        Assert.Equal(9, bundle.GetInt("maxDepth"));
        Assert.Equal(3, bundle.GetInt("score"));

        Statistics.Reset();
        Statistics.RestoreFromBundle(bundle);
        Assert.Equal(9, Statistics.DeepestFloor);
        Assert.Equal(4, Statistics.EnemiesSlain);
        Assert.Equal(1.5f, Statistics.Duration);
        Assert.True(Statistics.AmuletObtained);
        Statistics.Reset();
    }
}