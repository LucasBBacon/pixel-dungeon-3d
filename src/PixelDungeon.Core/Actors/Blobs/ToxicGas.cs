using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Blobs;

public class ToxicGas : Blob, Hero.Hero.IDoom
{
    protected override void Evolve()
    {
        base.Evolve();

        var levelDamage = 5 + Dungeon.Depth * 5;

        Char ch;
        for (var i = 0; i < Length; i++)
        {
            if (Cur[i] > 0 && (ch = Actor.FindChar(i)) != null)
            {
                var damage = (ch.HT + levelDamage) / 40;
                if (Random.Int(40) < (ch.HT + levelDamage) % 40)
                {
                    damage++;
                }

                ch.Damage(damage, this);
            }
        }

        var blob = Dungeon.Level.Blobs.GetValueOrDefault(typeof(ParalyticGas));
        if (blob != null)
        {
            var par = blob.Cur;

            for (var i = 0; i < Length; i++)
            {
                var t = Cur[i];
                var p = par[i];

                if (p >= t)
                {
                    Volume -= t;
                    Cur[i] = 0;
                }
                else
                {
                    blob.Volume -= p;
                    par[i] = 0;
                }
            }
        }
    }

    public override string TileDesc() => "A greenish cloud of toxic gas is swirling here.";

    public void OnDeath()
    {
        // TODO: Badges.validateDeathFromGas()

        Dungeon.Fail(TextUtils.Format(ResultDescriptions.Gas, Dungeon.Depth));
        GLog.N("You died from a toxic gas..");
    }
}