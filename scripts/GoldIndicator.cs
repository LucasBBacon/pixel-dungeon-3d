using Godot;
using PixelDungeon.Core;

namespace PixelDungeon.Client;

public partial class GoldIndicator : Label
{
    private const float CTime = 2f;

    private int _lastValue;
    private float _time;

    public static GoldIndicator Create()
    {
        var label = new GoldIndicator
        {
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        label.SetAnchorsPreset(LayoutPreset.Center);
        label.GrowHorizontal = GrowDirection.Both;
        label.OffsetTop = 8f;
        label.AddThemeFontSizeOverride("font_size", 28);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.84f, 0.1f));
        return label;
    }

    public void ResetState()
    {
        _lastValue = 0;
        _time = 0f;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            _time -= (float)delta;
            if (_time > 0)
            {
                Modulate = new Color(1f,
                    1f,
                    1f,
                    _time > CTime / 2
                        ? 1f
                        : _time * 2 / CTime);
            }
            else
            {
                Visible = false;
            }
        }

        if (Dungeon.Gold != _lastValue)
        {
            _lastValue = Dungeon.Gold;
            Text = _lastValue.ToString();
            Visible = true;
            _time = CTime;
        }
    }
}