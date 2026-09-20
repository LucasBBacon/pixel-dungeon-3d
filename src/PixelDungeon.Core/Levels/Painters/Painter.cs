using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Painters;

public class Painter
{
    public static void Set(Level level, int cell, int value)
    {
        level.Map[cell] = value;
    }

    public static void Set(Level level, int x, int y, int value)
    {
        Set(level, x + y * Level.Width, value);
    }

    private static void Set(Level level, Point p, int value)
    {
        Set(level, p.X, p.Y, value);
    }

    private static void Fill(Level level, int x, int y, int w, int h, int value)
    {
        const int width = Level.Width;
        var pos = y * width + x;
        for (var i = y; i < y + h; i++, pos += width)
        {
            Array.Fill(level.Map, value, pos, w);
        }
    }

    public static void Fill(Level level, Rect rect, int value)
    {
        Fill(level,
            rect.Left,
            rect.Top,
            rect.Width() + 1,
            rect.Height() + 1,
            value);
    }

    public static void Fill(Level level, Rect rect, int m, int value)
    {
        Fill(level,
            rect.Left + m,
            rect.Top + m,
            rect.Width() + 1 - m * 2,
            rect.Height() + 1 - m * 2,
            value);
    }

    public static Point DrawInside(Level level, Rect room, Point from, int n, int value)
    {
        var step = new Point();
        if (from.X == room.Left)
        {
            step.Set(+1, 0);
        }
        else if (from.X == room.Right)
        {
            step.Set(-1, 0);
        }
        else if (from.Y == room.Top)
        {
            step.Set(0, +1);
        }
        else if (from.Y == room.Bottom)
        {
            step.Set(0, -1);
        }

        var p = new Point(from).Offset(step);
        for (var i = 0; i < n; i++)
        {
            if (value != -1)
            {
                Set(level, p, value);
            }

            p.Offset(step);
        }

        return p;
    }
}