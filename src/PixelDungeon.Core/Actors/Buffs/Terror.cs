using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Buffs;

public class Terror : FlavorBuff
{
    public const float Duration = 10f;

    public int Object = 0;

    private const string TagObject = "object";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Object = bundle.GetInt(TagObject);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagObject, Object);
    }

    // TODO: icon() returning BuffIndicator.Terror

    public override string ToString() => "Terror";

    public static void Recover(Char target)
    {
        var terror = target.GetBuff<Terror>();
        if (terror != null && terror.Cooldown() < Duration)
        {
            target.Remove(terror);
        }
    }
}