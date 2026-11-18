using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Mobs.Npcs;

public class MirrorImage : Npc
{
    public MirrorImage()
    {
        Name = "mirror image";

        State = HuntingState;
    }

    public int Tier;

    private int _attack;
    private int _damage;

    private const string TierTag = "tier";
    private const string AttackTag = "attack";
    private const string DamageTag = "damage";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        Tier = bundle.GetInt(TierTag);
        _attack = bundle.GetInt(AttackTag);
        _damage = bundle.GetInt(DamageTag);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TierTag, Tier);
        bundle.Put(AttackTag, _attack);
        bundle.Put(DamageTag, _damage);
    }

    public void Duplicate(Hero.Hero hero)
    {
        Tier = hero.Tier();
        _attack = hero.AttackSkill(hero);
        _damage = hero.DamageRoll();
    }

    public override int AttackSkill(Char target) => _attack;

    public override int DamageRoll() => _damage;

    public override int AttackProc(Char enemy, int damage)
    {
        var dmg = base.AttackProc(enemy, damage);

        Destroy();
        Sprite.Die();

        return dmg;
    }

    protected override Char ChooseEnemy()
    {
        if (Enemy != null && Enemy.IsAlive()) return Enemy;

        // derivation: java draws Random.element from a HashSet of candidates
        // here we collect them in a list, in Level.Mobs order
        var enemies = Dungeon.Level.Mobs
            .Where(mob => mob.Hostile && Level.FieldOfView[mob.Pos])
            .ToList();
        return enemies.Count > 0 ? Random.Element(enemies) : null;
    }

    public override string Description() =>
        "This illusion bears a close resemblance to you, " +
        "but it's paler and twitches a little.";

    public override void Interact()
    {
        var curPos = Pos;

        MoveSprite(Pos, Dungeon.Hero.Pos);
        Move(Dungeon.Hero.Pos);

        Dungeon.Hero.Sprite.Move(Dungeon.Hero.Pos, curPos);
        Dungeon.Hero.Move(curPos);

        Dungeon.Hero.Spend(1 / Dungeon.Hero.Speed());
        Dungeon.Hero.Busy();
    }

    private static readonly HashSet<Type> ImmunitiesSet =
    [
        typeof(ToxicGas),
        // TODO: Burning.class
    ];

    public override HashSet<Type> Immunities() => ImmunitiesSet;
}