using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfIdentify : InventoryScroll
{
    public ScrollOfIdentify()
    {
        NameValue = "Scroll of Identify";
        InventoryTitle = "Select an item to identify";
        Mode = BagMode.Unidentified;
    }

    protected override void OnItemSelected(Item item)
    {
        CurUser.Sprite.Burst(0xFF4488CC, 5);

        item.Identify();
        GLog.I("It is " + item);
        
        // TODO: Badges.validate
    }

    public override string Desc() => 
        "Permanently reveals all of the secrets of a single item.";

    public override int Price() => IsKnown() ? 30 * QuantityValue : base.Price();
}