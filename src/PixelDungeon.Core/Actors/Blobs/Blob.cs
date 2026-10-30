using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Blobs;

public class Blob : Actor
{
    public const int Width = Level.Width;
    public const int Height = Level.Height;
    public const int Length = Level.Length;

    public int Volume = 0;

    public int[] Cur;
    protected internal int[] Off;

    protected Blob()
    {
        Cur = new int[Length];
        Off = new int[Length];

        Volume = 0;
    }

    private const string TagCur = "cur";
    private const string TagStart = "start";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);

        var data = bundle.GetIntArray(TagCur);

        if (data == null) return;

        var start = bundle.GetInt(TagStart);
        for (var i = 0; i < data.Length; i++)
        {
            Cur[i + start] = data[i];
            Volume += data[i];
        }
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);

        if (Volume > 0)
        {
            int start;
            for (start = 0; start < Length; start++)
            {
                if (Cur[start] > 0)
                {
                    break;
                }
            }

            int end;
            for (end = Length - 1; end > start; end--)
            {
                if (Cur[end] > 0)
                {
                    break;
                }
            }

            bundle.Put(TagStart, start);
            bundle.Put(TagCur, Trim(start, end + 1));
        }
    }

    private int[] Trim(int start, int end)
    {
        var len = end - start;
        var copy = new int[len];
        Array.Copy(Cur, start, copy, 0, len);
        return copy;
    }

    protected override bool Act()
    {
        Spend(Tick);
        if (Volume > 0)
        {
            Volume = 0;
            Evolve();

            (Off, Cur) = (Cur, Off);
        }

        return true;
    }

    protected virtual void Evolve()
    {
        var notBlocking = BArray.Not(Level.Solid, null);

        for (var i = 1; i < Height - 1; i++)
        {
            var from = i * Width + 1;
            var to = from + Width - 2;

            for (var pos = from; pos < to; pos++)
            {
                if (notBlocking[pos])
                {
                    var count = 1;
                    var sum = Cur[pos];

                    if (notBlocking[pos - 1])
                    {
                        sum += Cur[pos - 1];
                        count++;
                    }

                    if (notBlocking[pos + 1])
                    {
                        sum += Cur[pos + 1];
                        count++;
                    }

                    if (notBlocking[pos - Width])
                    {
                        sum += Cur[pos - Width];
                        count++;
                    }

                    if (notBlocking[pos + Width])
                    {
                        sum += Cur[pos + Width];
                        count++;
                    }

                    var value = sum >= count ? (sum / count) - 1 : 0;
                    Off[pos] = value;

                    Volume += value;
                }
                else
                {
                    Off[pos] = 0;
                }
            }
        }
    }

    public virtual void Seed(int cell, int amount)
    {
        Cur[cell] += amount;
        Volume += amount;
    }

    public void Clear(int cell)
    {
        Volume -= Cur[cell];
        Cur[cell] = 0;
    }

    public virtual string TileDesc() => null;

    public static T Seed<T>(int cell, int amount) where T : Blob, new()
    {
        var gas = (T)Dungeon.Level.Blobs.GetValueOrDefault(typeof(T));
        if (gas == null)
        {
            gas = new T();
            Dungeon.Level.Blobs[typeof(T)] = gas;
        }

        gas.Seed(cell, amount);

        return gas;
    }
}