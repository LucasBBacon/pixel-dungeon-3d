using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Blindness : FlavorBuff
{
    public override void Detach()
    {
        base.Detach();
        Dungeon.Observe();
    }

    public override int Icon() => BuffIndicator.Blindness;

    public override string ToString() => "Blinded";
}