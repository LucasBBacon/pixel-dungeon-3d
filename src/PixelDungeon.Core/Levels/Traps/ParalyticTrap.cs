using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Levels.Traps;

public static class ParalyticTrap
{
    // 0xFFFF66
    public static void Trigger(int pos, Char ch)
    {
        GameScene.Add(Blob.Seed<ParalyticGas>(pos, 80 + 5 * Dungeon.Depth));
    }
}