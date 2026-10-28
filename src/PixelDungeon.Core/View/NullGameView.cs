using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;

namespace PixelDungeon.Core.View;

public sealed class NullGameView : IGameView
{
    public static readonly NullGameView Instance = new();

    public void UpdateMap()
    {
    }

    public void UpdateMap(int cell)
    {
    }

    public void DiscoverTile(int cell, int oldTerrain)
    {
    }

    public void AfterObserve()
    {
    }

    public void Ready()
    {
    }

    public void Log(string text, LogKind kind)
    {
    }

    public void PlaySound(string id, float pitch)
    {
    }

    public void Shake(float magnitude, float duration)
    {
    }

    public void Effect(EffectKind kind, int cell)
    {
    }

    public void ShowWindow(WindowRequest request)
    {
    }

    public void SwitchLevel(InterlevelMode mode)
    {
    }

    public void AddHeap(Heap heap)
    {
    }

    public void DiscardHeap(Heap heap)
    {
    }

    public void AddMob(Mob mob)
    {
    }

    public void GameOver()
    {
    }

    public void PickUp(Item item)
    {
    }

    public void SelectCell(ICellListener listener)
    {
    }

    // no flight to wait for, so the throw resolves at once, tests and headless Core need this
    public void Missile(int from, int to, Item item, Action onComplete) => onComplete();

    public void RefreshQuickSlots()
    {
    }

    public void ShowBag(Bag bag)
    {
    }

    public void SelectItem(Action<Item> listener, BagMode mode, string title)
    {
    }

    public void AddBlob(Blob blob)
    {
    }

    public void Flash(uint color)
    {
    }

    public void Splash(int cell, uint color, int n)
    {
    }
}