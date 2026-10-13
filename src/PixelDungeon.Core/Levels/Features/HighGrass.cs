using PixelDungeon.Core.Items;
using PixelDungeon.Core.Scenes;

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
            // TODO: seed drop when Random.Int(18) <= Random.Int(level + 1)
            // TODO: Dewdrop when Random.Int(6) <= Random.Int(level + 1)

            // Seed
            if (herbalismLevel >= 0 && Random.Int(18) <= Random.Int(herbalismLevel + 1))
            {
                // not in java: no seed drops is registered until later
                // roll above still runs and nothing drops rather than an empty heap
                var seed = Generator.Random(Generator.Category.Seed);
                if (seed != null)
                {
                    level.Drop(seed, pos).Sprite.Drop();
                }
            }

            // Dew
            if (herbalismLevel >= 0 && Random.Int(6) <= Random.Int(herbalismLevel + 1))
            {
                level.Drop(new Dewdrop(), pos).Sprite.Drop();
            }
        }

        // TODO: warden gains barkskin and doubles leaf burst 4 -> 8
        // TODO: CellEmitter.Get(Pos).Burst(LeafParticle.LevelSpecific, leaves)

        Dungeon.Observe();
    }
}