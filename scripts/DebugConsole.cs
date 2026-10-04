using System;
using System.Collections.Generic;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Client;

public partial class DebugConsole : CanvasLayer
{
    private PanelContainer _panel;
    private RichTextLabel _output;
    private LineEdit _input;
    private readonly Dictionary<string, (string help, Action<string[]> Run)> _commands = new();

    public bool IsOpen => _panel.Visible;

    public event Action<bool> OpenChanged;

    // Set by GameScene, rebuilds view after jump
    public Action AfterLevelChange;

    public override void _Ready()
    {
        Layer = 10;

        _panel = new PanelContainer { Visible = false, CustomMinimumSize = new Vector2(0f, 220f) };
        _panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        AddChild(_panel);

        var box = new VBoxContainer();
        _panel.AddChild(box);

        _output = new RichTextLabel
        {
            ScrollFollowing = true,
            CustomMinimumSize = new Vector2(0f, 180f),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        box.AddChild(_output);

        _input = new LineEdit { PlaceholderText = "help" };
        _input.TextSubmitted += OnSubmitted;
        box.AddChild(_input);

        Register("help", "list commands", _ =>
        {
            foreach (var (name, entry) in _commands)
            {
                Print($"{name}: {entry.help}");
            }
        });

        Register("depth", "depth N: travel to depth N, generating levels on the way", args =>
        {
            if (args.Length != 1 || !int.TryParse(args[0], out var target) || target < 1)
            {
                Print("usage: depth N");
                return;
            }

            Dungeon.Hero.Interrupt();
            var guard = 0;
            while (Dungeon.Depth != target && guard++ < 64)
            {
                Interlevel.Mode = Dungeon.Depth < target
                    ? InterlevelMode.Descend
                    : InterlevelMode.Ascend;
                Interlevel.Run();
            }

            AfterLevelChange?.Invoke();
            Print($"now at depth {Dungeon.Depth}");
        });

        Register("reveal", "map every discoverable cell", _ =>
        {
            for (var i = 0; i < Level.Length; i++)
            {
                Dungeon.Level.Mapped[i] = Level.Discoverable[i];
            }

            Dungeon.Observe();
            Print("map revealed");
        });

        Register("seed", "seed N: seed the RNG; affects the next generated level", args =>
        {
            if (args.Length != 1 || !int.TryParse(args[0], out var seed))
            {
                Print("usage: seed N");
                return;
            }

            Random.Seed(seed);
            Print($"seeded {seed}");
        });

        Register("where", "hero cell, terrain, feeling, and room type", _ =>
        {
            var pos = Dungeon.Hero.Pos;
            var level = Dungeon.Level;
            var room = level is RegularLevel regular ? (regular.RoomAt(pos)?.Type.ToString() ?? "none") : "n/a";
            Print(
                $"cell {pos} ({pos % Level.Width}, {pos / Level.Width}) {level.TileName(level.Map[pos])}; feeling {level.Feeling}; room {room}");
        });
    }

    public void Register(string name, string help, Action<string[]> run) => _commands[name] = (help, run);


    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("debug_console"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && @event.IsActionPressed("ui_cancel"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        if (_panel.Visible)
        {
            _input.Text = "";
            _input.GrabFocus();
        }

        OpenChanged?.Invoke(_panel.Visible);
    }

    private void OnSubmitted(string text)
    {
        _input.Text = "";
        text = text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        Print("> " + text);

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (_commands.TryGetValue(parts[0], out var command))
        {
            try
            {
                command.Run(parts[1..]);
            }
            catch (Exception e)
            {
                Print("error: " + e.Message);
            }
        }
        else
        {
            Print($"Unknown command '{parts[0]}'; try help");
        }
    }

    private void Print(string line) => _output.AppendText(line + "\n");
}