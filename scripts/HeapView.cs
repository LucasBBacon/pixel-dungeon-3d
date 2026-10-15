using Godot;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.View;

namespace PixelDungeon.Client;

public partial class HeapView : Node3D, IHeapView
{
    private const float DropInterval = 0.4f;

    private Heap _heap;
    private bool _discarded;
    private MeshInstance3D _body;
    private MeshInstance3D _band;
    private StandardMaterial3D _material;
    private Color _base = Colors.White;
    private Glowing _glowing;
    private float _glowTime;
    private bool _lit = true;
    private Tween _drop;

    public Heap Heap => _heap;

    public static HeapView Create(bool discarded)
    {
        var view = new HeapView { Name = discarded ? "DiscardedHeap" : "Heap", _discarded = discarded };
        view._material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        view._body = new MeshInstance3D { Mesh = new BoxMesh(), MaterialOverride = view._material };
        view.AddChild(view._body);
        view._band = new MeshInstance3D
        {
            Mesh = new BoxMesh(),
            MaterialOverride = TerrainVisuals.Unshaded(new Color(0.96f, 0.75f, 0.2f)),
            Visible = false,
        };
        view.AddChild(view._band);
        return view;
    }

    public void Link(Heap heap)
    {
        _heap = heap;
        View(heap.Image(), heap.Glowing());
        Place(heap.Pos);
    }

    public void Link() => Link(_heap);

    public void Place(int cell)
    {
        KillDrop();
        Position = LevelRenderer.CellToWorld(cell);
    }

    public void Drop()
    {
        if (_heap.IsEmpty())
        {
            return;
        }

        if (_discarded)
        {
            Vanish();
            return;
        }

        // Java also bursts coins and plays SndGold for a visible gold drop
        KillDrop();
        var rest = LevelRenderer.CellToWorld(_heap.Pos);
        Position = rest;
        _drop = CreateTween();
        _drop.TweenProperty(this,
                "position",
                rest + new Vector3(0f, 0.5f, 0f),
                DropInterval / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _drop.TweenProperty(this,
                "position",
                rest,
                DropInterval / 2)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In);
    }

    public void Drop(int from)
    {
        if (_heap.Pos == from || _discarded)
        {
            Drop();
            return;
        }

        if (_heap.IsEmpty())
        {
            return;
        }

        KillDrop();
        var start = LevelRenderer.CellToWorld(from);
        var end = LevelRenderer.CellToWorld(_heap.Pos);
        Position = start;
        _drop = CreateTween();
        _drop.TweenMethod(Callable.From<float>(t =>
        {
            var p = start.Lerp(end, t);
            p.Y = 2f * t * (1f - t); // a parabola peaking at 0.5
            Position = p;
        }), 0f, 1f, DropInterval);
    }

    public void View(int image, Glowing glowing)
    {
        var size = ItemVisuals.SizeOf(image);
        ((BoxMesh)_body.Mesh).Size = size;
        _body.Position = new Vector3(0f, size.Y / 2f, 0f);
        _body.Visible = ItemVisuals.IsDrawn(image);

        _band.Visible = image == ItemSpriteSheet.LockedChest;
        if (_band.Visible)
        {
            ((BoxMesh)_band.Mesh).Size = new Vector3(size.X + 0.02f, 0.08f, size.Z + 0.02f);
            _band.Position = new Vector3(0f, size.Y * 0.6f, 0f);
        }

        _base = ItemVisuals.ColorOf(image);
        _glowing = glowing;
        _glowTime = 0f;
        ApplyColor();
    }

    public void SetLit(bool lit)
    {
        if (_lit == lit) return;

        _lit = lit;
        ApplyColor();
    }

    public void Kill()
    {
        KillDrop();
        QueueFree();
    }

    public override void _Process(double delta)
    {
        if (_discarded && _drop == null && _heap != null)
        {
            Vanish(); // a discarded heap that nobody dropped still has to go
        }

        if (_glowing != null)
        {
            _glowTime += (float)delta;
            ApplyColor();
        }
    }

    private void ApplyColor()
    {
        var c = _base;
        if (_glowing != null)
        {
            // ItemSprite.update oscillates the tint toward the glow color over the glow's period
            var phase = Mathf.Abs(Mathf.Sin(_glowTime * Mathf.Pi / _glowing.Period));
            c = c.Lerp(new Color(_glowing.Red, _glowing.Green, _glowing.Blue, c.A), phase * 0.6f);
        }

        if (!_lit)
        {
            c = new Color(c.R * 0.35f, c.G * 0.35f, c.B * 0.35f, c.A);
        }

        _material.AlbedoColor = c;
        _material.Transparency = c.A < 1f
            ? BaseMaterial3D.TransparencyEnum.Alpha
            : BaseMaterial3D.TransparencyEnum.Disabled;
    }

    private void Vanish()
    {
        KillDrop();
        _drop = CreateTween().SetParallel();
        _drop.TweenProperty(this,
            "scale",
            Vector3.One * 0.1f,
            DropInterval);
        _drop.TweenProperty(this,
            "position",
            Position + new Vector3(0f, -0.6f, 0f),
            DropInterval);
        _drop.Chain().TweenCallback(Callable.From(QueueFree));
    }

    private void KillDrop()
    {
        if (_drop == null) return;

        _drop.Kill();
        _drop = null;
    }
}