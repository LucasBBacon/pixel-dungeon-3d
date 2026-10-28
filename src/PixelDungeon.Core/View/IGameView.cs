using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;

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
    void ShowBag(Bag bag);
    void SelectItem(Action<Item> listener, BagMode mode, string title);
    void AddBlob(Blob blob);
    void Flash(uint color);
    void Splash(int cell, uint color, int n);
}