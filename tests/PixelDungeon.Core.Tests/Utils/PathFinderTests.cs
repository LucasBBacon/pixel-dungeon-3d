using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class PathFinderTests
{
    private const int W = 8;

    private static int At(int x, int y) => y * W + x;

    private static int Cheb(int a, int b)
    {
        return Math.Max(Math.Abs(a % W - b % W), Math.Abs(a / W - b / W));
    }

    private static bool[] Grid(params string[] rows)
    {
        var passable = new bool[W * rows.Length];
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < W; x++)
            {
                passable[y * W + x] = rows[y][x] == '.';
            }
        }

        PathFinder.SetMapSize(W, rows.Length);
        return passable;
    }

    private static readonly string[] Corridor =
    [
        "########",
        "#......#",
        "########",
        "########",
        "########",
        "########",
        "########",
        "########"
    ];

    private static readonly string[] Detour =
    [
        "########",
        "#..#...#",
        "#..#...#",
        "#..#...#",
        "#......#",
        "#..#...#",
        "#..#...#",
        "########"
    ];

    private static readonly string[] Sealed =
    [
        "########",
        "#......#",
        "#......#",
        "#......#",
        "#......#",
        "#....###",
        "#....#.#",
        "########"
    ];

    private static readonly string[] Open =
    [
        "########",
        "#......#",
        "#......#",
        "#......#",
        "#......#",
        "#......#",
        "#......#",
        "########"
    ];

    [Fact]
    public void Find_StraightCorridor_ReturnsEachStepUpToTarget()
    {
        var grid = Grid(Corridor);
        var path = PathFinder.Find(At(1, 1), At(6, 1), grid);
        Assert.Equal(
            new[] { At(2, 1), At(3, 1), At(4, 1), At(5, 1), At(6, 1) },
            path
        );
    }

    [Fact]
    public void Find_Detour_RoutesThroughTheGap()
    {
        var grid = Grid(Detour);
        var from = At(1, 1);
        var to = At(5, 1);
        var path = PathFinder.Find(from, to, grid);

        Assert.NotNull(path);
        Assert.Equal(6, path.Count);
        Assert.Equal(to, path[^1]);
        var previous = from;
        foreach (var cell in path)
        {
            Assert.Equal(1, Cheb(previous, cell));
            Assert.True(grid[cell]);
            previous = cell;
        }

        Assert.Contains(At(3, 4), path);
    }

    [Fact]
    public void Find_SealedTarget_ReturnsNull()
    {
        Assert.Null(PathFinder.Find(At(1, 1), At(6, 6), Grid(Sealed)));
    }

    [Fact]
    public void Find_FromEqualsTo_ReturnsNull()
    {
        Assert.Null(PathFinder.Find(At(1, 1), At(1, 1), Grid(Open)));
    }

    [Fact]
    public void GetStep_Corridor_ReturnsNextCell()
    {
        Assert.Equal(At(2, 1), PathFinder.GetStep(At(1, 1), At(6, 1), Grid(Corridor)));
    }

    [Fact]
    public void GetStep_Sealed_ReturnsMinusOne()
    {
        Assert.Equal(-1, PathFinder.GetStep(At(1, 1), At(6, 6), Grid(Sealed)));
    }

    [Fact]
    public void GetStepBack_MovesAwayFromThreat()
    {
        var grid = Grid(Open);
        var cur = At(3, 3);
        var threat = At(2, 2);
        var back = PathFinder.GetStepBack(cur, threat, grid);

        Assert.Equal(1, Cheb(cur, back));
        Assert.True(Cheb(back, threat) > Cheb(cur, threat));
        Assert.True(grid[back]);
    }

    [Fact]
    public void BuildDistanceMap_WithLimit_LeavesFarCellsUnreached()
    {
        var grid = Grid(Open);
        PathFinder.BuildDistanceMap(At(1, 1), grid, 2);
        Assert.Equal(0, PathFinder.Distance[At(1, 1)]);
        Assert.Equal(1, PathFinder.Distance[At(2, 2)]);
        Assert.Equal(2, PathFinder.Distance[At(3, 3)]);
        Assert.Equal(int.MaxValue, PathFinder.Distance[At(4, 4)]);
        Assert.Equal(int.MaxValue, PathFinder.Distance[At(0, 0)]);
    }

    [Fact]
    public void SetMapSize_newSize_ReallocatesDistance()
    {
        PathFinder.SetMapSize(8, 8);
        PathFinder.SetMapSize(4, 4);
        Assert.Equal(16, PathFinder.Distance.Length);
        PathFinder.SetMapSize(8, 8);
        Assert.Equal(64, PathFinder.Distance.Length);
    }
}