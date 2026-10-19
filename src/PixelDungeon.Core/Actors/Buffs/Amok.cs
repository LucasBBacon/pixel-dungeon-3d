using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Amok : FlavorBuff
{
    public override int Icon() => BuffIndicator.Amok;

    public override string ToString() => "Amok";
}