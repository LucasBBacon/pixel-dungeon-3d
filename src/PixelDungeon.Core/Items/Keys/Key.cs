using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Keys;

public class Key : Item
{
    public const float TimeToUnlock = 1f;

    public int Depth;

    public Key()
    {
        Stackable = false;
        Depth = Dungeon.Depth;
    }

    private const string TagDepth = "depth";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Depth = bundle.GetInt(TagDepth);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagDepth, Depth);
    }

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public override string Status() => Depth + "\u007F";
}