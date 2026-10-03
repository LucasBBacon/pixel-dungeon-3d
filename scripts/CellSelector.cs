using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Client;

public partial class CellSelector : Node
{
    public bool Enabled = true;

    private CameraRig _rig;

    private static readonly (string Action, Key[] Keys, bool Shift)[] Bindings =
    [
        ("move_up", [Key.W, Key.Up], false),
        ("move_down", [Key.S, Key.Down], false),
        ("move_left", [Key.A, Key.Left], false),
        ("move_right", [Key.D, Key.Right], false),
        ("move_up_left", [Key.Q], false),
        ("move_up_right", [Key.E], false),
        ("move_down_left", [Key.Z], false),
        ("move_down_right", [Key.C], false),
        ("search", [Key.Space], false),
        ("rest", [Key.Period], false),
        ("rest_full", [Key.Period], true),
        ("debug_console", [Key.Quoteleft], false)
    ];

    public static void RegisterActions()
    {
        foreach (var (action, keys, shift) in Bindings)
        {
            if (InputMap.HasAction(action))
            {
                continue;
            }

            InputMap.AddAction(action);
            foreach (var key in keys)
            {
                InputMap.ActionAddEvent(
                    action,
                    new InputEventKey { PhysicalKeycode = key, ShiftPressed = shift }
                );
            }
        }
    }

    public void Init(CameraRig rig)
    {
        _rig = rig;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Enabled || Dungeon.Hero == null)
        {
            return;
        }

        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
        {
            var cell = CellUnder(mouse.Position);
            if (cell != -1)
            {
                Select(cell);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private int CellUnder(Vector2 screen)
    {
        var camera = _rig.Camera;
        var origin = camera.ProjectRayOrigin(screen);
        var direction = camera.ProjectRayNormal(screen);
        var hit = new Plane(Vector3.Up, 0f).IntersectsRay(origin, direction);
        return hit.HasValue ? LevelRenderer.WorldToCell(hit.Value) : -1;
    }

    public override void _Process(double delta)
    {
        if (!Enabled || Dungeon.Hero == null || !Dungeon.Hero.Ready)
        {
            return;
        }

        if (Input.IsActionJustPressed("search"))
        {
            Dungeon.Hero.Search(true);
            return;
        }

        if (Input.IsActionJustPressed("rest_full"))
        {
            Dungeon.Hero.Rest(true);
            return;
        }

        if (Input.IsActionJustPressed("rest"))
        {
            Dungeon.Hero.Rest(false);
            return;
        }

        var dx = 0;
        var dy = 0;
        if (Input.IsActionJustPressed("move_up")) dy -= 1;
        if (Input.IsActionJustPressed("move_down")) dy += 1;
        if (Input.IsActionJustPressed("move_left")) dx -= 1;
        if (Input.IsActionJustPressed("move_right")) dx += 1;
        if (Input.IsActionJustPressed("move_up_left"))
        {
            dx -= 1;
            dy -= 1;
        }

        if (Input.IsActionJustPressed("move_up_right"))
        {
            dx += 1;
            dy -= 1;
        }

        if (Input.IsActionJustPressed("move_down_left"))
        {
            dx -= 1;
            dy += 1;
        }

        if (Input.IsActionJustPressed("move_down_right"))
        {
            dx += 1;
            dy += 1;
        }

        dx = Mathf.Clamp(dx, -1, 1);
        dy = Mathf.Clamp(dy, -1, 1);
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var x = Dungeon.Hero.Pos % Level.Width + dx;
        var y = Dungeon.Hero.Pos / Level.Width + dy;
        if (x < 0 || x >= Level.Width || y < 0 || y >= Level.Height)
        {
            return;
        }

        Select(x + y * Level.Width);
    }

    private static void Select(int cell)
    {
        if (Dungeon.Hero.Ready)
        {
            Dungeon.Hero.Handle(cell);
        }
    }
}