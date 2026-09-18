using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Utils;

public class GLogTests
{
    [Fact]
    public void Kinds_ForwardWithFormatting()
    {
        var view = new RecordingGameView();
        GameScene.Instance = view;
        GLog.I("{0} hit {1}", "rat", "you");
        GLog.P("level up!");
        GLog.N("{0} killed you...", "a rat");
        GLog.W("You noticed something");
        GLog.H("Welcome to teh level {0} of Pixel Dungeon!", 3);
        Assert.Equal(("rat hit you", LogKind.Info), view.Logs[0]);
        Assert.Equal(("level up!", LogKind.Positive), view.Logs[1]);
        Assert.Equal(("a rat killed you...", LogKind.Negative), view.Logs[2]);
        Assert.Equal(("You noticed something", LogKind.Warning), view.Logs[3]);
        Assert.Equal(("Welcome to teh level 3 of Pixel Dungeon!", LogKind.Highlight), view.Logs[4]);
        GameScene.Instance = NullGameView.Instance;
    }
}