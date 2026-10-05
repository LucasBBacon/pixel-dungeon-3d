namespace PixelDungeon.Core.Actors.Mobs;

public class Rat : Mob
{
    public Rat()
    {
        Name = "marsupial rat";

        HP = HT = 8;
        DefenseSkillValue = 3;

        MaxLvl = 5;
    }

    public override int DamageRoll() => Random.NormalIntRange(1, 5);

    public override int AttackSkill(Char target) => 8;

    public override int Dr() => 1;

    public override void Die(object cause)
    {
        // TODO: Ghost.Quest.ProcessSewersKill(pos)
        base.Die(cause);
    }

    public override string Description() =>
        "Marsupial rats are aggressive, but rather weak denizens " +
        "of the sewers. They can be dangerous only in big numbers.";
}