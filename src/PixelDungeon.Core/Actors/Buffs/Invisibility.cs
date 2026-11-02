using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Invisibility : FlavorBuff
{
    public const float Duration = 15f;

    public override bool AttachTo(Char target)
    {
        if (base.AttachTo(target))
        {
            target.Invisible++;
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Detach()
    {
        Target.Invisible--;
        base.Detach();
    }

    public override int Icon() => BuffIndicator.Invisible;

    public override string ToString() => "Invisible";

    public static void Dispel()
    {
        var buff = Dungeon.Hero.GetBuff<Invisibility>();
        if (buff != null && Dungeon.Hero.VisibleEnemies() > 0)
        {
            buff.Detach();
        }
    }
}