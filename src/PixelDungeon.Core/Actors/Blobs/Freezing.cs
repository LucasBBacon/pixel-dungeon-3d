using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Actors.Blobs;

public static class Freezing
{
    public static bool Affect(int cell, Fire fire)
    {
        var ch = Actor.FindChar(cell);
        if (ch != null)
        {
            Buff.Prolong<Frost>(ch, Frost.Duration(ch) * Random.Float(1.0f, 1.5f));
        }

        fire?.Clear(cell);

        var heap = Dungeon.Level.Heaps.GetValueOrDefault(cell);
        heap?.Freeze();
        
        if (Dungeon.Visible[cell])
        {
            GameScene.Splash(cell, 0xFFFFFFFF, 6);
            return true;
        }
        
        return false;
    }
}