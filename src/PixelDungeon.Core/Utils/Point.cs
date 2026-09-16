#nullable enable
namespace PixelDungeon.Core.Utils;

public class Point
{
    public int X;
    public int Y;

    public Point()
    {
    }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Point(Point p)
    {
        X = p.X;
        Y = p.Y;
    }

    public Point Set(int x, int y)
    {
        X = x;
        Y = y;
        return this;
    }

    public Point Set(Point p)
    {
        X = p.X;
        Y = p.Y;
        return this;
    }

    public Point Clone()
    {
        return new Point(this);
    }

    public Point Scale(float f)
    {
        // java truncates toward 0 after float multiply: this.x *= f 
        X = (int)(X * f);
        Y = (int)(Y * f);
        return this;
    }

    public Point Offset(int dx, int dy)
    {
        X += dx;
        Y += dy;
        return this;
    }

    public Point Offset(Point d)
    {
        X += d.X;
        Y += d.Y;
        return this;
    }

    public override bool Equals(object? obj)
    {
        return obj is Point p && p.X == X && p.Y == Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Point a, Point b)
    {
        return a is null ? b is null : a.Equals(b);
    }

    public static bool operator !=(Point a, Point b)
    {
        return !(a == b);
    }
}