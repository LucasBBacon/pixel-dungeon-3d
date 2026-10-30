using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Blobs;

public class BlobTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########"
    ];

    private sealed class TestBlob : Blob
    {
        public bool ActForTest() => Act();
        public int[] OffForTest => Off;
    }

    private static int C(int x) => TestLevel.At(x, 1);

    [Fact]
    public void Act_SpreadsEachOpenCellToTheAverageOfItselfAndItsOpenNeighboursLessOne_ThenSwaps()
    {
        LoadLevel(Corridor);
        var blob = new TestBlob();
        blob.Seed(C(3), 90);

        blob.ActForTest();

        // The walls above and below count for nothing
        // (2,1), (3,1) and (4,1) each see 90 among three open cells: 90 / 3 - 1 = 29
        // (1,1) & (5,1) see a sum of 0, below count, so 0
        Assert.Equal([0, 29, 29, 29, 0, 0, 0], Enumerable.Range(1, 7).Select(x => blob.Cur[C(x)]));
        Assert.Equal(87, blob.Volume);
        Assert.Equal(90, blob.OffForTest[C(3)]);
    }

    [Fact]
    public void Act_WithNoVolume_SpendsATurnAndLEavesTheCellsAlone()
    {
        LoadLevel(Corridor);
        var blob = new TestBlob();
        var cells = blob.Cur;

        Assert.True(blob.ActForTest());

        Assert.Equal(1f, CooldownOf(blob));
        Assert.Same(cells, blob.Cur);
        Assert.Equal(0, blob.Volume);
    }

    [Fact]
    public void SeedAndClear_KeepTheVolumeInStepWithTheCells()
    {
        var blob = new TestBlob();

        blob.Seed(C(2), 30);
        blob.Seed(C(2), 5);
        blob.Seed(C(5), 7);
        Assert.Equal(35, blob.Cur[C(2)]);
        Assert.Equal(42, blob.Volume);

        blob.Clear(C(2));

        Assert.Equal(0, blob.Cur[C(2)]);
        Assert.Equal(7, blob.Volume);
    }

    [Fact]
    public void Bundle_StoresTheCellsFromTheFirstToTheLastWithGas_AndRestoresThemByClassName()
    {
        var blob = new ToxicGas();
        blob.Seed(C(3), 5);
        blob.Seed(C(6), 7);
        var bundle = new Bundle();

        bundle.Put("gas", blob);
        var restored = (ToxicGas)bundle.Get("gas");

        var stored = bundle.GetBundle("gas");
        Assert.Equal(C(3), stored.GetInt("start"));
        Assert.Equal(new[] { 5, 0, 0, 7 }, stored.GetIntArray("cur"));
        Assert.Equal(5, restored.Cur[C(3)]);
        Assert.Equal(7, restored.Cur[C(6)]);
        Assert.Equal(12, restored.Volume);
    }

    [Fact]
    public void Bundle_WithNoVolume_StoresNoCells()
    {
        var bundle = new Bundle();

        new ToxicGas().StoreInBundle(bundle);

        Assert.False(bundle.Contains("start"));
        Assert.False(bundle.Contains("cur"));
    }

    [Fact]
    public void TileDesc_OfTheBase_IsNull()
    {
        Assert.Null(new TestBlob().TileDesc());
    }
}