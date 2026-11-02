using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class GasesImmunity : FlavorBuff
{
    public const float Duration = 5f;

    public override int Icon() => BuffIndicator.Immunity;

    public override string ToString() => "Immune to gases";

    public static readonly HashSet<Type> Immunities =
    [
        typeof(Paralysis),
        typeof(ToxicGas),
        // TODO: Vertigo.class
    ];
}