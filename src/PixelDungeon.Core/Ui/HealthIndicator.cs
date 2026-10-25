namespace PixelDungeon.Core.Ui;

public static class HealthIndicator
{
    public static Char Target { get; private set; }

    public static void SetTarget(Char ch)
    {
        if (ch != null && ch.IsAlive())
        {
            Target = ch;
        }
        else
        {
            Target = null;
        }
    }
}