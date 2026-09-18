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
}