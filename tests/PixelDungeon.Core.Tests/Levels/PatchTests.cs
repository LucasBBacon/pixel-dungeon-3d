using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests.Levels;

public class PatchTests : DungeonFixture
{
    [Fact]
    public void Generate_Seed0_IsFalseAwayFromTheBorder_AndSeed1_FillsTheDeepInterior()
    {
        var none = Patch.Generate(0f, 3);
        for (var y = 2; y < Level.Height - 2; y++)
        {
            for (var x = 2; x < Level.Width - 2; x++)
            {
                Assert.False(none[TestLevel.At(x, y)]);
            }
        }

        var all = Patch.Generate(1f, 3);
        for (var y = 4; y < Level.Height - 4; y++)
        {
            for (var x = 4; x < Level.Width - 4; x++)
            {
                Assert.True(all[TestLevel.At(x, y)]);
            }
        }
    }

    [Fact]
    public void Generate_WithoutSmoothing_ReturnsTheRawField()
    {
        Random.Seed(7);
        var raw = Patch.Generate(0.5f, 0);
        var trues = raw.Count(b => b);
        Assert.InRange(trues, Level.Length * 3 / 10, Level.Length * 7 / 10);
    }

    [Fact]
    public void Generate_IsDeterministicForASeed_AwayFromTheBorder()
    {
        Random.Seed(3);
        var first = (bool[])Patch.Generate(0.45f, 5).Clone();
        Random.Seed(3);
        var second = Patch.Generate(0.45f, 5);
        // Five passes can carry a border difference at most five cells inward.
        for (var y = 6; y < Level.Height - 6; y++)
        {
            for (var x = 6; x < Level.Width - 6; x++)
            {
                Assert.Equal(first[TestLevel.At(x, y)], second[TestLevel.At(x, y)]);
            }
        }
    }
}