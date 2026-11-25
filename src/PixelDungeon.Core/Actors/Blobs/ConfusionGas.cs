using PixelDungeon.Core.Actors.Buffs;

namespace PixelDungeon.Core.Actors.Blobs;

public class ConfusionGas : Blob
{
    protected override void Evolve()
    {
        base.Evolve();

        for (var i = 0; i < Length; i++)
        {
            Char ch;
            if (Cur[i] > 0 && (ch = FindChar(i)) != null)
            {
                Buff.Prolong<Vertigo>(ch, Vertigo.Duration(ch));
            }
        }
    }

    public override string TileDesc() => "A cloud of confusion gas is swirling here.";
}