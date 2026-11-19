using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfEnchantment : InventoryScroll
{
    private const string TxtGlows = "your {0} glows in the dark";

    public ScrollOfEnchantment()
    {
        NameValue = "Scroll of Enchantment";
        InventoryTitle = "Select an enchantable item";
        Mode = BagMode.Enchantable;
    }

    protected override void OnItemSelected(Item item)
    {
        ScrollOfRemoveCurse.Uncurse(Dungeon.Hero, item);

        if (item is Weapon.Weapon weapon)
        {
            weapon.Enchant();
        }
        else
        {
            ((Armor.Armor)item).Inscribe();
        }
        
        item.Fix();
        
        CurUser.Sprite.Burst(0xFFFFFFFF, 5);
        GLog.W(TxtGlows, item.Name());
    }

    public override string Desc() =>
        "This scroll is able to imbue a weapon or an armor " +
        "with a random enchantment, granting it a special power.";
}