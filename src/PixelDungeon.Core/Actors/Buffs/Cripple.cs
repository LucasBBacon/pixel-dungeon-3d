namespace PixelDungeon.Core.Actors.Buffs;

public class Cripple : FlavorBuff
{
    public const float Duration = 10f;

    // TODO: icon() returning BuffIndicator.Cripple

    public override string ToString() => "Crippled";
}