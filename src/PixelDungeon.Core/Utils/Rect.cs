namespace PixelDungeon.Core.Utils;

public class Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public Rect() : this(0, 0, 0, 0)
    {
    }

    public Rect(Rect rect) : this(rect.Left, rect.Top, rect.Right, rect.Bottom)
    {
    }

    public Rect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Width()
    {
        return Right - Left;
    }

    public int Height()
    {
        return Bottom - Top;
    }

    public int Square()
    {
        return (Right - Left) * (Bottom - Top);
    }

    private Rect Set(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        return this;
    }

    public bool IsEmpty()
    {
        return Right <= Left || Bottom <= Top;
    }

    public Rect SetEmpty()
    {
        Left = Right = Top = Bottom = 0;
        return this;
    }

    public Rect Intersect(Rect other)
    {
        var result = new Rect
        {
            Left = Math.Max(Left, other.Left),
            Right = Math.Min(Right, other.Right),
            Top = Math.Max(Top, other.Top),
            Bottom = Math.Min(Bottom, other.Bottom)
        };
        return result;
    }

    public Rect Union(int x, int y)
    {
        if (IsEmpty())
        {
            return Set(x, y, x + 1, y + 1);
        }

        if (x < Left)
        {
            Left = x;
        }
        else if (x >= Right)
        {
            Right = x + 1;
        }

        if (y < Top)
        {
            Top = y;
        }
        else if (y >= Bottom)
        {
            Bottom = y + 1;
        }

        return this;
    }

    public Rect Union(Point p)
    {
        return Union(p.X, p.Y);
    }

    public bool Inside(Point p)
    {
        return p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom;
    }

    public Rect Shrink(int d)
    {
        return new Rect(Left + d, Top + d, Right - d, Bottom - d);
    }

    public Rect Shrink() => Shrink(1);
}