using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Keys;
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

            if (!RequireHeroReady())
            {
                return;
            }

            Dungeon.Hero.Interrupt();
            var guard = 0;
            try
            {
                while (Dungeon.Depth != target && guard++ < 64)
                {
                    var depth = Dungeon.Depth;
                    var pos = Dungeon.Hero.Pos;
                    Interlevel.Mode = Dungeon.Depth < target
                        ? InterlevelMode.Descend
                        : InterlevelMode.Ascend;
                    try
                    {
                        Interlevel.Run();
                    }
                    catch (Exception e)
                    {
                        // same recovery as GameScene.SwitchLevelAsync: every transition saves the level it leaves first
                        Print("error: " + e.Message);
                        Dungeon.Depth = depth;
                        Dungeon.SwitchLevel(Dungeon.LoadLevel(), pos);
                        Interlevel.Mode = InterlevelMode.None;
                        break;
                    }
                }
            }
            finally
            {
                AfterLevelChange?.Invoke(); // the view re-syncs with whatever the core now holds
            }

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

        // Stands the hero on the exit or entrance and takes it through Hero.Handle, so the real
        // faded level switch runs. Exists so the switch can be exercised without finding the stairs
        Register("stairs", "stairs down|up: stand on the exit or entrance and take it", args =>
        {
            if (args.Length != 1 || (args[0] != "down" && args[0] != "up"))
            {
                Print("usage: stairs down|up");
                return;
            }

            if (!RequireHeroReady())
            {
                return;
            }

            var cell = args[0] == "down" ? Dungeon.Level.Exit : Dungeon.Level.Entrance;
            Dungeon.Hero.Interrupt();
            Dungeon.Hero.Pos = cell;
            Dungeon.Hero.Sprite.Place(cell);
            Dungeon.Observe();
            Dungeon.Hero.Handle(cell);
            Print($"taking the stairs {args[0]}");
        });

        Register("spawn", "spawn <rat|albino|gnoll|crab|swarm> [n]: place mobs next to the hero", args =>
        {
            if (args.Length == 0)
            {
                Print("spawn <rat|albino|gnoll|crab|swarm> [n]");
                return;
            }

            Func<Mob> factory = args[0].ToLowerInvariant() switch
            {
                "rat" => () => new Rat(),
                "albino" => () => new Albino(),
                "gnoll" => () => new Gnoll(),
                "crab" => () => new Crab(),
                "swarm" => () => new Swarm(),
                _ => null,
            };

            if (factory == null)
            {
                Print($"unknown '{args[0]}'");
                return;
            }

            var count = args.Length > 1 && int.TryParse(args[1], out var n) ? n : 1;
            var placed = 0;

            foreach (var offset in Level.Neighbours8)
            {
                if (placed >= count)
                {
                    break;
                }

                var cell = Dungeon.Hero.Pos + offset;
                if (Level.Passable[cell] && Actor.FindChar(cell) == null)
                {
                    var mob = factory();
                    mob.Pos = cell;
                    Core.Scenes.GameScene.Add(mob);
                    placed++;
                }
            }

            Print($"spawned {placed}");
        });

        Register("kill", "destroy every mob in the hero's field of view", _ =>
        {
            // Level.FieldOfView is one shared buffer that every actor's Act() overwrites with its
            // own viewshed; without recomputing it here it could hold whatever mob last moved, not
            // the hero's view. Dungeon.Observe() re-runs Level.UpdateFieldOfView(Hero) so this reads
            // the hero's own field of view, as the help text promises.
            Dungeon.Observe();
            var killed = 0;
            foreach (var mob in Dungeon.Level.Mobs.ToArray())
            {
                if (Level.FieldOfView[mob.Pos])
                {
                    mob.Die(Dungeon.Hero);
                    killed++;
                }
            }

            Print($"killed {killed}");
        });

        Register("give",
            "give <gold N|dewdrop N|dewvial|ironkey|goldenkey|skeletonkey|keyrin>: add to the hero's belongings",
            args =>
            {
                if (args.Length == 0)
                {
                    Print("usage: give <gold N|dewdrop N|dewvial|ironkey|goldenkey|skeletonkey|keyring>");
                    return;
                }

                var name = args[0].ToLowerInvariant();
                var n = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 1;

                if (name == "gold")
                {
                    Dungeon.Gold += n;
                    Print($"gold now {Dungeon.Gold}");
                    return;
                }

                var item = MakeItem(name, n);
                if (item == null)
                {
                    Print($"unknown item '{args[0]}'");
                    return;
                }

                Print(item.Collect() ? $"gave {item}" : "the pack is full");
            });

        Register("heap", "heap <heap|chest|locked|crystal|skeleton|mimic> [item] [n]: put a heap next to the hero",
            args =>
            {
                HeapType? type = args.Length == 0
                    ? null
                    : args[0].ToLowerInvariant() switch
                    {
                        "heap" => HeapType.Heap,
                        "chest" => HeapType.Chest,
                        "locked" => HeapType.LockedChest,
                        "crystal" => HeapType.CrystalChest,
                        "skeleton" => HeapType.Skeleton,
                        "mimic" => HeapType.Mimic,
                        _ => null,
                    };
                if (type == null)
                {
                    Print("usage: heap <heap|chest|locked|crystal|skeleton|mimic> [item] [n]");
                    return;
                }

                var itemName = args.Length > 1
                    ? args[1].ToLowerInvariant()
                    : "gold";
                var n = args.Length > 2 && int.TryParse(args[2], out var parsed)
                    ? parsed
                    : (itemName == "gold" ? 50 : 1);
                var item = MakeItem(itemName, n);
                if (item == null)
                {
                    Print($"unknown item '{args[1]}'");
                    return;
                }

                foreach (var offset in Level.Neighbours8)
                {
                    var cell = Dungeon.Hero.Pos + offset;
                    if (Level.Passable[cell] && Actor.FindChar(cell) == null && !Dungeon.Level.Heaps.ContainsKey(cell))
                    {
                        var heap = Dungeon.Level.Drop(item, cell);
                        heap.Type = type.Value;
                        heap.Sprite.Link(); // the view drew it as a plain heap before the type changed
                        heap.Sprite.Drop();
                        Print($"{type.Value} with {item} at {cell}");
                        return;
                    }
                }

                Print("no free cell next to the hero");
            });

        Register("lock", "turn the nearest visible door into a locked door", _ =>
        {
            var pos = Dungeon.Hero.Pos;
            var best = -1;
            for (var i = 0; i < Level.Length; i++)
            {
                if (Dungeon.Visible[i] && Dungeon.Level.Map[i] == Terrain.Door &&
                    (best == -1 || Level.Distance(pos, i) < Level.Distance(pos, best)))
                {
                    best = i;
                }
            }

            if (best == -1)
            {
                Print("no visible door");
                return;
            }

            Level.Set(best, Terrain.LockedDoor);
            PixelDungeon.Core.Scenes.GameScene.UpdateMap(best);
            Print($"locked the door at {best}");
        });

        Register("fill", "fill the dew vial in the backpack", _ =>
        {
            DewVial vial = Dungeon.Hero.Belongings.GetItem<DewVial>();
            if (vial == null)
            {
                Print("no dew vial; try give dewvial");
                return;
            }

            vial.Fill();
            Print("dew vial filled");
        });
    }

    public void Register(string name, string help, Action<string[]> run) => _commands[name] = (help, run);

    private bool RequireHeroReady()
    {
        if (Dungeon.Hero.Ready)
        {
            return true;
        }

        Print("the hero is busy; try again when it is ready");
        return false;
    }

    private static Item MakeItem(string name, int n)
    {
        switch (name)
        {
            case "gold":
                return new Gold(n);
            case "dewdrop":
                var dew = new Dewdrop();
                dew.Quantity(n);
                return dew;
            case "dewvial":
                return new DewVial();
            case "ironkey":
                return new IronKey();
            case "goldenkey":
                return new GoldenKey();
            case "skeletonkey":
                return new SkeletonKey();
            case "keyring":
                return new Keyring();
            default:
                return null;
        }
    }

    // runs one command line exactly as if it had been typed, also used for startup commands
    public void Execute(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        Print(">, text");

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
            Print($"unknown command '{parts[0]}'; try help");
        }
    }

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
        Execute(text);
    }

    // echoed to stdout too, so headless runs can prove what command did
    private void Print(string line)
    {
        _output.AppendText(line + "\n");
        GD.Print("[console] " + line);
    }
}