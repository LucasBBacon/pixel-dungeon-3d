using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Paralysis : FlavorBuff
{
    private const float DurationConst = 10f;

    public override bool AttachTo(Char target)
    {
        if (!base.AttachTo(target)) return false;
        
        target.Paralysed = true;
        return true;
    }

    public override void Detach()
    {
        base.Detach();
        Unfreeze(Target);
    }

    public override int Icon() => BuffIndicator.Paralysis;

    public override string ToString() => "Paralysed";

    public static float Duration(Char ch)
    {
        // TODO: a RingOfElements.Resistance buff on ch scales duration by durationFactor()
        return DurationConst;
    }

    public static void Unfreeze(Char ch)
    {
        if (ch.GetBuff<Paralysis>() == null && ch.GetBuff<Frost>() == null)
        {
            ch.Paralysed = false;
        }
    }
}