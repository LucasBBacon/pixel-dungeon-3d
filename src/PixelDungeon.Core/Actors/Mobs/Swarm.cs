using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Mobs;

public class Swarm : Mob
{
    public Swarm()
    {
        Name = "swarm of flies";

        HP = HT = 80;
        DefenseSkillValue = 5;

        MaxLvl = 10;

        Flying = true;
    }

    private const float SplitDelay = 1f;

    private int _generation = 0;

    private const string TagGeneration = "generation";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        _generation = bundle.GetInt(TagGeneration);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagGeneration, _generation);
    }

    public override int DamageRoll() => Random.NormalIntRange(1, 4);

    public override int DefenseProc(Char enemy, int damage)
    {
        if (HP >= damage + 2)
        {
            var candidates = new List<int>();
            var passable = Level.Passable;

            int[] neighbours =
            [
                Pos + 1,
                Pos - 1,
                Pos + Level.Width,
                Pos - Level.Width
            ];
            foreach (var neighbour in neighbours)
            {
                if (passable[neighbour] && Actor.FindChar(neighbour) == null)
                {
                    candidates.Add(neighbour);
                }
            }

            if (candidates.Count > 0)
            {
                var clone = Split();
                clone.HP = (HP - damage) / 2;
                clone.Pos = Random.Element(candidates);
                clone.State = clone.HuntingState;

                if (Dungeon.Level.Map[clone.Pos] == Terrain.Door)
                {
                    Door.Enter(clone.Pos);
                }

                GameScene.Add(clone, SplitDelay);
                // TODO: Actor.AddDelayed(new Pushing(clone, pos, clone.pos), -1) slide clone out

                HP -= clone.HP;
            }
        }

        return damage;
    }

    public override int AttackSkill(Char target) => 12;

    public override string DefenseVerb() => "evaded";

    private Swarm Split()
    {
        var clone = new Swarm { _generation = _generation + 1 };
        // TODO: a burning parent reignites the clone and a Poison parent sets its poison to 2
        return clone;
    }

    protected override void DropLoot()
    {
        if (Random.Int(5 * (_generation + 1)) == 0)
        {
        }
        // TODO: Dungeon.Level.Drop(new PotionOfHealing(), pos).Sprite.Drop()
    }

    public override string Description() =>
        "The deadly swarm of flies buzzes angrily. Every non-magical attack " +
        "will split it into two smaller but equally dangerous swarms.";
}