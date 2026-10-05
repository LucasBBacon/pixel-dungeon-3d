namespace PixelDungeon.Core.Actors.Mobs;

public class Gnoll : Mob
{
    public Gnoll()
    {
        Name = "gnoll scout";

        HP = HT = 12;
        DefenseSkillValue = 4;

        Exp = 2;
        MaxLvl = 8;

        // TODO: loot = typeof(Gold)
        // TODO: lootChance = 0.5f
    }

    public override int DamageRoll() => Random.NormalIntRange(2, 5);

    public override int AttackSkill(Char target) => 11;

    public override int Dr() => 2;

    public override void Die(object cause)
    {
        // TODO: Ghost.Quest.ProcessSewersKill(pos)
        base.Die(cause);
    }

    public override string Description() =>
        "Gnolls are hyena-like humanoids. They dwell in sewers and dungeons, venturing up to raid the surface from time to time. " +
        "Gnoll scouts are regular members of their pack, they are not as strong as brutes and not as intelligent as shamans.";
}