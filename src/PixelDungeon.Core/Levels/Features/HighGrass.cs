using PixelDungeon.Core.Items;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Levels.Features;

public static class HighGrass
{
    public static void Trample(Level level, int pos, Char ch)
    {
        Level.Set(pos, Terrain.Grass);
        GameScene.UpdateMap(pos);

        if (!Dungeon.IsChallenged(Challenges.NoHerbalism))
        {
            var herbalismLevel = 0;
            // TODO: ring of herbalism level

            // Seed
            if (herbalismLevel >= 0 && Random.Int(18) <= Random.Int(herbalismLevel + 1))
            {
                level.Drop(Generator.Random(Generator.Category.Seed), pos).Sprite.Drop();
            }

            // Dew
            if (herbalismLevel >= 0 && Random.Int(6) <= Random.Int(herbalismLevel + 1))
            {
                level.Drop(new Dewdrop(), pos).Sprite.Drop();
            }
        }

        // TODO: warden gains barkskin and doubles leaf burst 4 -> 8

        var leaves = 4;
        GameScene.Splash(pos, ColorMath.Midpoint(Dungeon.Level.Color1, Dungeon.Level.Color2), leaves);

        Dungeon.Observe();
    }
}