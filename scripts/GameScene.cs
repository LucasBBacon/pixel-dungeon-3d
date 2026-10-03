using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;
using CoreScene = PixelDungeon.Core.Scenes.GameScene;


namespace PixelDungeon.Client;

public partial class GameScene : Node3D, IGameView
{
    private const string TXT_WELCOME = "Welcome to the level {0} of Pixel Dungeon!";
    private const string TXT_WELCOME_BACK = "Welcome back to the level {0} of Pixel Dungeon!";
    private const string TXT_NIGHT_MODE = "Be cautious, since the dungeon is even more dangerous at night!";

    private const string TXT_CHASM = "Your steps echo across the dungeon.";
    private const string TXT_WATER = "You hear the water splashing around you.";
    private const string TXT_GRASS = "The smell of vegetation is thick in the air.";
    private const string TXT_SECRETS = "The atmosphere hints that this floor hides many secrets.";

    private LevelRenderer _renderer;

    public override void _Ready()
    {
        CoreScene.Instance = this;

        _renderer = new LevelRenderer { Name = "LevelRenderer" };
        AddChild(_renderer);

        Dungeon.Reset();
        Interlevel.Mode = InterlevelMode.Descend;
        Interlevel.Run();
        Build();
    }

    // create() half of GameScene.java that runs after every level switch.
    private void Build()
    {
        _renderer.Rebuild(Dungeon.Level);
        Arrive();
        Dungeon.Observe();
        GD.Print($"Level built: depth {Dungeon.Depth}, hero at {Dungeon.Hero.Pos}, feeling {Dungeon.Level.Feeling}");
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
        // TODO: Dungeon.droppedItems for this depth land on random respawn cells

        if (Dungeon.Depth < Statistics.DeepestFloor)
        {
            GLog.H(TXT_WELCOME_BACK, Dungeon.Depth);
        }
        else
        {
            GLog.H(TXT_WELCOME, Dungeon.Depth);
            Sample.Play(Assets.SndDescend);
        }

        switch (Dungeon.Level.Feeling)
        {
            case LevelFeeling.Chasm:
                GLog.W(TXT_CHASM);
                break;
            case LevelFeeling.Water:
                GLog.W(TXT_WATER);
                break;
            case LevelFeeling.Grass:
                GLog.W(TXT_GRASS);
                break;
        }

        if (Dungeon.Level is RegularLevel regular && regular.SecretDoors > Random.IntRange(3, 4))
        {
            GLog.W(TXT_SECRETS);
        }

        if (Dungeon.NightMode && !Dungeon.BossLevel())
        {
            GLog.W(TXT_NIGHT_MODE);
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

    public void AfterObserve()
    {
        _renderer.UpdateVisibility(Dungeon.Visible, Dungeon.Level.Visited, Dungeon.Level.Mapped);
    }

    public new void Ready()
    {
    }

    public void Log(string text, LogKind kind)
    {
        GD.Print($"[{kind}] {text}");
    }

    public void PlaySound(string id, float pitch)
    {
        // No audio assets in this port.
    }

    public void Shake(float magnitude, float duration)
    {
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
        GD.Print($"[window] {request.Body}");
    }

    public void SwitchLevel(InterlevelMode mode)
    {
        // Called from inside Actor.Process; run the transition once the current frame is done.
        Callable.From(RunSwitch).CallDeferred();
    }

    private void RunSwitch()
    {
        Interlevel.Run();
        Build();
    }

    public void AddHeap(Heap heap)
    {
        // TODO: heap sprites
    }

    public void DiscardHeap(Heap heap)
    {
        // TODO: heap sprites
    }
}