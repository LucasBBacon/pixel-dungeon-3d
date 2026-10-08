using PixelDungeon.Core;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class SeamTest : DungeonFixture
{
    [Fact]
    public void GameScene_DefaultsToNullView_AndCanBeReplaced()
    {
        GameScene.Instance = NullGameView.Instance;
        GameScene.UpdateMap(5); // must not throw
        var view = new RecordingGameView();
        GameScene.Instance = view;
        GameScene.UpdateMap(7);
        GameScene.DiscoverTile(8, 16);
        GameScene.AfterObserve();
        GameScene.Ready();
        GameScene.Shake(2, 0.5f);
        GameScene.Effect(EffectKind.CheckedCell, 9);

        Assert.Equal(new[] { 7 }, view.UpdatedCells);
        Assert.Equal((8, 16), view.Discovered[0]);
        Assert.Equal(1, view.ObserveCount);
        Assert.Equal(1, view.ReadyCount);
        Assert.Equal((2f, 0.5f), view.Shakes[0]);
        Assert.Equal((EffectKind.CheckedCell, 9), view.Effects[0]);
        GameScene.Instance = NullGameView.Instance;
    }

    [Fact]
    public void Sample_Play_ForwardsIdAndPitch()
    {
        var view = new RecordingGameView();
        GameScene.Instance = view;
        Sample.Play("snd_step.mp3");
        Sample.Play("snd_water.mp3", 1.1f);
        Assert.Equal(("snd_step.mp3", 1f), view.Sounds[0]);
        Assert.Equal(("snd_water.mp3", 1.1f), view.Sounds[1]);
        GameScene.Instance = NullGameView.Instance;
    }

    [Fact]
    public void WindowRequest_Message_HasOneOption_AndSelectIsSafeWithoutCallback()
    {
        var request = WindowRequest.Message("Hello");
        Assert.Single(request.Options);
        Assert.False(request.IsChoice);
        request.Select(0);
        var chosen = -1;
        var choice = new WindowRequest("T", "B", ["Yes", "No"], i => chosen = i);
        Assert.True(choice.IsChoice);
        choice.Select(1);
        Assert.Equal(1, chosen);
    }

    [Fact]
    public void NullCharView_NeverMoves()
    {
        ICharView view = NullCharView.Instance;
        view.Move(1, 2);
        Assert.False(view.IsMoving);
        view.Visible = false;
        Assert.False(view.Visible);
    }

    [Fact]
    public void GameSceneAdd_Mob_AddsToLevelSchedulesAndOccupiesTheCell()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = new TestMob { Pos = TestLevel.At(3, 1) };

        GameScene.Add(mob);

        Assert.Contains(mob, Dungeon.Level.Mobs);
        Assert.Same(mob, Actor.FindChar(TestLevel.At(3, 1)));
        Assert.Contains(mob, View.AddedMobs);
    }

    [Fact]
    public void GameSceneAdd_MobWithDelay_SchedulesItLater()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var mob = new TestMob { Pos = TestLevel.At(3, 1) };

        GameScene.Add(mob, 5f);

        Assert.Contains(mob, Dungeon.Level.Mobs);
        Assert.Contains(mob, Actor.All());
        Assert.Contains(mob, View.AddedMobs);
    }

    [Fact]
    public void GameSceneGameOver_ForwardsToTheView()
    {
        GameScene.GameOver();
        Assert.Equal(1, View.GameOvers);
    }

    [Fact]
    public void Facade_ForwardsPickUpSelectCellMissileAndQuickSlotRefresh()
    {
        var item = new Item();
        var listener = new TestListener();
        var completed = 0;

        GameScene.PickUp(item);
        GameScene.SelectCell(listener);
        GameScene.Missile(3, 7, item, () => completed++);
        GameScene.RefreshQuickSlots();

        Assert.Same(item, View.PickedUp[0]);
        Assert.Same(listener, View.Listeners[0]);
        Assert.Equal((3, 7, item), View.Missiles[0]);
        Assert.Equal(1, completed);
        Assert.Equal(1, View.QuickSlotRefreshes);
    }

    [Fact]
    public void RecordingGameView_HeldMissile_CompletesOnlyWhenReleased()
    {
        var completed = 0;
        View.HoldMissiles = true;

        GameScene.Missile(1, 2, null, () => completed++);
        Assert.Equal(0, completed);

        View.CompleteMissile();
        Assert.Equal(1, completed);
    }

    [Fact]
    public void RecordingGameView_Answer_SelectsOnTheLatestListener()
    {
        var first = new TestListener();
        var second = new TestListener();
        GameScene.SelectCell(first);
        GameScene.SelectCell(second);

        View.Answer(42);

        Assert.Null(first.Selected);
        Assert.Equal(42, second.Selected);
    }

    [Fact]
    public void NullGameView_Missile_CompletesImmediately()
    {
        var completed = 0;
        NullGameView.Instance.Missile(1, 2, null, () => completed++);
        Assert.Equal(1, completed);
    }

    [Fact]
    public void Heap_Sprite_DefaultsToTheNullView()
    {
        Assert.Same(NullHeapView.Instance, new Heap().Sprite);
    }

    [Fact]
    public void AddHeap_AssignsALinkedHeapView()
    {
        var heap = new Heap { Pos = 9 };
        GameScene.Add(heap);
        var view = Assert.IsType<FakeHeapView>(heap.Sprite);
        Assert.Same(heap, view.Links[0]);
    }

    [Fact]
    public void Glowing_SplitsTheColourIntoChannels()
    {
        var glowing = new Glowing(0xFF8000);

        Assert.Equal(0xFF8000, glowing.Color);
        Assert.Equal(1f, glowing.Red);
        Assert.Equal(128 / 255f, glowing.Green);
        Assert.Equal(0f, glowing.Blue);
        Assert.Equal(1f, glowing.Period);
        Assert.Equal(0.6f, Glowing.White.Period);
    }

    private sealed class TestListener : ICellListener
    {
        public int? Selected;
        public void OnSelect(int? cell) => Selected = cell;
        public string Prompt() => "pick a cell";
    }

    // minimal mob for now
    private sealed class TestMob : Mob
    {
        protected override bool Act() => false;
    }
}