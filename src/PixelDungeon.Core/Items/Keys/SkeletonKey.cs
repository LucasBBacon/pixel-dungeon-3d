using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Keys;

public class SkeletonKey : Key
{
    public SkeletonKey()
    {
        NameValue = "skeleton key";
        ImageValue = ItemSpriteSheet.SkeletonKey;
    }

    public override string Info() =>
        "This key looks serious: its head is shaped like a skull. " +
        "Probably it can open some serious door.";
}