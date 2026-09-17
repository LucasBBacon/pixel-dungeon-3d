namespace PixelDungeon.Core.Utils;

public interface IBundlable
{
    void RestoreFromBundle(Bundle bundle);

    void StoreInBundle(Bundle bundle);
}