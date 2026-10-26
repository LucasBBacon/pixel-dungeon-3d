using Godot;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Client;

public partial class HealthIndicatorView : Control
{
    private const float Width = 32f;
    private const float Height = 4f; // HealthIndicator.HEIGHT 2, at the port's 2x UI scale
    private const float Lift = 1f; // world units above the view's origin; the cube's top is at 0.8

    private static readonly Color Bg = CharView.FromArgb(0xFFCC0000);
    private static readonly Color Level = CharView.FromArgb(0xFF00CC00);

    private CameraRig _rig;
    private ColorRect _level;

    public static HealthIndicatorView Create(CameraRig rig)
    {
        var view = new HealthIndicatorView
        {
            _rig = rig,
            Size = new Vector2(Width, Height),
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        view.AddChild(new ColorRect
        {
            Color = Bg,
            Size = new Vector2(Width, Height),
            MouseFilter = MouseFilterEnum.Ignore
        });
        view._level = new ColorRect
        {
            Color = Level,
            Size = new Vector2(Width, Height),
            MouseFilter = MouseFilterEnum.Ignore
        };
        view.AddChild(view._level);
        return view;
    }

    public override void _Process(double delta)
    {
        var target = HealthIndicator.Target;
        var view = target?.Sprite as CharView;
        var camera = _rig.Camera;

        var shown = target != null
                    && target.IsAlive()
                    && view != null && IsInstanceValid(view)
                    && view.IsInsideTree()
                    && !view.IsQueuedForDeletion()
                    && view.Visible
                    && camera != null;
        if (shown)
        {
            var top = view.GlobalPosition + new Vector3(0f, Lift, 0f);
            shown = !camera.IsPositionBehind(top);
            if (shown)
            {
                var screen = camera.UnprojectPosition(top);
                Position = new Vector2(screen.X - Width / 2f, screen.Y - Height);
                _level.Size = new Vector2(
                    Width * target.HP / target.HT,
                    Height); // level.scale.x = sprite.width * target.HP / target.HT
            }
        }

        Visible = shown;
    }
}