using System.Collections.Generic;
using Godot;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.View;

namespace PixelDungeon.Client;

public partial class CharView : Node3D, ICharView
{
    private const float MoveInterval = 0.1f; // CharSprite.MOVE_INTERVAL

    private static readonly int[] ArmorImages =
    [
        ItemSpriteSheet.ArmorCloth,
        ItemSpriteSheet.ArmorLeather,
        ItemSpriteSheet.ArmorMail,
        ItemSpriteSheet.ArmorScale,
        ItemSpriteSheet.ArmorPlate
    ];

    private const float ArmorBlend = 0.6f;

    private const float StatusStackOffset = 0.25f;

    private Char _ch;
    private Color _color;
    private StandardMaterial3D _material;
    private MeshInstance3D _body;
    private readonly List<Label3D> _statusFloaters = [];
    private Label3D _emo;
    private Tween _motion;
    private bool _sleeping;

    public static CharView For(Char ch, Color color)
    {
        var view = new CharView { Name = ch.Name, _ch = ch };
        view.BuildNodes(color);
        return view;
    }

    private void BuildNodes(Color color)
    {
        _color = color;
        _material = TerrainVisuals.Unshaded(color);

        _body = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.6f, 0.8f, 0.6f) },
            MaterialOverride = _material,
            Position = new Vector3(0f, 0.4f, 0f),
        };
        AddChild(_body);

        _emo = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            PixelSize = 0.01f,
            FontSize = 48,
            Position = new Vector3(0f, 1.4f, 0f),
            Visible = false,
        };
        AddChild(_emo);
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
        IsMoving = true;
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
        IsMoving = false; // clears the gate before the callback ends turn
        _ch.OnMotionComplete();
    }

    public void InterruptMotion()
    {
        KillMotion();
        Position = LevelRenderer.CellToWorld(_ch.Pos);
    }

    // shared by move and attack
    // both park their tween in _motion, so killing it here cancels whichever is in flight
    // Tween.Kill() does not raise Finished in GD4, so an interrupted move never calls
    // OnMotionComplete and an interrupted Attack never calls OnAttackComplete
    // the caller is trusted to only do that when the scheduler gate is being torn down
    // some other way
    private void KillMotion()
    {
        if (_motion != null)
        {
            _motion.Kill();
            _motion = null;
        }

        IsMoving = false;
    }

    public void Operate(int cell)
    {
        var tween = CreateTween();
        tween.TweenProperty(_body, "scale", new Vector3(1.2f, 0.8f, 1.2f), 0.1);
        tween.TweenProperty(_body, "scale", Vector3.One, 0.1);
        tween.Finished += () => _ch.OnOperateComplete();
    }

    public void Attack(int cell)
    {
        TurnTo(_ch.Pos, cell);

        var dx = cell % Level.Width - _ch.Pos % Level.Width;
        var dz = cell / Level.Width - _ch.Pos / Level.Width;
        var home = Position;
        var lunge = home + new Vector3(dx, 0f, dz) / 3f;

        KillMotion();
        _motion = CreateTween();
        _motion.TweenProperty(this, "position", lunge, MoveInterval);
        _motion.TweenProperty(this, "position", home, MoveInterval);
        _motion.Finished += OnAttackFinished;
    }

    private void OnAttackFinished()
    {
        _motion = null;
        _ch.OnAttackComplete();
    }

    // CharSprite.zap
    // turn toward the cell and play zap animation
    // base CharSprite has no completion callback
    public void Zap(int cell)
    {
        TurnTo(_ch.Pos, cell);
        var tween = CreateTween();
        tween.TweenProperty(_body, "scale", new Vector3(0.8f, 1.2f, 0.8f), 0.08);
        tween.TweenProperty(_body, "scale", Vector3.One, 0.08);
    }

    public void ShowAlert()
    {
        _emo.Text = "!";
        _emo.Visible = true;
    }

    public void HideAlert()
    {
        if (_emo.Text == "!")
        {
            _emo.Visible = false;
        }
    }

    // Java derives the sleep icon from mob.state every frame
    // GameScene._Process calls this, alert and sleep share one slot, as they share
    // CharSprite's single `emo` field
    public void SetSleeping(bool sleeping)
    {
        if (_sleeping == sleeping)
        {
            return;
        }

        _sleeping = sleeping;

        if (sleeping)
        {
            _emo.Text = "z";
            _emo.Visible = true;
        }
        else if (_emo.Text == "z")
        {
            _emo.Visible = false;
        }
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
        var offset = StatusStackOffset + _statusFloaters.Count;
        var status = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            PixelSize = 0.01f,
            FontSize = 48,
            Text = text,
            Modulate = ColorFor(color),
            OutlineModulate = Colors.Black,
            Position = new Vector3(0f, 1.2f + offset, 0f),
        };
        AddChild(status);
        _statusFloaters.Add(status);

        var tween = CreateTween().SetParallel();
        tween.TweenProperty(status, "position", new Vector3(0f, 1.7f + offset, 0f), 1.0);
        tween.TweenProperty(status, "modulate:a", 0f, 1.0);
        tween.TweenProperty(status, "outline_modulate:a", 0f, 1.0);
        tween.Finished += () =>
        {
            _statusFloaters.Remove(status);
            status.QueueFree();
        };
    }

    public void Burst(uint color, int n)
    {
        for (var i = 0; i < n; i++)
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

    public void UpdateArmor(int tier)
    {
        // Deviation, Java swaps to tier's strip of hero sprite sheet
        // cube takes a tint instead, tier 0 its own color
        if (tier <= 0)
        {
            _material.AlbedoColor = _color;
        }

        // TODO: HeroSprite shows worn class armour, tier 6, as own sprite strip. until class armours exist a tier above 5 draws as plate
        var image = ArmorImages[Mathf.Min(tier, ArmorImages.Length) - 1];
        _material.AlbedoColor = _color.Lerp(ItemVisuals.ColorOf(image), ArmorBlend);
    }

    public bool IsMoving { get; private set; }
}