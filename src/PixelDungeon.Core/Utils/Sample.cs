using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Utils;

public static class Sample
{
    public static void Play(string id)
    {
        Play(id, 1f);
    }

    public static void Play(string id, float pitch)
    {
        GameScene.Instance.PlaySound(id, pitch);
    }
}