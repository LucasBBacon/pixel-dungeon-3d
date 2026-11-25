using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Weakness : FlavorBuff
{
    private const float DurationConst = 40f;

    public override int Icon() => BuffIndicator.Weakness;

    public override string ToString() => "Weakened";

    public override bool AttachTo(Char target)
    {
        if (base.AttachTo(target))
        {
            var hero = (Hero.Hero)target;
            hero.Weakened = true;
            // TODO: hero.belongings.discharge() drains one charge from wand held
            return true;
        }
        else
        {
            return false;
        }
    }

    public override void Detach()
    {
        base.Detach();
        ((Hero.Hero)Target).Weakened = false;
    }

    public static float Duration(Char ch)
    {
        // TODO: RingOfElements.Resistance
        return DurationConst;
    }
}