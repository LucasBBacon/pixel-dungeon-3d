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
            // TODO: ring of herbalism level
            // TODO: seed drop when Random.Int(18) <= Random.Int(level + 1)
            // TODO: Dewdrop when Random.Int(6) <= Random.Int(level + 1)
        }

        // TODO: warden gains barkskin and doubles leaf burst 4 -> 8
        // TODO: CellEmitter.Get(Pos).Burst(LeafParticle.LevelSpecific, leaves)

        Dungeon.Observe();
    }
}