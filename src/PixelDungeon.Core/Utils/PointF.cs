namespace PixelDungeon.Core.Utils;

public class PointF
{
    public float X;
    public float Y;

    public const float Pi = 3.1415926f;

    public PointF()
    {
    }

    public PointF(float x, float y)
    {
        X = x;
        Y = y;
    }

    public PointF(PointF p)
    {
        X = p.X;
        Y = p.Y;
    }

    public PointF(Point p)
    {
        X = p.X;
        Y = p.Y;
    }

    public PointF Clone()
    {
        return new PointF(this);
    }

    public PointF Scale(float f)
    {
        X *= f;
        Y *= f;
        return this;
    }

    public PointF InvScale(float f)
    {
        X /= f;
        Y /= f;
        return this;
    }

    public PointF Set(float x, float y)
    {
        X = x;
        Y = y;
        return this;
    }

    public PointF Set(PointF p)
    {
        X = p.X;
        Y = p.Y;
        return this;
    }

    public PointF Set(float v)
    {
        X = v;
        Y = v;
        return this;
    }

    public PointF Polar(float a, float l)
    {
        X = l * MathF.Cos(a);
        Y = l * MathF.Sin(a);
        return this;
    }

    public PointF Offset(float dx, float dy)
    {
        X += dx;
        Y += dy;
        return this;
    }

    public PointF Offset(PointF p)
    {
        X += p.X;
        Y += p.Y;
        return this;
    }

    public PointF Negate()
    {
        X = -X;
        Y = -Y;
        return this;
    }

    public PointF Normalize()
    {
        var l = Length();
        X /= l;
        Y /= l;
        return this;
    }

    public Point Floor()
    {
        return new Point((int)X, (int)Y);
    }

    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y);
    }

    public static PointF Sum(PointF a, PointF b)
    {
        return new PointF(a.X + b.X, a.Y + b.Y);
    }

    public static PointF Diff(PointF a, PointF b)
    {
        return new PointF(a.X - b.X, a.Y - b.Y);
    }

    public static PointF Inter(PointF a, PointF b, float d)
    {
        return new PointF(a.X + (b.X - a.X) * d, a.Y + (b.Y - a.Y) * d);
    }

    public static float Distance(PointF a, PointF b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static float Angle(PointF start, PointF end)
    {
        return (float)MathF.Atan2(end.Y - start.Y, end.X - start.X);
    }

    public override string ToString()
    {
        return X + ", " + Y;
    }
}