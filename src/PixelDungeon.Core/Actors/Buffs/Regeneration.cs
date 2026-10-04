namespace PixelDungeon.Core.Actors.Buffs;

public class Regeneration : Buff
{
    private const float RegenerationDelay = 10;

    protected override bool Act()
    {
        if (Target.IsAlive())
        {
            if (Target.HP < Target.HT && !((Hero.Hero)Target).IsStarving())
            {
                Target.HP += 1;
            }

            // TODO: RingOfMending.Rejuvenation levels divide the delay by 1.2^bonus
            Spend(RegenerationDelay);
        }
        else
        {
            Diactivate();
        }

        return true;
    }

    // Test shim, Act() is protected on Actor, tests drive one tick without scheduler's time bookkeeping
    public bool ActForTest() => Act();
}