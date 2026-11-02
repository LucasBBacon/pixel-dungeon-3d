using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Levitation : FlavorBuff
{
    public const float Duration = 20f;

    public override bool AttachTo(Char target)
    {
        if (base.AttachTo(target))
        {
            target.Flying = true;
            Buff.Detach<Roots>(target);
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Detach()
    {
        Target.Flying = false;
        Dungeon.Level.Press(Target.Pos, Target);
        base.Detach();
    }

    public override int Icon() => BuffIndicator.Levitation;

    public override string ToString() => "Levitating";
}