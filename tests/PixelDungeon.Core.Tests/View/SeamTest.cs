using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
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
    public void UpdateArmor_TheNullViewIgnoresIt_AndTheFakeRecordsEachTier()
    {
        var nullView = NullCharView.Instance;
        nullView.UpdateArmor(3); // must not throw

        var fake = new FakeCharView();
        var seam = fake;
        seam.UpdateArmor(1);
        seam.UpdateArmor(0);

        Assert.Equal(new[] { 1, 0 }, fake.ArmorTiers);
    }

    [Fact]
    public void SpriteStatesReadAndFadeIn_TheNullViewIgnoresThem_AndTheFakeRecordsThem()
    {
        var nullView = NullCharView.Instance;
        nullView.Read();
        nullView.Add(CharState.Invisible);
        nullView.Remove(CharState.Levitating);
        nullView.FadeIn(); // none should throw

        var fake = new FakeCharView();
        var seam = fake;
        seam.Read();
        seam.Add(CharState.Levitating);
        seam.Remove(CharState.Levitating);
        seam.FadeIn();
        seam.Burst(0xFF33FF33, 4);
        seam.Burst(0xFFFFFFFF, 3);

        Assert.Equal(1, fake.Reads);
        Assert.Equal(new[] { (CharState.Levitating, true), (CharState.Levitating, false) }, fake.States);
        Assert.Equal(1, fake.FadeIns);
        Assert.Equal(new[] { (0xFF33FF33, 4), (0xFFFFFFFF, 3) }, fake.BurstLog);
        Assert.Equal(7, fake.Bursts);
    }

    [Fact]
    public void Facade_ForwardsSelectItemFlashSplashAndTheDiscoverEffect()
    {
        var chosen = new Item();
        var item = new Item();

        GameScene.SelectItem(i => chosen = i, BagMode.Unidentified, "Select an item to identify");
        GameScene.Flash(0xFFFFFF);
        GameScene.Splash(9, 0xFF40E0D0, 5);
        GameScene.Effect(EffectKind.Discover, 12);

        Assert.Single(View.ItemSelections);
        Assert.Equal(BagMode.Unidentified, View.ItemSelections[0].Mode);
        Assert.Equal("Select an item to identify", View.ItemSelections[0].Title);
        View.ChooseItem(item);
        Assert.Same(item, chosen);
        View.ChooseItem(null);
        Assert.Null(chosen);
        Assert.Equal(new[] { 0xFFFFFFu }, View.Flashes);
        Assert.Equal((9, 0xFF40E0D0u, 5), View.Splashes[0]);
        Assert.Equal((EffectKind.Discover, 12), View.Effects[0]);
    }

    [Fact]
    public void GameSceneAdd_Blob_SchedulesItAndHandsItToTheView()
    {
        var blob = new TestBlob();

        GameScene.Add(blob);

        Assert.Contains(blob, Actor.All());
        Assert.Same(blob, View.AddedBlobs[0]);
    }

    [Fact]
    public void NullGameView_IgnoresSelectItemBlobsFlashAndSplash()
    {
        var view = NullGameView.Instance;
        view.SelectItem(_ => { }, BagMode.All, null);
        view.AddBlob(new TestBlob());
        view.Flash(0xFFFFFF);
        view.Splash(1, 0xFFFFFFFF, 5);
    }

    [Fact]
    public void WindowRequest_IgnoresBack_DefaultsToFalse_AndCanBeSet()
    {
        Assert.False(WindowRequest.Message("Hello").IgnoresBack);
        Assert.False(new WindowRequest("T",
            "B",
            new[]
            {
                "Yes",
                "No"
            },
            _ => { }).IgnoresBack);
        Assert.True(new WindowRequest("T",
            "B",
            new[]
            {
                "Yes",
                "No"
            },
            _ => { },
            ignoresBack: true).IgnoresBack);
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

    private sealed class TestBlob : Blob
    {
    }

    // minimal mob for now
    private sealed class TestMob : Mob
    {
        protected override bool Act() => false;
    }
}