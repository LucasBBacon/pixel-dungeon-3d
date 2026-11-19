using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfUpgrade : InventoryScroll
{
    private const string TxtLooksBetter = "your {0} certainly looks better now";

    public ScrollOfUpgrade()
    {
        NameValue = "Scroll of Upgrade";
        InventoryTitle = "Select an item to upgrade";
        Mode = BagMode.Upgradable;
    }

    protected override void OnItemSelected(Item item)
    {
        ScrollOfRemoveCurse.Uncurse(Dungeon.Hero, item);
        if (item.IsBroken())
        {
            item.Fix();
        }
        else
        {
            item.Upgrade();
        }

        Upgrade(CurUser);
        GLog.P(TxtLooksBetter, item.Name());
        
        // TODO: Badges.validateItemLevelAcquired
    }

    public static void Upgrade(Hero hero)
    {
        hero.Sprite.Burst(0xFFFFDD44, 3);
    }

    public override string Desc() =>
        "This scroll will upgrade a single item, improving its quality. A wand will " +
        "increase in power and in number of charges; a weapon will inflict more damage " +
        "or find its mark more frequently; a suit of armor will deflect additional blows; " +
        "the effect of a ring on its wearer will intensify. Weapons and armor will also " +
        "require less strength to use, and any curses on the item will be lifted.";
}