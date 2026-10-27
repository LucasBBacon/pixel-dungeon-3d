using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;
using CoreScene = PixelDungeon.Core.Scenes.GameScene;


namespace PixelDungeon.Client;

public partial class GameScene : Node3D, IGameView
{
    private const string TxtWelcome = "Welcome to the level {0} of Pixel Dungeon!";
    private const string TxtWelcomeBack = "Welcome back to the level {0} of Pixel Dungeon!";
    private const string TxtNightMode = "Be cautious, since the dungeon is even more dangerous at night!";

    private const string TxtChasm = "Your steps echo across the dungeon.";
    private const string TxtWater = "You hear the water splashing around you.";
    private const string TxtGrass = "The smell of vegetation is thick in the air.";
    private const string TxtSecrets = "The atmosphere hints that this floor hides many secrets.";

    private LevelRenderer _renderer;
    private Node3D _chars;
    private CameraRig _camera;
    private CharView _heroView;
    private CellSelector _selector;
    private StatusPane _pane;
    private Toolbar _toolbar;
    private DebugConsole _console;
    private int _openWindows;
    private readonly Queue<string> _startupCommands = new();
    private readonly Dictionary<Mob, CharView> _mobViews = new();
    private Node3D _heaps;
    private readonly Dictionary<Heap, HeapView> _heapViews = new();
    private GoldIndicator _goldIndicator;

    private static readonly Dictionary<Type, Color> MobColors = new()
    {
        [typeof(Rat)] = new Color(0.55f, 0.13f, 0.13f),
        [typeof(Albino)] = new Color(0.92f, 0.90f, 0.88f),
        [typeof(Gnoll)] = new Color(0.45f, 0.30f, 0.15f),
        [typeof(Crab)] = new Color(0.85f, 0.35f, 0.12f),
        [typeof(Rat)] = new Color(0.40f, 0.35f, 0.45f),
    };

    internal static Color ColorForMob(Mob mob) =>
        MobColors.TryGetValue(mob.GetType(), out var c)
            ? c
            : new Color(0.7f, 0.2f, 0.2f);

