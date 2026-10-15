using System;
using Godot;
using PixelDungeon.Core.Items;

namespace PixelDungeon.Client;

public partial class MissileView : Node3D
{
    private const float CellsPerSecond = 15f;

    private Action _onComplete;

    public static MissileView Launch(Node parent, int from, int to, Item item, Action onComplete)
    {
        var image = item?.Image() ?? 0;
        var view = new MissileView { Name = "Missile", _onComplete = onComplete };
        var body = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = ItemVisuals.SizeOf(image) },
            MaterialOverride = TerrainVisuals.Unshaded(ItemVisuals.ColorOf(image))
        };
        view.AddChild(body);
        parent.AddChild(view);

        var start = LevelRenderer.CellToWorld(from) + new Vector3(0f, 0.5f, 0f);
        var end = LevelRenderer.CellToWorld(to) + new Vector3(0f, 0.5f, 0f);
        view.Position = start;
        var duration = Mathf.Max(start.DistanceTo(end) / CellsPerSecond, 0.01f);

        var flight = view.CreateTween().SetParallel();
        flight.TweenProperty(view, "position", end, duration);
        flight.TweenProperty(body, "rotation:y", Mathf.Tau * 2f * duration, duration);
        flight.Chain().TweenCallback(Callable.From(view.Land));
        return view;
    }

    private void Land()
    {
        var complete = _onComplete;
        _onComplete = null;
        QueueFree();
        complete?.Invoke();
    }
}