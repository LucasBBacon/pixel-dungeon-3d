using PixelDungeon.Core.Actors.Buffs;

namespace PixelDungeon.Core.Actors.Blobs;

public class ParalyticGas : Blob
{
    protected override void Evolve()
    {
        base.Evolve();

        Char ch;
        for (var i = 0; i < Length; i++)
        {
            if (Cur[i] > 0 && (ch = Actor.FindChar(i)) != null)
            {
                Buff.Prolong<Paralysis>(ch, Paralysis.Duration(ch));
            }
        }
    }

    public override string TileDesc() => "A cloud of paralytic gas is swirling here.";
}