    public override void _Ready()
    {
        CoreScene.Instance = this;

        _renderer = new LevelRenderer { Name = "LevelRenderer" };
        AddChild(_renderer);

        _chars = new Node3D { Name = "Chars" };
        AddChild(_chars);

        _heaps = new Node3D { Name = "Heaps" };
        AddChild(_heaps);

        _camera = new CameraRig { Name = "CameraRig" };
        AddChild(_camera);

        // GameScene.java:206 adds the HealthIndicator after hero and before cell selector and status pane
        // bar is screen-space, layer under toolbar (4) and status pane (5) -> level-switch fade covers it
        // added after the rig, it is placed after the camera moves each frame.
        var overlay = new CanvasLayer { Name = "Overlay", Layer = 3 };
        AddChild(overlay);
        overlay.AddChild(HealthIndicatorView.Create(_camera));

        CellSelector.RegisterActions();
        _selector = new CellSelector { Name = "CellSelector" };
        _selector.Init(_camera);
        AddChild(_selector);

        _pane = new StatusPane { Name = "Ui" };
        AddChild(_pane);

        _selector.PromptChanged += text => _pane.ShowPrompt(text);
        _pane.PromptPressed += () => _selector.Cancel();

        _toolbar = new Toolbar { Name = "Toolbar" };
        AddChild(_toolbar);
        _toolbar.OnExamine = Examine;
        _toolbar.OnBackpack = OpenBackpack;
        _toolbar.OnChooseQuickSlot = slot => SelectItem(slot.Assign, BagMode.Quickslot, slot.SelectPrompt);
        // QuickSlot's second press calls GameScene.handleCell(lastTarget.pos). Each slot's crosshair ring lives in
        // the 3D world, where the Java adds crossM to the target sprite's parent. It sits under the scene root,
        // not under _chars, so ClearChars never frees it
        _toolbar.Primary.HandleCell = _selector.HandleCell;
        _toolbar.Secondary.HandleCell = _selector.HandleCell;
        AddChild(_toolbar.Primary.Crosshair);
        AddChild(_toolbar.Secondary.Crosshair);
        _selector.InventoryPressed += OpenBackpack;
        _selector.QuickSlotPressed += n => (n == 1 ? _toolbar.Primary : _toolbar.Secondary).Use();

        _goldIndicator = GoldIndicator.Create();
        _pane.AddChild(_goldIndicator);
        _pane.HeroPressed += OpenHeroWindow;

        // right edge above the toolbar, attack indicator over the loot indicator
        var tags = new VBoxContainer();
        tags.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        tags.GrowHorizontal = Control.GrowDirection.Begin;
        tags.GrowVertical = Control.GrowDirection.Begin;
        tags.OffsetRight = -8f;
        tags.OffsetBottom = -72f;
        tags.AddChild(AttackIndicatorView.Create(ColorForMob));
        tags.AddChild(LootIndicator.Create());
        _toolbar.AddChild(tags);

        _console = new DebugConsole { Name = "DebugConsole" };
        _console.AfterLevelChange = Build;
        _console.OpenChanged += _ => UpdateSelector();
        AddChild(_console);

        Dungeon.Reset();
        Interlevel.Mode = InterlevelMode.Descend;
        Interlevel.Run();
        Build();

        // `-- console="stairs down" console="where"` on the command line queues console commands
        // each runs once the hero is ready again, so a headless run can exercise the level switch
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("console=", StringComparison.Ordinal))
            {
                _startupCommands.Enqueue(arg["console=".Length..]);
            }
        }
    }

    public override void _ExitTree()
    {
        if (CoreScene.Instance == this)
        {
            CoreScene.Instance = NullGameView.Instance;
        }
    }

    // create() half of GameScene.java that runs after every level switch.
    private void Build()
    {
        _renderer.Rebuild(Dungeon.Level);

        ClearChars();

        foreach (var heap in Dungeon.Level.Heaps.Values)
        {
            AddHeap(heap);
        }

        _heroView = CharView.For(Dungeon.Hero, new Color(0.2f, 0.4f, 1f));
        _heroView.UpdateArmor(Dungeon.Hero.Tier());
        _chars.AddChild(_heroView);
        Dungeon.Hero.Sprite = _heroView;
        _heroView.Place(Dungeon.Hero.Pos);

        foreach (var mob in Dungeon.Level.Mobs)
        {
            AddMob(mob);
        }

        _camera.Follow(_heroView);
        _camera.Snap();

        Arrive();
        Dungeon.Observe();
        GD.Print($"Level built: depth {Dungeon.Depth}, hero at {Dungeon.Hero.Pos}, feeling {Dungeon.Level.Feeling}");

        // ui/StatusPane.java createChildren calls Dungeon.hero.belongings.countIronKeys() when a
        // new StatusPane is built for each game scene, this covers the same cases (startup,
        // every level switch, console level changes, restart) since a new StatusPane isn't built
        // here but Build() runs on all of them
        Dungeon.Hero.Belongings.CountIronKeys(); // StatusPane.createChildren in the Java
        _goldIndicator.ResetState();
        CancelQuickSlotTargeting();
        RefreshQuickSlots();
    }

    private void ClearChars()
    {
        foreach (var child in _chars.GetChildren())
        {
            if (child is CharView view)
            {
                view.InterruptMotion(); // kills the tween so no completion fires into a freed node
            }

            _chars.RemoveChild(child);
            child.QueueFree();
        }

        _heroView = null;
        if (Dungeon.Hero != null)
        {
            Dungeon.Hero.Sprite =
                NullCharView.Instance; // no core code can touch a freed node between ClearChars and Build
        }

        _mobViews.Clear();

        foreach (var (heap, view) in _heapViews)
        {
            heap.Sprite = NullHeapView.Instance; // no core code can touch a freed node
        }

        _heapViews.Clear();

        foreach (var child in _heaps.GetChildren())
        {
            _heaps.RemoveChild(child);
            child.QueueFree(); // discarded heaps included
        }
    }

    public override void _Process(double delta)
    {
        if (Dungeon.Hero == null || _switching)
        {
            return;
        }

        Actor.Process();

        if (Dungeon.Level != null)
        {
            foreach (var (mob, view) in _mobViews.ToArray())
            {
                if (!Dungeon.Level.Mobs.Contains(mob))
                {
                    _mobViews.Remove(mob);
                    view.InterruptMotion(); // kills the tween so no completion fires into a freed node
                    // InterruptMotion() never raises Finished, so if this mob was Actor._current
                    // (frozen mid its own attack tween) nothing would ever clear that pointer and
                    // Actor.Process() would stop dispatching turns to everyone, forever
                    // Next() is a no-op for any mob that is not current, so this is free insurance
                    mob.Next();
                    mob.Sprite = NullCharView.Instance;
                    view.QueueFree();
                    continue;
                }

                view.Visible = Dungeon.Visible[mob.Pos];
                view.SetSleeping(mob.State == mob.SleepingState);
            }

            foreach (var (heap, view) in _heapViews.ToArray())
            {
                var stale = !IsInstanceValid(view) ||
                            view.IsQueuedForDeletion() ||
                            !Dungeon.Level.Heaps.TryGetValue(heap.Pos, out var current) ||
                            current != heap;
                if (stale)
                {
                    // Heap.Destroy kills its own view, net for any other way out of Level.Heaps
                    _heapViews.Remove(heap);
                    if (IsInstanceValid(view))
                    {
                        view.QueueFree();
                    }

                    if (heap.Sprite == view)
                    {
                        heap.Sprite = NullHeapView.Instance;
                    }

                    continue;
                }

                var p = heap.Pos;
                view.Visible = Dungeon.Visible[p] || Dungeon.Level.Visited[p] || Dungeon.Level.Mapped[p];
                view.SetLit(Dungeon.Visible[p]);
            }
        }

        if (_startupCommands.Count > 0 && Dungeon.Hero.Ready && _openWindows == 0)
        {
            _console.Execute(_startupCommands.Dequeue());
        }
    }

    private void Arrive()
    {
        if (Interlevel.Mode == InterlevelMode.None)
        {
            return;
        }

        // TODO: RESURRECT and RETURN arrive with WandOfBlink.appear and a Flare
        if (Interlevel.Mode == InterlevelMode.Fall)
        {
            Chasm.HeroLand();
        }

        // TODO: WndStory chapter windows on depths 1, 6, 11, 16, 22
        CoreScene.LandDroppedItems();

        if (Dungeon.Depth < Statistics.DeepestFloor)
        {
            GLog.H(TxtWelcomeBack, Dungeon.Depth);
        }
        else
        {
            GLog.H(TxtWelcome, Dungeon.Depth);
            Sample.Play(Assets.SndDescend);
        }

        switch (Dungeon.Level.Feeling)
        {
            case LevelFeeling.Chasm:
                GLog.W(TxtChasm);
                break;
            case LevelFeeling.Water:
                GLog.W(TxtWater);
                break;
            case LevelFeeling.Grass:
                GLog.W(TxtGrass);
                break;
        }

        if (Dungeon.Level is RegularLevel regular && regular.SecretDoors > Random.IntRange(3, 4))
        {
            GLog.W(TxtSecrets);
        }

        if (Dungeon.NightMode && !Dungeon.BossLevel())
        {
            GLog.W(TxtNightMode);
        }

        Interlevel.Mode = InterlevelMode.None;
    }

    // ---- IGameView ----

    public void UpdateMap()
    {
        _renderer.UpdateMap();
    }

    public void UpdateMap(int cell)
    {
        _renderer.UpdateMap(cell);
    }

    public void DiscoverTile(int cell, int oldTerrain)
    {
        _renderer.UpdateMap(cell);
    }

    public void AfterObserve() =>
        _renderer.UpdateVisibility(Dungeon.Visible, Dungeon.Level.Visited, Dungeon.Level.Mapped);

    void IGameView.Ready()
    {
        // GameScene.ready(), back to the default listener, then re-arm input
        _selector.SetListener(null);
        CancelQuickSlotTargeting();
        UpdateSelector();
    }

    private bool _switching;

    public void Log(string text, LogKind kind)
    {
        _pane.Log(text, kind);
        GD.Print($"[{kind}] {text}");
    }

    public void PlaySound(string id, float pitch)
    {
        // No audio assets in this port.
    }

    public void Shake(float magnitude, float duration)
    {
        _camera.Shake(magnitude, duration);
    }

    public void Effect(EffectKind kind, int cell)
    {
        if (kind == EffectKind.CheckedCell)
        {
            _renderer.Flash(cell);
        }
    }

    public void ShowWindow(WindowRequest request)
    {
        _openWindows++;
        UpdateSelector();
        _pane.ShowWindow(request, () =>
        {
            _openWindows--;
            UpdateSelector();
        });
    }

    // Game.SwitchScene(InterlevelScene) in java, fade out run transition, rebuild, fade in
    public void SwitchLevel(InterlevelMode mode)
    {
        if (_switching)
        {
            return;
        }

        _switching = true;
        UpdateSelector();
        // statement lambda: an expression would return the Task, godot try to convert it to a variant
        Callable.From(() => { _ = SwitchLevelAsync(); }).CallDeferred();
    }

    private async Task SwitchLevelAsync()
    {
        await FadeTo(1f);

        var depth = Dungeon.Depth;
        var pos = Dungeon.Hero.Pos;
        ClearChars();
        try
        {
            try
            {
                Interlevel.Run();
            }
            catch (Exception e)
            {
                // InterlevelScene shows "Something went wrong..." here, log it and go back to the level we left
                GD.PushError(e.ToString());
                GLog.N("Something went wrong...");
                Dungeon.Depth = depth;
                var level = Dungeon.LoadLevel();
                Dungeon.SwitchLevel(level, pos);
                Interlevel.Mode = InterlevelMode.None;
            }

            Build();
        }
        catch (Exception e)
        {
            // the recovery or the rebuild failed too, surface rather than freeze behind fade
            GD.PushError(e.ToString());
            GLog.N("Something went wrong...");
        }
        finally
        {
            await FadeTo(0f);
            _switching = false;
            UpdateSelector();
        }
    }

    private async Task FadeTo(float alpha)
    {
        var tween = CreateTween();
        tween.TweenProperty(_pane.Fade, "color:a", alpha, 0.3);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private void UpdateSelector()
    {
        _selector.Enabled = !_switching && _openWindows == 0 && !_console.IsOpen;
        if (_toolbar != null)
        {
            _toolbar.Blocked = !_selector.Enabled;
        }
    }

    public void AddHeap(Heap heap)
    {
        var view = HeapView.Create(discarded: false);
        _heaps.AddChild(view);
        heap.Sprite = view;
        view.Link(heap);
        _heapViews[heap] = view;
    }

    public void DiscardHeap(Heap heap)
    {
        var view = HeapView.Create(discarded: true);
        _heaps.AddChild(view);
        heap.Sprite = view;
        view.Link(heap);
    }

    public void AddMob(Mob mob)
    {
        var view = CharView.For(mob, ColorForMob(mob));
        _chars.AddChild(view);
        mob.Sprite = view;
        view.Place(mob.Pos);
        if (mob.Flying)
        {
            // applied after place.
            // Place() sets position outright, so an offset added before it
            // would be overwritten immediately
            view.Position += new Vector3(0f, 0.35f, 0f);
        }

        view.Visible = Dungeon.Visible[mob.Pos];
        _mobViews[mob] = view;
    }

    public void GameOver()
    {
        // TODO: java shows GAME_OVER banner and plays SND_DEATH then the rankings scene takes over

        // every death path (melee in Char.Attack, chasm falls, Bleeding) calls
        // GameScene.GameOver() from Hero.ReallyDie() *before* Dungeon.Fail(...) runs
        // and populates Dungeon.ResultDescription
        // the Java gets away with the same ordering because its gameOver() only shows
        // a banner and the rankings scene reads resultDescription much later
        // so defer by one frame and build the body inside the deferred call, once
        // ResultDescription is actually set
        // a plain method reference, not an expression lambda — this project has hit Godot's
        // lambda-to-Task conversion trap before (see the level-switch deferral above)
        Callable.From(ShowDeathWindow).CallDeferred();
    }

    private void ShowDeathWindow()
    {
        var body = $"{Dungeon.ResultDescription}\n\n" +
                   $"Level {Dungeon.Hero.Lvl}  ·  deepest floor {Statistics.DeepestFloor}";

        // Through the counted ShowWindow(request) path (not _pane.ShowWindow directly)
        // so _openWindows/UpdateSelector disable the selector while this is up
        // otherwise WASD input reaches a dead hero behind the modal
        ShowWindow(
            new WindowRequest("You died", body, ["Restart", "Quit"], index =>
            {
                if (index == 0)
                {
                    Restart();
                }
                else
                {
                    GetTree().Quit();
                }
            }));
    }

    // Toolbar.pickup, a swatch of the item flies from the hero into the backpack button
    public void PickUp(Item item)
    {
        if (_heroView == null || _toolbar == null)
        {
            return;
        }

        var from = _camera.Camera.UnprojectPosition(_heroView.GlobalPosition);
        var to = _toolbar.Backpack.GetGlobalRect().GetCenter();
        var swatch = new ColorRect
        {
            Color = ItemVisuals.ColorOf(item.Image()),
            Size = new Vector2(16f, 16f),
            Position = from,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _toolbar.AddChild(swatch);
        var tween = swatch.CreateTween();
        tween.TweenProperty(swatch, "position", to - swatch.Size / 2f, 0.2); // PickUpItem.Duration
        tween.TweenCallback(Callable.From(swatch.QueueFree));
    }

    public void SelectCell(ICellListener listener)
    {
        _selector.SetListener(listener);
    }

    public void Examine()
    {
        SelectCell(new ExamineListener(OpenHeroWindow));
    }

    public void OpenHeroWindow()
    {
        if (_switching || Dungeon.Hero == null)
        {
            return; // no window over a level that is being rebuilt behind the fade
        }

        // GameScene.show: cancelCellSelector() runs before the window opens, as it does in OpenBag
        _selector.Cancel();

        OpenWindow(WndHero.Create(), null);
    }

    public void Missile(int from, int to, Item item, Action onComplete) =>
        MissileView.Launch(this, from, to, item, onComplete);

    public void RefreshQuickSlots()
    {
        if (_toolbar != null)
        {
            _toolbar.Primary.Refresh();
            _toolbar.Secondary.Refresh();
        }
    }

    private void CancelQuickSlotTargeting()
    {
        if (_toolbar != null)
        {
            _toolbar.Primary.CancelTargeting();
            _toolbar.Secondary.CancelTargeting();
        }
    }

    public void ShowBag(Bag bag) =>
        OpenBag(WndBag.Create(bag, null, BagMode.All, null));

    // toolbar's backpack button and the "I" key
    public void OpenBackpack() =>
        OpenBag(WndBag.Create(Dungeon.Hero.Belongings.Backpack, null, BagMode.All, null));

    public void SelectItem(Action<Item> listener, BagMode mode, string title)
    {
        OpenBag(WndBag.LastBag(listener, mode, title));
    }

    private void OpenBag(WndBag wnd)
    {
        _selector.Cancel();

        wnd.ItemClicked += (owner, item) => OpenWindow(WndItem.Create(owner, item), owner);
        wnd.Reopened += OpenBag;
        OpenWindow(wnd, null);
    }

    private void OpenWindow(Window wnd, Window parent)
    {
        _openWindows++;
        UpdateSelector();

        var open = true;

        void Closed()
        {
            if (!open)
            {
                return;
            }

            open = false;
            _openWindows--;
            UpdateSelector();
        }

        wnd.VisibilityChanged += () =>
        {
            if (!wnd.Visible)
            {
                Closed();
                wnd.QueueFree();
            }
        };
        wnd.TreeExiting += Closed; // freed with its parent before it ever hid
        wnd.WindowInput += e =>
        {
            if (e.IsActionPressed("ui_cancel"))
            {
                wnd.Hide();
            }
        };
        (parent ?? (Node)_pane).AddChild(wnd);
        wnd.PopupCentered();
    }

    private void Restart()
    {
        Dungeon.Reset();
        Interlevel.Mode = InterlevelMode.Descend;
        Interlevel.Run();
        Build();
    }
}