using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Plants;

namespace PixelDungeon.Core.View;

public interface IGameView
{
    void UpdateMap();
    void UpdateMap(int cell);
    void DiscoverTile(int cell, int oldTerrain);
    void AfterObserve();
    void Ready();
    void Log(string text, LogKind kind);
    void PlaySound(string id, float pitch);
    void Shake(float magnitude, float duration);
    void Effect(EffectKind kind, int cell);
    void ShowWindow(WindowRequest request);
    void SwitchLevel(InterlevelMode mode);
    void AddHeap(Heap heap);
    void DiscardHeap(Heap heap);
    void AddMob(Mob mob);
    void GameOver();
    void PickUp(Item item); // Toolbar.pickup, fly the item into the backpack button
    void SelectCell(ICellListener listener); // next cell selection goes to the listener
    void Missile(int from, int to, Item item, Action onComplete); // MissileSprite.reset, call onComplete exactly once
    void RefreshQuickSlots(); // QuickSlot.Refresh()
    void ShowBag(Bag bag); // GameScene.show(new WndBag(bag, null, ALL, null))
    void SelectItem(Action<Item> listener, BagMode mode, string title); // GameScene.selectItem: choose from the last bag, null on cancel
    void AddBlob(Blob blob); // GameScene.addBlobSprite: draw the gas
    void Flash(uint color); // GameScene.flash: full-screen fade in the color
    void Splash(int cell, uint color, int n); // Splash.at: n colored particles burst from the cell
    void AddPlant(Plant plant); // GameScene.addPlantSprite, create the plant's view, set plant.Sprite, call Reset
    void Lightning(int[] cells, int length, Action onComplete); // Lightning: arcs between consecutive cells for 0.3s, onComplete once if non-null
}