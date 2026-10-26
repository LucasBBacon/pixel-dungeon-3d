using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Client;

public partial class BuffIndicatorView : HBoxContainer
{
    public const float Tile = 20f;

    private static readonly Dictionary<int, (string Letter, uint Argb)> Palette = new()
    {
        [BuffIndicator.MindVision] = ("M", 0xFF8855CC),
        [BuffIndicator.Levitation] = ("L", 0xFF99CCFF),
        [BuffIndicator.Fire] = ("F", 0xFFFF6600),
        [BuffIndicator.Poison] = ("P", 0xFF8844FF), // Char.Add's poison splash
        [BuffIndicator.Paralysis] = ("X", 0xFFDDCC33),
        [BuffIndicator.Hunger] = ("H", 0xFFCC8833),
        [BuffIndicator.Starvation] = ("H", 0xFFCC2222),
        [BuffIndicator.Slow] = ("S", 0xFF5577AA),
        [BuffIndicator.Ooze] = ("O", 0xFF445511),
        [BuffIndicator.Amok] = ("A", 0xFFFF4444),
        [BuffIndicator.Terror] = ("T", 0xFF553377),
        [BuffIndicator.Roots] = ("R", 0xFF557722),
        [BuffIndicator.Invisible] = ("I", 0xFFBBBBCC),
        [BuffIndicator.Shadows] = ("S", 0xFF333344),
        [BuffIndicator.Weakness] = ("W", 0xFF887766),
        [BuffIndicator.Frost] = ("F", 0xFF66DDFF),
        [BuffIndicator.Blindness] = ("B", 0xFF222222),
        [BuffIndicator.Combo] = ("C", 0xFFFFAA00),
        [BuffIndicator.Fury] = ("F", 0xFFDD2200),
        [BuffIndicator.Healing] = ("H", 0xFF33CC33),
        [BuffIndicator.Armor] = ("A", 0xFF999999),
        [BuffIndicator.Heart] = ("H", 0xFFFF6699),
        [BuffIndicator.Light] = ("L", 0xFFFFFF99),
        [BuffIndicator.Cripple] = ("C", 0xFFAA6633),
        [BuffIndicator.Barkskin] = ("B", 0xFF886633),
        [BuffIndicator.Immunity] = ("I", 0xFF66CC99),
        [BuffIndicator.Bleeding] = ("B", 0xFFBB0000), // CharView.BloodBurst
        [BuffIndicator.Mark] = ("M", 0xFFFF8800),
        [BuffIndicator.Deferred] = ("D", 0xFF777799),
        [BuffIndicator.Vertigo] = ("V", 0xFF9966CC),
        [BuffIndicator.Rage] = ("R", 0xFFCC0033),
        [BuffIndicator.Sacrifice] = ("S", 0xFFAA0066),
    };

    private Func<Char> _owner;
    private readonly List<(int Icon, string Name)> _shown = [];
    private readonly List<(int Icon, string Name)> _current = [];

    // new BuffIndicator(ch): the status pane passes the hero.
    public static BuffIndicatorView Create(Func<Char> owner)
    {
        var view = new BuffIndicatorView
        {
            _owner = owner,
            MouseFilter = MouseFilterEnum.Pass,
            Visible = false,
        };
        view.AddThemeConstantOverride("separation", 2);
        return view;
    }

    public static Color ColorFor(int icon) => Palette
        .TryGetValue(icon, out var entry)
        ? CharView.FromArgb(entry.Argb)
        : new Color(0.8f, 0.3f, 0.8f);

    public static string LetterFor(int icon) => Palette
        .TryGetValue(icon, out var entry)
        ? entry.Letter
        : "?";

    public static Control CreateTile(int icon, float size)
    {
        var tile = new ColorRect
        {
            Color = ColorFor(icon),
            CustomMinimumSize = new Vector2(size, size),
            MouseFilter = MouseFilterEnum.Pass, // hovering shows the tooltip; a click passes on to the status panel
        };
        var letter = new Label
        {
            Text = LetterFor(icon),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        letter.SetAnchorsPreset(LayoutPreset.FullRect);
        letter.AddThemeFontSizeOverride("font_size", (int)(size * 0.6f));
        letter.AddThemeColorOverride("font_color", Colors.White);
        letter.AddThemeColorOverride("font_outline_color", Colors.Black);
        letter.AddThemeConstantOverride("outline_size", 4);
        tile.AddChild(letter);
        return tile;
    }

    // BuffIndicator.layout(), run when the owner's buffs, their icons or their names change.
    public override void _Process(double delta)
    {
        _current.Clear();
        var ch = _owner();
        if (ch != null)
        {
            foreach (var buff in ch.Buffs())
            {
                var icon = buff.Icon();
                if (icon != BuffIndicator.None)
                {
                    _current.Add((icon, buff.ToString()));
                }
            }
        }

        if (!Changed())
        {
            return;
        }

        _shown.Clear();
        _shown.AddRange(_current);

        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        foreach (var (icon, name) in _shown)
        {
            var tile = CreateTile(icon, Tile);
            tile.TooltipText = name;
            AddChild(tile);
        }

        Visible = _shown.Count > 0;
    }

    private bool Changed()
    {
        return _current.Count != _shown.Count
               || _current
                   .Where((t, i) => t != _shown[i])
                   .Any();
    }
}