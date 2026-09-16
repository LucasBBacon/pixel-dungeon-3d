namespace PixelDungeon.Core.Utils;

public static class GameMath
{
    public static float Gate(float min, float value, float max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}