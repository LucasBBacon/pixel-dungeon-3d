using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Cripple : FlavorBuff
{
    public const float Duration = 10f;

    public override int Icon() => BuffIndicator.Cripple;

    public override string ToString() => "Crippled";
}