using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Levels.Traps;

public static class FireTrap
{
    // 0xFF7708
    public static void Trigger(int pos, Char ch)
    {
        GameScene.Add(Blob.Seed<Fire>(pos, 2));
        GameScene.Splash(pos, 0xFFEE7722, 5);
    }
}