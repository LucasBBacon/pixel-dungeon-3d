using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Keys;

public class IronKey : Key
{
    private const string TxtFromDepth = "iron key from depth {0}";

    public static int CurDepthQuantity = 0;

    public IronKey()
    {
        NameValue = "iron key";
        ImageValue = ItemSpriteSheet.IronKey;
    }

    public override bool Collect(Bag bag)
    {
        var result = base.Collect(bag);
        if (result && Depth == Dungeon.Depth && Dungeon.Hero != null)
        {
            Dungeon.Hero.Belongings.CountIronKeys();
        }

        return result;
    }

    protected override void OnDetach()
    {
        if (Depth == Dungeon.Depth)
        {
            Dungeon.Hero.Belongings.CountIronKeys();
        }
    }

    public override string ToString() => TextUtils.Format(TxtFromDepth, Depth);

    public override string Info() =>
        "The notches on this ancient iron key are well worn; its leather lanyard " +
        "is battered by age. What door might it open?";
}