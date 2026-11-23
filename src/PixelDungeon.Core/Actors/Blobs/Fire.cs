using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;

namespace PixelDungeon.Core.Actors.Blobs;

public class Fire : Blob
{
    protected override void Evolve()
    {
        var flammable = Level.Flammable;

        var from = Width + 1;
        var to = Level.Length - Width - 1;

        var observe = false;

        for (var pos = from; pos < to; pos++)
        {
            int fire;

            if (Cur[pos] > 0)
            {
                Burn(pos);
                
                fire = Cur[pos] - 1;
                if (fire <= 0 && flammable[pos])
                {
                    var oldTile = Dungeon.Level.Map[pos];
                    Dungeon.Level.Destroy(pos);

                    observe = true;
                    GameScene.UpdateMap(pos);
                    if (Dungeon.Visible[pos])
                    {
                        GameScene.DiscoverTile(pos, oldTile);
                    }
                }
            }
            else
            {
                if (flammable[pos] && 
                    (Cur[pos - 1] > 0 || 
                     Cur[pos + 1] > 0 || 
                     Cur[pos - Width] > 0 || 
                     Cur[pos + Width] > 0
                     ))
                {
                    fire = 4;
                    Burn(pos);
                }
                else
                {
                    fire = 0;
                }
            }

            Volume += (Off[pos] = fire);
        }

        if (observe)
        {
            Dungeon.Observe();
        }
    }

    private void Burn(int pos)
    {
        var ch = FindChar(pos);
        if (ch != null)
        {
            Buff.Affect<Burning>(ch).Reignite(ch);
        }

        var heap = Dungeon.Level.Heaps.GetValueOrDefault(pos);
        heap?.Burn();
    }

    public override void Seed(int cell, int amount)
    {
        if (Cur[cell] == 0)
        {
            Volume += amount;
            Cur[cell] = amount;
        }
    }

    public override string TileDesc() => "A fire is raging here.";
}