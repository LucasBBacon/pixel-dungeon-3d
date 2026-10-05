namespace PixelDungeon.Core.Actors.Mobs;

public class Crab : Mob
{
    public Crab()
    {
        Name = "sewer crab";

        HP = HT = 15;
        DefenseSkillValue = 5;
        BaseSpeed = 2f;

        Exp = 3;
        MaxLvl = 9;

        // TODO: loot = new MysteryMeat
        // TODO: lootChance = 0.167f;
    }

    public override int DamageRoll() => Random.NormalIntRange(3, 6);

    public override int AttackSkill(Char target) => 12;

    public override int Dr() => 4;

    public override string DefenseVerb() => "parried";

    public override void Die(object cause)
    {
        // TODO: Ghost.Quest.ProcessSewersKill(pos)
        base.Die(cause);
    }

    public override string Description() =>
        "These huge crabs are at the top of the food chain in the sewers. " +
        "They are extremely fast and their thick exoskeleton can withstand " +
        "heavy blows.";
}