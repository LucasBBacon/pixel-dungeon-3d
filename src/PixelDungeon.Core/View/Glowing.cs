namespace PixelDungeon.Core.View;

public class Glowing
{
    public static readonly Glowing White = new(0xFFFFFF, 0.6f);

    public readonly int Color;
    public readonly float Red;
    public readonly float Green;
    public readonly float Blue;
    public readonly float Period;


    public Glowing(int color) : this(color, 1f)
    {
    }

    public Glowing(int color, float period)
    {
        Color = color;

        Red = (color >> 16) / 255f;
        Green = ((color >> 8) & 0xFF) / 255f;
        Blue = (color & 0xFF) / 255f;

        Period = period;
    }
}