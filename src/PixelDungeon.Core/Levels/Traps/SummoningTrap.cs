using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Wands;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Levels.Traps;

public static class SummoningTrap
{
    private const float Delay = 2f;

    private sealed class Dummy : Mob
    {
    }

    private static readonly Mob DummyMob = new Dummy();

    // 0x770088
    public static void Trigger(int pos, Char ch)
    {
        if (Dungeon.BossLevel())
        {
            return;
        }

        var nMobs = 1;
        if (Random.Int(2) == 0)
        {
            nMobs++;
            if (Random.Int(2) == 0)
            {
                nMobs++;
            }
        }

        var candidates = Level.Neighbours8
            .Select(t => pos + t)
            .Where(p => Actor.FindChar(p) == null && (Level.Passable[p] || Level.Avoid[p]))
            .ToList();

        var respawnPoints = new List<int>();

        while (nMobs > 0 && candidates.Count > 0)
        {
            var index = Random.Index(candidates);

            DummyMob.Pos = candidates[index];
            Actor.OccupyCell(DummyMob);

            respawnPoints.Add(candidates[index]);
            candidates.RemoveAt(index);
            nMobs--;
        }

        foreach (var point in respawnPoints)
        {
            var mob = Bestiary.Mob(Dungeon.Depth);
            if (mob == null)
            {
                // TODO: Bestiary returns null at depths whose mobs are not ported yet
                continue;
            }

            mob.State = mob.WanderingState;
            GameScene.Add(mob, Delay);
            WandOfBlink.Appear(mob, point);
        }
    }
}