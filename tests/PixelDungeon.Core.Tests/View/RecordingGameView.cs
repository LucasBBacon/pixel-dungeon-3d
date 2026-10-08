using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class RecordingGameView : IGameView
{
    public readonly List<(string Text, LogKind Kind)> Logs = [];
    public readonly List<int> UpdatedCells = [];
    public int FullMapUpdates;
    public readonly List<(int Cell, int OldTerrain)> Discovered = [];
    public int ObserveCount;
    public int ReadyCount;
    public readonly List<(string Id, float Pitch)> Sounds = [];
    public readonly List<(float Magnitude, float Duration)> Shakes = [];
    public readonly List<(EffectKind Kind, int Cell)> Effects = [];
    public readonly List<WindowRequest> Windows = [];
    public readonly List<InterlevelMode> SwitchedModes = [];
    public readonly List<Heap> AddedHeaps = [];
    public readonly List<Heap> DiscardedHeaps = [];
    public readonly List<Mob> AddedMobs = [];

    public int GameOvers;

    // Dungeon.ResultDescription as it stood at the moment GameOver() was called not afterward
    // Every Core death path calls GameOver() before Dungeon.Fail(...) sets this,
    // so it captures the real (and, in the Godot view, deferral-worthy) ordering
    // instead of the end-of-test snapshot
    public string ResultDescriptionAtGameOver;

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

    public void AddHeap(Heap heap)
    {
        AddedHeaps.Add(heap);
        var view = new FakeHeapView();
        heap.Sprite = view;
        view.Link(heap);
    }

    public void DiscardHeap(Heap heap)
    {
        DiscardedHeaps.Add(heap);
        var view = new FakeHeapView();
        heap.Sprite = view;
        view.Link(heap);
    }

    public void AddMob(Mob mob) => AddedMobs.Add(mob);

    public void GameOver()
    {
        GameOvers++;
        ResultDescriptionAtGameOver = Dungeon.ResultDescription;
    }

    public readonly List<Item> PickedUp = [];
    public readonly List<ICellListener> Listeners = [];
    public readonly List<(int From, int To, Item item)> Missiles = [];
    public bool HoldMissiles;
    public int QuickSlotRefreshes;
    private readonly Queue<Action> _heldMissiles = new();

    public void PickUp(Item item) => PickedUp.Add(item);

    public void SelectCell(ICellListener listener) => Listeners.Add(listener);


    public void Missile(int from, int to, Item item, Action onComplete)
    {
        Missiles.Add((from, to, item));
        if (HoldMissiles)
        {
            _heldMissiles.Enqueue(onComplete);
        }
        else
        {
            onComplete();
        }
    }

    public void RefreshQuickSlots() => QuickSlotRefreshes++;

    // what a click on a cell does to the listener the core installed last
    public void Answer(int? cell) => Listeners[^1].OnSelect(cell);

    public void CompleteMissile() => _heldMissiles.Dequeue();

    public bool Logged(string fragment) => Logs.Exists(l => l.Text.Contains(fragment));
}