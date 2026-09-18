using PixelDungeon.Core.Items;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class RecordingGameView : IGameView
{
    public readonly List<(string Text, LogKind Kind)> Logs = new();
    public readonly List<int> UpdatedCells = new();
    public int FullMapUpdates;
    public readonly List<(int Cell, int OldTerrain)> Discovered = new();
    public int ObserveCount;
    public int ReadyCount;
    public readonly List<(string Id, float Pitch)> Sounds = new();
    public readonly List<(float Magnitude, float Duration)> Shakes = new();
    public readonly List<(EffectKind Kind, int Cell)> Effects = new();
    public readonly List<WindowRequest> Windows = new();
    public readonly List<InterlevelMode> SwitchedModes = new();
    public readonly List<Heap> AddedHeaps = new();
    public readonly List<Heap> DiscardedHeaps = new();

    public void UpdateMap() => FullMapUpdates++;

    public void UpdateMap(int cell) => UpdatedCells.Add(cell);

    public void DiscoverTile(int cell, int oldTerrain) => Discovered.Add((cell, oldTerrain));

    public void AfterObserve() => ObserveCount++;

    public void Ready() => ReadyCount++;

    public void Log(string text, LogKind kind) => Logs.Add((text, kind));

    public void PlaySound(string id, float pitch) => Sounds.Add((id, pitch));

    public void Shake(float magnitude, float duration) => Shakes.Add((magnitude, duration));

    public void Effect(EffectKind kind, int cell) => Effects.Add((kind, cell));

    public void ShowWindow(WindowRequest request) => Windows.Add(request);

    public void SwitchLevel(InterlevelMode mode) => SwitchedModes.Add(mode);

    public void AddHeap(Heap heap) => AddedHeaps.Add(heap);

    public void DiscardHeap(Heap heap) => DiscardedHeaps.Add(heap);

    public bool Logged(string fragment) => Logs.Exists(l => l.Text.Contains(fragment));
}