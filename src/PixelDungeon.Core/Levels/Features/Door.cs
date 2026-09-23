using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Features;

public static class Door
{
    public static void Enter(int pos)
    {
        Level.Set(pos, Terrain.OpenDoor);
        GameScene.UpdateMap(pos);

        Dungeon.Observe();

        if (Dungeon.Visible[pos])
        {
            Sample.Play(Assets.SndOpen);
        }
    }

    public static void Leave(int pos)
    {
        if (Dungeon.Level.Heaps.GetValueOrDefault(pos) != null) return;

        Level.Set(pos, Terrain.Door);
        GameScene.UpdateMap(pos);
        Dungeon.Observe();
    }
}