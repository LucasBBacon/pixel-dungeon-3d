using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Vertigo : FlavorBuff
{
    public const float DurationConst = 10f;

    public override int Icon() => BuffIndicator.Vertigo;

    public override string ToString() => "Vertigo";

    public static float Duration(Char ch)
    {
        // TODO: a RingOfElements.Resistance buff on ch scales Duration by durationFactor()
        return DurationConst;
    }
}