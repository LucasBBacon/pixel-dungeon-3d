using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class BArrayTest
{
    private static readonly bool[] A = [true, true, false, false];
    private static readonly bool[] B = [true, false, true, false];
    private static readonly int[] N = [1, 2, 3, 2];

    [Fact]
    public void And_Or_Not_ComputeElementwise()
    {
        Assert.Equal([true, false, false, false], BArray.And(A, B, null));
        Assert.Equal([true, true, true, false], BArray.Or(A, B, null));
        Assert.Equal([false, false, true, true], BArray.Not(A, null));
    }

    [Fact]
    public void ResultBuffer_IsFilledAndReturned()
    {
        var result = new bool[4];
        Assert.Same(result, BArray.Or(A, B, result));
        Assert.Equal([true, true, true, false], result);
    }

    [Fact]
    public void Is_IsNot_IsOneOf_IsNotOneOf_CompareInts()
    {
        Assert.Equal([false, true, false, true], BArray.Is(N, null, 2));
        Assert.Equal([true, false, true, false], BArray.IsNot(N, null, 2));
        Assert.Equal([true, false, true, false], BArray.IsOneOf(N, null, 1, 3));
        Assert.Equal([false, true, false, true], BArray.IsNotOneOf(N, null, 1, 3));
    }
}