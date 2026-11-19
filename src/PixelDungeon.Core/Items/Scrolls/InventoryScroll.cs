using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Scrolls;

public abstract class InventoryScroll : Scroll
{
    protected internal string InventoryTitle = "Select an item";
    protected internal BagMode Mode = BagMode.All;

    private const string TxtWarning = "Do you really want to cancel this scroll usage? It will be consumed anyway.";
    private const string TxtYes = "Yes, I'm positive";
    private const string TxtNo = "No, I changed my mind";
    
    protected override void DoRead()
    {
        if (!IsKnown())
        {
            SetKnown();
            IdentifiedByUse = true;
        }
        else
        {
            IdentifiedByUse = false;
        }
        
        GameScene.SelectItem(ItemSelector, Mode, InventoryTitle);
    }

    private void ConfirmCancellation()
    {
        GameScene.Show(new WindowRequest(Name(), TxtWarning, [TxtYes, TxtNo], index =>
        {
            switch (index)
            {
                case 0:
                    CurUser.SpendAndNext(TimeToRead);
                    IdentifiedByUse = false;
                    break;
                case 1:
                    GameScene.SelectItem(ItemSelector, Mode, InventoryTitle);
                    break;
            }
        }, ignoresBack: true));
    }

    protected abstract void OnItemSelected(Item item);
    
    protected internal static bool IdentifiedByUse = false;

    protected internal static readonly Action<Item> ItemSelector = item =>
    {
        if (item != null)
        {
            ((InventoryScroll)CurItem).OnItemSelected(item);
            ((InventoryScroll)CurItem).ReadAnimation();

            Sample.Play(Assets.SndRead);
        }
        else if (IdentifiedByUse)
        {
            ((InventoryScroll)CurItem).ConfirmCancellation();
        }
        else
        {
            CurItem.Collect(CurUser.Belongings.Backpack);
        }
    };
}