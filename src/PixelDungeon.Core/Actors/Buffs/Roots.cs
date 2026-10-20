using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Roots : FlavorBuff
{
    public override bool AttachTo(Char target)
    {
        if (!target.Flying && base.AttachTo(target))
        {
            target.Rooted = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Detach()
    {
        Target.Rooted = false; // java clears flag before detaching
        base.Detach();
    }

    public override int Icon() => BuffIndicator.Roots;

    public override string ToString() => "Rooted";
}