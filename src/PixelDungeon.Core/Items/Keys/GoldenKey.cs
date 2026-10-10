using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Keys;

public class GoldenKey : Key
{
    public GoldenKey()
    {
        NameValue = "golden key";
        ImageValue = ItemSpriteSheet.GoldenKey;
    }

    public override string Info() =>
        "The notches on this golden key are tiny and intricate. " +
        "Maybe it can open some chest lock?";
}