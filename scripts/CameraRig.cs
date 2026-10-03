using Godot;

namespace PixelDungeon.Client;

public partial class CameraRig : Node3D
{
    private Camera3D _camera;
    private Node3D _target;
    private float _shakeMagnitude;
    private float _shakeTime;
    private float _shakeDuration;

    public Camera3D Camera => _camera;

    public override void _Ready()
    {
        _camera = new Camera3D
        {
            Name = "Camera3D",
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = 16f,
            Near = 0.05f,
            Far = 200f,
            RotationDegrees = new Vector3(-55f, 45f, 0f),
        };
        AddChild(_camera);
        _camera.Position = _camera.Transform.Basis.Z * 40f; // back along the view axis
        _camera.Current = true;
    }

    public void Follow(Node3D target)
    {
        _target = target;
    }

    public void Snap()
    {
        if (_target != null)
        {
            Position = _target.Position;
        }
    }

    public void Shake(float magnitude, float duration)
    {
        _shakeMagnitude = magnitude;
        _shakeTime = duration;
        _shakeDuration = duration;
    }

    public override void _Process(double delta)
    {
        if (_target == null)
        {
            return;
        }

        var goal = _target.Position;
        if (_shakeTime > 0f)
        {
            _shakeTime -= (float)delta;
            var remaining = Mathf.Max(_shakeTime, 0f) / _shakeDuration;
            goal += new Vector3(GD.Randf() - 0.5f, 0f, GD.Randf() - 0.5f) * (_shakeMagnitude * 0.2f * remaining);
        }

        var t = 1f - Mathf.Exp(-8f * (float)delta);
        Position = Position.Lerp(goal, t);
    }
}