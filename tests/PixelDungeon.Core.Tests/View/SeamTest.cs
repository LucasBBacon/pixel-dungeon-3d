using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class SeamTest
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
}