using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Rage : FlavorBuff
{
    public override int Icon() => BuffIndicator.Rage;

    public override string ToString() => "Blinded with rage";
}