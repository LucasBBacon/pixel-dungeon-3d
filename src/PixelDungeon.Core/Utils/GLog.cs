using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Utils;

public static class GLog
{
    public static void I(string text, params object[] args)
    {
        Log(text, args, LogKind.Info);
    }

    public static void P(string text, params object[] args)
    {
        Log(text, args, LogKind.Positive);
    }

    public static void N(string text, params object[] args)
    {
        Log(text, args, LogKind.Negative);
    }

    public static void W(string text, params object[] args)
    {
        Log(text, args, LogKind.Warning);
    }

    public static void H(string text, params object[] args)
    {
        Log(text, args, LogKind.Highlight);
    }

    private static void Log(string text, object[] args, LogKind kind)
    {
        if (args.Length > 0)
        {
            text = TextUtils.Format(text, args);
        }

        GameScene.Instance.Log(text, kind);
    }
}