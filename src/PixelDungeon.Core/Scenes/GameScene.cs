using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Scenes;

public static class GameScene
{
    public static IGameView Instance = NullGameView.Instance;

    public static void UpdateMap() => Instance.UpdateMap();
    public static void UpdateMap(int cell) => Instance.UpdateMap(cell);
    public static void DiscoverTile(int pos, int oldValue) => Instance.DiscoverTile(pos, oldValue);
    public static void AfterObserve() => Instance.AfterObserve();
    public static void Ready() => Instance.Ready();
    public static void Effect(EffectKind kind, int cell) => Instance.Effect(kind, cell);
    public static void Shake(float magnitude, float duration) => Instance.Shake(magnitude, duration);
    public static void Show(WindowRequest request) => Instance.ShowWindow(request);
    public static void SwitchLevel(InterlevelMode mode) => Instance.SwitchLevel(mode);
    public static void Add(Heap heap) => Instance.AddHeap(heap);
    public static void Discard(Heap heap) => Instance.DiscardHeap(heap);

    // GameScene.java add(Mob) and add(Mob, float) bundle level bookkeeping
    // scheduling and cell occupancy with sprite creation
    // only the last is view work, so the other 3 stay here and NullGameView can't swallow a mob
    public static void Add(Mob mob)
    {
        Dungeon.Level.Mobs.Add(mob);
        Actor.Add(mob);
        Actor.OccupyCell(mob);
        Instance.AddMob(mob);
    }

    public static void Add(Mob mob, float delay)
    {
        Dungeon.Level.Mobs.Add(mob);
        Actor.AddDelayed(mob, delay);
        Actor.OccupyCell(mob);
        Instance.AddMob(mob);
    }

    public static void GameOver() => Instance.GameOver();

    public static void PickUp(Item item) => Instance.PickUp(item);

    public static void SelectCell(ICellListener listener) => Instance.SelectCell((listener));

    // Item.cast in the java recycles a MissileSprite and resets it with this callback
    public static void Missile(int from, int to, Item item, Action onComplete) =>
        Instance.Missile(from, to, item, onComplete);
    
    public static void RefreshQuickSlots() => Instance.RefreshQuickSlots();
}