using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Plants;

public class Plant : IBundlable
{
    public int Pos;

    public virtual void Activate(Char ch)
    {
    }

    public virtual void Wither()
    {
    }

    private const string TagPos = "pos";

    public virtual void RestoreFromBundle(Bundle bundle)
    {
        Pos = bundle.GetInt(TagPos);
    }

    public virtual void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagPos, Pos);
    }

    public class Seed : Item
    {
        public virtual Plant Couch(int pos)
        {
            return null;
        }
    }
}