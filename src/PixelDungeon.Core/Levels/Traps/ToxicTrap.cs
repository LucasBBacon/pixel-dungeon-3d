using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Levels.Traps;

public static class ToxicTrap
{
    // 0x40FF40
    public static void Trigger(int pos, Char ch)
    {
        GameScene.Add(Blob.Seed<ToxicGas>(pos, 300 + 20 * Dungeon.Depth));
    }
}