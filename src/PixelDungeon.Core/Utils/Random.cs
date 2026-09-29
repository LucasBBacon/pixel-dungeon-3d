namespace PixelDungeon.Core.Utils;

public static class Random
{
    private const long Multiplier = 0x5DEECE66DL;
    private const long Increment = 0xBL;
    private const long Mask = (1L << 48) - 1;

    private static long _seed = 0;

    public static void Seed(int seed)
    {
        _seed = (seed ^ Multiplier) & Mask;
    }

    private static int Next(int bits)
    {
        _seed = (_seed * Multiplier + Increment) & Mask;
        return (int)(_seed >> (48 - bits));
    }

    private static double NextDouble()
    {
        return (((long)Next(26) << 27) + Next(27)) / (double)(1L << 53);
    }

    public static float Float()
    {
        return (float)NextDouble();
    }

    public static float Float(float max)
    {
        return (float)(NextDouble() * max);
    }

    public static float Float(float min, float max)
    {
        return min + (float)(NextDouble() * (max - min));
    }

    public static int Int(int max)
    {
        return max > 0 ? (int)(NextDouble() * max) : 0;
    }

    public static int Int(int min, int max)
    {
        var range = max - min;
        return range <= 0 ? min : min + (int)(NextDouble() * range);
    }

    public static int IntRange(int min, int max)
    {
        var range = max - min + 1;
        return range <= 0 ? min : min + (int)(NextDouble() * range);
    }

    public static int NormalIntRange(int min, int max)
    {
        var range = max - min + 1;
        return range <= 0 ? min : min + (int)((NextDouble() + NextDouble()) * range / 2f);
    }

    public static int Chances(float[] chances)
    {
        var length = chances.Length;

        var sum = chances[0];
        for (var i = 1; i < length; i++)
        {
            sum += chances[i];
        }

        var value = Float(sum);
        sum = chances[0];
        for (var i = 0; i < length; i++)
        {
            if (value < sum)
            {
                return i;
            }

            if (i + 1 < length)
            {
                sum += chances[i + 1];
            }
        }

        return 0;
    }

    public static TK Chances<TK>(Dictionary<TK, float> chances)
    {
        var size = chances.Count;

        var values = chances.Keys.ToArray();
        var probabilities = new float[size];
        float sum = 0;
        for (var i = 0; i < size; i++)
        {
            probabilities[i] = chances[values[i]];
            sum += probabilities[i];
        }

        var value = Float(sum);

        sum = probabilities[0];
        for (var i = 0; i < size; i++)
        {
            if (value < sum)
            {
                return values[i];
            }
            if (i + 1 < size)
            {
                sum += probabilities[i + 1];
            }
        }

        return default;
    }

    public static T Element<T>(T[] array)
    {
        return Element(array, array.Length);
    }

    public static T Element<T>(T[] array, int max)
    {
        return array[(int)(NextDouble() * max)];
    }

    public static T Element<T>(ICollection<T> collection)
    {
        var size = collection.Count;
        return size > 0 ? collection.ElementAt(Int(size)) : default;
    }

    public static T OneOf<T>(params T[] array)
    {
        return array[(int)(NextDouble() * array.Length)];
    }

    public static int Index<T>(ICollection<T> collection)
    {
        return (int)(NextDouble() * collection.Count);
    }

    public static void Shuffle<T>(T[] array)
    {
        for (var i = 0; i < array.Length - 1; i++)
        {
            var j = Int(i, array.Length);
            if (j != i)
            {
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }

    public static void Shuffle<TU, TV>(TU[] u, TV[] v)
    {
        for (var i = 0; i < u.Length; i++)
        {
            var j = Int(i, u.Length);

            if (j == i) continue;

            (u[i], u[j]) = (u[j], u[i]);
            (v[i], v[j]) = (v[j], v[i]);
        }
    }
}