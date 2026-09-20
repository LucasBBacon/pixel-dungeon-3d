using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Tests.Levels;

namespace PixelDungeon.Core.Tests.Mechanics;

public class ShadowCasterTests
{
    private static readonly string[] OpenRoom = BuildOpenRoom();

    private static string[] BuildOpenRoom()
    {
        var rows = new string[15];
        rows[0] = rows[14] = new string('#', 15);
        for (var y = 1; y < 14; y++)
        {
            rows[y] = "#" + new string('.', 13) + "#";
        }

        return rows;
    }

    [Fact]
    public void OpenRoom_SeesARoundedSquare()
    {
        TestLevel.FromRows(OpenRoom);
        var fov = new bool[Level.Length];
        ShadowCaster.CastShadow(7, 7, fov, 4);
        Assert.True(fov[TestLevel.At(7, 7)]);
        for (var dy = -3; dy <= 3; dy++)
        {
            for (var dx = -3; dx <= 3; dx++)
            {
                Assert.True(fov[TestLevel.At(7 + dx, 7 + dy)]);
            }
        }

        Assert.True(fov[TestLevel.At(7, 3)]); // up 4
        Assert.True(fov[TestLevel.At(9, 3)]); // distance 4 with other axis 2
        Assert.False(fov[TestLevel.At(10, 3)]); // distance 4 with other axis 3 rounded off
        Assert.False(fov[TestLevel.At(11, 3)]);
        Assert.False(fov[TestLevel.At(7, 2)]); // distance 5
        Assert.False(fov[TestLevel.At(12, 7)]);
    }

    [Fact]
    public void Distance1_SeesOnlyTheThreeByThreeBlock()
    {
        TestLevel.FromRows(OpenRoom);
        var fov = new bool[Level.Length];
        ShadowCaster.CastShadow(7, 7, fov, 1);
        var count = 0;
        for (var i = 0; i < Level.Length; i++)
        {
            if (fov[i])
            {
                count++;
            }
        }

        Assert.Equal(9, count);
        Assert.True(fov[TestLevel.At(6, 6)]);
        Assert.False(fov[TestLevel.At(7, 5)]);
    }

    [Fact]
    public void WallRow_HidesEverythingBehindIt()
    {
        var rows = (string[])OpenRoom.Clone();
        rows[9] = new string('#', 15);
        TestLevel.FromRows(rows);
        var fov = new bool[Level.Length];
        ShadowCaster.CastShadow(7, 7, fov, 4);
        Assert.True((fov[TestLevel.At(7, 8)]));
        Assert.True((fov[TestLevel.At(7, 9)])); // the wall itself is seen

        for (var x = 3; x <= 11; x++)
        {
            Assert.False(fov[TestLevel.At(x, 10)]);
            Assert.False(fov[TestLevel.At(x, 11)]);
        }
    }

    [Fact]
    public void CastShadow_ClearsPreviousVisibility()
    {
        TestLevel.FromRows(OpenRoom);
        var fov = new bool[Level.Length];
        Array.Fill(fov, true);
        ShadowCaster.CastShadow(7, 7, fov, 1);
        Assert.False(fov[TestLevel.At(1, 1)]);
    }
}