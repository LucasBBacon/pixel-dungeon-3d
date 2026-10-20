using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Slow : FlavorBuff
{
    private const float DurationConst = 10f;

    public override int Icon() => BuffIndicator.Slow;

    public override string ToString() => "Slowed";

    public static float Duration(Char ch)
    {
        // TODO: a RingOfElements.Resistance buff on ch scales DURATION by its durationFactor()
        return DurationConst;
    }
}