using System;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Client;

public partial class AttackIndicatorView : Button
{
    private ColorRect _swatch;
    private Func<Mob, Color> _colorOf;

    public static AttackIndicatorView Create(Func<Mob, Color> colorOf)
    {
        var view = new AttackIndicatorView
        {
            CustomMinimumSize = new Vector2(ItemSlot.SlotSize, ItemSlot.SlotSize),
            FocusMode = FocusModeEnum.None,
            Visible = false,
            _colorOf = colorOf,
        };
        view._swatch = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        view._swatch.SetAnchorsPreset(LayoutPreset.FullRect);
        view._swatch.OffsetLeft = 12f;
        view._swatch.OffsetTop = 8f;
        view._swatch.OffsetRight = -12f;
        view._swatch.OffsetBottom = -8f;
        view.AddChild(view._swatch);
        view.Pressed += view.OnPressed;
        return view;
    }

    // AttackIndicator.update, hidden when the hero is dead, disabled while the hero is busy.
    public override void _Process(double delta)
    {
        if (Dungeon.Hero == null)
        {
            return;
        }

        var target = AttackIndicator.LastTarget;
        Visible = Dungeon.Hero.IsAlive() && target != null;
        if (!Visible)
        {
            return;
        }

        _swatch.Color = _colorOf(target);
        Disabled = !Dungeon.Hero.Ready;
        Modulate = new Color(1f, 1f, 1f, Disabled ? 0.3f : 1f);
    }

    private void OnPressed()
    {
        var target = AttackIndicator.LastTarget;
        if (!Disabled && target != null)
        {
            Dungeon.Hero.Handle(target.Pos);
        }
    }
}