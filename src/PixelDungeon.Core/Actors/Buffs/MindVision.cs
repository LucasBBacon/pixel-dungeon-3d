using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class MindVision : FlavorBuff
{
    public const float Duration = 20f;
    
    public int Distance = 2;

    public override int Icon() => BuffIndicator.MindVision;

    public override string ToString() => "Mind vision";

    public override void Detach()
    {
        base.Detach();
        Dungeon.Observe();
    }
}