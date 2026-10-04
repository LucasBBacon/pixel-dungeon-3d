using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Buffs;

public class Bleeding : Buff
{
    protected int Level;

    private const string TagLevel = "level";

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagLevel, Level);
    }

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Level = bundle.GetInt(TagLevel);
    }

    public void Set(int level)
    {
        Level = level;
    }

    // TODO: icon() returning BuffIndicator.Bleeding

    public override string ToString() => "Bleeding";

    protected override bool Act()
    {
        if (Target.IsAlive())
        {
            if ((Level = Random.Int(Level / 2, Level)) > 0)
            {
                Target.Damage(Level, this);
                if (Target.Sprite.Visible)
                {
                    // Splash.at(center, -PI/2, PI/6, blood, min(10 * level / HT, 10)) in java
                    Target.Sprite.BloodBurst(Level);
                }

                if (Target == Dungeon.Hero && !Target.IsAlive())
                {
                    Dungeon.Fail(TextUtils.Format(ResultDescriptions.Bleeding, Dungeon.Depth));
                    GLog.N("You bled to death...");
                }

                Spend(Tick);
            }
            else
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
}