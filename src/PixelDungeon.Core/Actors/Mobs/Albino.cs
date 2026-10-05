using PixelDungeon.Core.Actors.Buffs;

namespace PixelDungeon.Core.Actors.Mobs;

public class Albino : Rat
{
    public Albino()
    {
        Name = "albino rat";

        HP = HT = 15;
    }

    public override void Die(object cause)
    {
        base.Die(cause);
        // TODO: Badges.ValidateRate(this)
    }

    public override int AttackProc(Char enemy, int damage)
    {
        if (Random.Int(2) == 0)
        {
            Buff.Affect<Bleeding>(enemy).Set(damage);
        }

        return damage;
    }
}