namespace PixelDungeon.Core.Actors.Buffs;

public class FlavorBuff : Buff
{
    protected override bool Act()
    {
        Detach();
        return true;
    }
}