using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Ui;

public static class AttackIndicator
{
    public static Mob LastTarget = null;

    private static readonly List<Mob> Candidates = [];

    public static void UpdateState()
    {
        var heroPos = Dungeon.Hero.Pos;
        Candidates.Clear();
        var v = Dungeon.Hero.VisibleEnemies();
        for (var i = 0; i < v; i++)
        {
            var mob = Dungeon.Hero.VisibleEnemy(i);
            if (Level.Adjacent(heroPos, mob.Pos))
            {
                Candidates.Add(mob);
            }
        }

        if (!Candidates.Contains(LastTarget))
        {
            LastTarget = Candidates.Count == 0
                ? null
                : Random.Element(Candidates);
        }
    }

    public static void Target(Char target)
    {
        LastTarget = (Mob)target;
        // TODO: HealthIndicator.instance.target(target
    }
}