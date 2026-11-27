using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Blobs;

public class Alchemy : Blob
{
    private int Pos;

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);

        for (var i = 0; i < Length; i++)
        {
            if (Cur[i] <= 0) continue;
            Pos = i;
            break;
        }
    }

    protected override void Evolve()
    {
        Volume = Off[Pos] = Cur[Pos];

        if (Dungeon.Visible[Pos])
        {
            // TODO: Journal.add(Journal.Feature.Alchemy)
        }
    }

    public override void Seed(int cell, int amount)
    {
        Cur[Pos] = 0;
        Pos = cell;
        Volume = Cur[Pos] = amount;
    }

    public static void Transmute(int cell)
    {
        var heap = Dungeon.Level.Heaps.GetValueOrDefault(cell);
        var result = heap?.Transmute();
        if (result != null)
        {
            Dungeon.Level.Drop(result, cell).Sprite.Drop(cell);
        }
    }
}