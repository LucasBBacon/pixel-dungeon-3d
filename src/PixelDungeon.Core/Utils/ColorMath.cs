namespace PixelDungeon.Core.Utils;

public static class ColorMath
{
    public static uint Midpoint(int a, int b)
    {
        var result = 0xFF000000;
        for (var shift = 0; shift <= 16; shift += 8)
        {
            result |= (uint)((((a >> shift) & 0xFF) + ((b >> shift) & 0xFF)) / 2) << shift;
        }

        return result;
    }
}