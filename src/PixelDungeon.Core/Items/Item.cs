using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items;

public class Item : IBundlable
{
    public string Name = "smth";

    public virtual int Price()
    {
        return 0;
    }

    public void RestoreFromBundle(Bundle bundle)
    {
    }

    public void StoreInBundle(Bundle bundle)
    {
    }
}