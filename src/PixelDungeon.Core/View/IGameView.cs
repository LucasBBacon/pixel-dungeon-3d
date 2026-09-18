using PixelDungeon.Core.Items;

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
}