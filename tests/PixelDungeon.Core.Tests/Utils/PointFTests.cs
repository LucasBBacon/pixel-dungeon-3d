using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class PointFTests
{
    private static void AssertClose(float expected, float actual)
    {
        Assert.InRange(actual, expected - 1e-5f, expected + 1e-5f);
    }

    [Fact]
    public void Polar_ZeroAngle_PointsAlongX()
    {
        var p = new PointF().Polar(0, 1);
        AssertClose(1, p.X);
        AssertClose(0, p.Y);
    }

    [Fact]
    public void Polar_RightAngle_PointsAlongY()
    {
        var p = new PointF().Polar(PointF.Pi / 2, 2);
        AssertClose(0, p.X);
        AssertClose(2, p.Y);
    }

    [Fact]
    public void Normalize_GivesUnitLength()
    {
        var p = new PointF(3, 4).Normalize();
        AssertClose(0.6f, p.X);
        AssertClose(0.8f, p.Y);
        AssertClose(1, p.Length());
    }

    [Fact]
    public void Length_IsEuclidean()
    {
        AssertClose(5, new PointF(3, 4).Length());
    }

    [Fact]
    public void Floor_TruncatesTowardZero()
    {
        Assert.Equal(new Point(1, 0), new PointF(1.7f, -0.2f).Floor());
    }

    [Fact]
    public void Inter_Halfway_IsMidpoint()
    {
        var m = PointF.Inter(new PointF(0, 0), new PointF(4, 2), 0.5f);
        AssertClose(2, m.X);
        AssertClose(1, m.Y);
    }

    [Fact]
    public void Distance_IsEuclidean()
    {
        AssertClose(5, PointF.Distance(new PointF(0, 0), new PointF(3, 4)));
    }

    [Fact]
    public void Angle_StraightUp_IsHalfPi()
    {
        AssertClose(PointF.Pi / 2, PointF.Angle(new PointF(0, 0), new PointF(0, 1)));
    }

    [Fact]
    public void SumAndDiff_ReturnNewPoints()
    {
        var a = new PointF(1, 2);
        var b = new PointF(3, 5);
        var s = PointF.Sum(a, b);
        var d = PointF.Diff(b, a);
        AssertClose(4, s.X);
        AssertClose(7, s.Y);
        AssertClose(2, d.X);
        AssertClose(3, d.Y);
        AssertClose(1, a.X);
    }

    [Fact]
    public void ScaleInvScaleNegateOffset_AreFluent()
    {
        var p = new PointF(1, 2);
        Assert.Same(p, p.Scale(2).InvScale(4).Negate().Offset(1, 1).Offset(new PointF(0.5f, 0.5f)));
        AssertClose(1, p.X);
        AssertClose(0.5f, p.Y);
    }

    [Fact]
    public void Constructors_CopyFromPointAndPointF()
    {
        var fromInt = new PointF(new Point(2, 3));
        var copy = new PointF(fromInt);
        AssertClose(2, copy.X);
        AssertClose(3, copy.Y);
        Assert.Equal("2, 3", copy.ToString());
    }
}