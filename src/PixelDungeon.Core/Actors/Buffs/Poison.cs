using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Buffs;

public class Poison : Buff, Hero.Hero.IDoom
{
    public float Left;

    private const string TagLeft = "left";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Left = bundle.GetFloat(TagLeft);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagLeft, Left);
    }

    public void Set(float duration) => Left = duration;

    public override int Icon() => BuffIndicator.Poison;

    public override string ToString() => "Poisoned";

    protected override bool Act()
    {
        if (Target.IsAlive())
        {
            Target.Damage((int)(Left / 3) + 1, this);
            Spend(Tick);

            if ((Left -= Tick) <= 0)
            {
                Detach();
            }
        }
        else
        {
            Detach();
        }

        return true;
    }

    public static float DurationFactor(Char ch)
    {
        // TODO: a RingOfElements.Resistance buff on ch returns its durationFactor() instead of 1
        return 1;
    }

    public void OnDeath()
    {
        // TODO: Badges.validateDeathFromPoison()

        Dungeon.Fail(TextUtils.Format(ResultDescriptions.Poison, Dungeon.Depth));
        GLog.N("You died from poison...");
    }
}