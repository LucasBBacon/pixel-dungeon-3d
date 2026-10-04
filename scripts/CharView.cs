using Godot;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.View;

namespace PixelDungeon.Client;

public partial class CharView : Node3D, ICharView
{
    private const float MoveInterval = 0.1f; // CharSprite.MOVE_INTERVAL

    private Char _ch;
    private MeshInstance3D _body;
    private Label3D _status;
    private Tween _motion;
    private Tween _statusTween;
    private bool _moving;

    public static CharView For(Char ch, Color color)
    {
        var view = new CharView { Name = ch.Name, _ch = ch };
        view.BuildNodes(color);
        return view;
    }

    private void BuildNodes(Color color)
    {
        _body = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.6f, 0.8f, 0.6f) },
            MaterialOverride = TerrainVisuals.Unshaded(color),
            Position = new Vector3(0f, 0.4f, 0f),
        };
        AddChild(_body);

        _status = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            PixelSize = 0.01f,
            FontSize = 48,
            Position = new Vector3(0f, 1.2f, 0f),
            Visible = false,
        };
        AddChild(_status);
    }

    public static Color FromArgb(uint argb) => new(
        ((argb >> 16) & 0xFF) / 255f,
        ((argb >> 8) & 0xFF) / 255f,
        (argb & 0xFF) / 255f
    );

    private static Color ColorFor(StatusColor color)
    {
        return color switch
        {
            StatusColor.Positive => new Color(0f, 1f, 0f),
            StatusColor.Negative => new Color(1f, 0f, 0f),
            StatusColor.Warning => new Color(1f, 0.53f, 0f),
            StatusColor.Neutral => new Color(1f, 1f, 0f),
            _ => Colors.White
        };
    }

    // ---- ICharView ----

    public void Place(int cell)
    {
        KillMotion();
        Position = LevelRenderer.CellToWorld(cell);
    }

    public void Move(int from, int to)
    {
        KillMotion();
        TurnTo(from, to);
        Position = LevelRenderer.CellToWorld(from);
        _moving = true;
        _motion = CreateTween();
        _motion.TweenProperty(this,
            "position",
            LevelRenderer.CellToWorld(to),
            MoveInterval);
        _motion.Finished += OnMotionFinished;
    }

    private void OnMotionFinished()
    {
        _motion = null;
        _moving = false; // clears the gate before the callback ends turn
        _ch.OnMotionComplete();
    }

    public void InterruptMotion()
    {
        KillMotion();
        Position = LevelRenderer.CellToWorld(_ch.Pos);
    }

    private void KillMotion()
    {
        if (_motion != null)
        {
            _motion.Kill();
            _motion = null;
        }

        _moving = false;
    }

    public void Operate(int cell)
    {
        var tween = CreateTween();
        tween.TweenProperty(_body, "scale", new Vector3(1.2f, 0.8f, 1.2f), 0.1);
        tween.TweenProperty(_body, "scale", Vector3.One, 0.1);
        tween.Finished += () => _ch.OnOperateComplete();
    }

    public void TurnTo(int from, int to)
    {
        var dx = to % Level.Width - from % Level.Width;
        var dz = to / Level.Width - from / Level.Width;
        if (dx != 0 || dz != 0)
        {
            _body.Rotation = new Vector3(0f, Mathf.Atan2(dx, dz), 0f);
        }
    }

    public void Idle()
    {
    }

    public void Die()
    {
        KillMotion();
        Visible = false;
    }

    public void ShowStatus(StatusColor color, string text)
    {
        _status.Text = text;
        _status.Modulate = ColorFor(color);
        _status.Position = new Vector3(0f, 1.2f, 0f);
        _status.Visible = true;
        _statusTween?.Kill();
        _statusTween = CreateTween().SetParallel();
        _statusTween.TweenProperty(_status, "position", new Vector3(0f, 1.7f, 0f), 1.0);
        _statusTween.TweenProperty(_status, "modulate:a", 0f, 1f);
    }

    public void Burst(uint color, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var bit = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = Vector3.One * 0.1f },
                MaterialOverride = TerrainVisuals.Unshaded(FromArgb(color)),
                Position = new Vector3(0f, 0.5f, 0f),
            };
            AddChild(bit);
            var target = new Vector3(GD.Randf() - 0.5f, GD.Randf() * 0.8f, GD.Randf() - 0.5f);
            var tween = CreateTween();
            tween.TweenProperty(bit, "position", target, 0.5);
            tween.Finished += () => bit.QueueFree();
        }
    }

    public void BloodBurst(int damage)
    {
        Burst(0xFFBB0000, Mathf.Min(damage, 10));
    }

    public void Flash()
    {
        var previous = _body.MaterialOverride;
        _body.MaterialOverride = TerrainVisuals.Flash;
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (IsInstanceValid(_body))
            {
                _body.MaterialOverride = previous;
            }
        };
    }

    public bool IsMoving => _moving;
}