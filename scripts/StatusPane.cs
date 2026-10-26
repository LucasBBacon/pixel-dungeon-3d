using System;
using System.Collections.Generic;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.View;

namespace PixelDungeon.Client;

public partial class StatusPane : CanvasLayer
{
    private Label _depth;
    private Label _hp;
    private Label _exp;
    private Label _keys;

    private readonly Label[] _logLines = new Label[4];
    private readonly Queue<(string Text, LogKind Kind)> _log = new();
    private Button _prompt;

    public event Action PromptPressed;

    public ColorRect Fade { get; private set; }

    public override void _Ready()
    {
        Layer = 5;

        Fade = new ColorRect
        {
            Name = "Fade",
            Color = new Color(0f, 0f, 0f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        Fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(Fade);

        var panel = new PanelContainer { Position = new Vector2(8f, 8f) };
        AddChild(panel);
        var box = new VBoxContainer();
        panel.AddChild(box);

        _depth = new Label();
        box.AddChild(_depth);
        _hp = new Label();
        box.AddChild(_hp);
        _exp = new Label();
        box.AddChild(_exp);
        _keys = new Label();
        box.AddChild(_keys);
        
        // StatusPane.java buffs = new BuffIndicator(Dungeon.hero)
        box.AddChild(BuffIndicatorView.Create(() => Dungeon.Hero));
        for (var i = 0; i < _logLines.Length; i++)
        {
            _logLines[i] = new Label();
            box.AddChild(_logLines[i]);
        }

        // toast in the java, centered above toolbar, tapping it cancels selection
        _prompt = new Button { Visible = false, FocusMode = Control.FocusModeEnum.None };
        _prompt.Pressed += () => PromptPressed?.Invoke();
        AddChild(_prompt);
    }

    public override void _Process(double delta)
    {
        if (Dungeon.Hero == null)
        {
            return;
        }

        _depth.Text = $"Depth {Dungeon.Depth}";
        _hp.Text = $"HP {Dungeon.Hero.HP}/{Dungeon.Hero.HT}";
        _exp.Text = $"Lvl {Dungeon.Hero.Lvl}   XP {Dungeon.Hero.Exp}/{Dungeon.Hero.MaxExp()}";

        // StatusPane.keys in java
        _keys.Visible = IronKey.CurDepthQuantity > 0;
        _keys.Text = $"Iron keys {IronKey.CurDepthQuantity}";
    }

    private static Color ColorFor(LogKind kind) =>
        kind switch
        {
            LogKind.Positive => new Color(0f, 1f, 0f),
            LogKind.Negative => new Color(1f, 0f, 0f),
            LogKind.Warning => new Color(0f, 0.53f, 0f),
            LogKind.Highlight => new Color(1f, 1f, 0f),
            _ => Colors.White
        };

    public void Log(string text, LogKind kind)
    {
        _log.Enqueue((text, kind));
        while (_log.Count > _logLines.Length)
        {
            _log.Dequeue();
        }

        var i = 0;
        foreach (var (t, k) in _log)
        {
            _logLines[i].Text = t;
            _logLines[i].Modulate = ColorFor(k);
            i++;
        }

        for (; i < _logLines.Length; i++)
        {
            _logLines[i].Text = "";
        }
    }

    public void ShowPrompt(string text)
    {
        _prompt.Visible = text != null;
        if (text == null)
        {
            return;
        }

        _prompt.Text = text;
        _prompt.ResetSize();
        var screen = _prompt.GetViewportRect().Size;
        _prompt.Position = new Vector2((screen.X - _prompt.Size.X) / 2f, screen.Y - 10f);
    }

    public void ShowWindow(WindowRequest request, Action onClosed)
    {
        AcceptDialog dialog;
        if (request.IsChoice)
        {
            var confirm = new ConfirmationDialog
            {
                OkButtonText = request.Options[0],
                CancelButtonText = request.Options[1]
            };
            confirm.Canceled += () => request.Select(1);
            dialog = confirm;
        }
        else
        {
            dialog = new AcceptDialog { OkButtonText = request.Options[0] };
        }

        dialog.Title = request.Title ?? "";
        dialog.DialogText = request.Body;
        dialog.Exclusive = true;
        dialog.Confirmed += () => request.Select(0);
        dialog.VisibilityChanged += () =>
        {
            if (!dialog.Visible)
            {
                onClosed();
                dialog.QueueFree();
            }
        };

        AddChild(dialog);
        dialog.PopupCentered();
    }
}