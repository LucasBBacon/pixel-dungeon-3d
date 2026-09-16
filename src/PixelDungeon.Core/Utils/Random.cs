namespace PixelDungeon.Core.Utils;

public static class Random
{
    private static System.Random _rng = new System.Random();

    public static void Seed(int seed)
    {
        _rng = new System.Random(seed);
    }

    private static double Next()
    {
        return _rng.NextDouble();
    }

    public static float Float()
    {
        return (float)Next();
    }

    public static float Float(float max)
    {
        return (float)(Next() * max);
    }

    public static float Float(float max, float min)
    {
        return (float)(min + Next() * (max - min));
    }

    public static int Int(int max)
    {
        return max > 0 ? (int)(Next() * max) : 0;
    }

    public static int Int(int min, int max)
    {
        return min + (int)(Next() * (max - min));
    }

    public static int IntRange(int min, int max)
    {
        return min + (int)(Next() * (max - min + 1));
    }

    public static int NormalIntRange(int min, int max)
    {
        return min + (int)((Next() + Next()) * (max - min + 1) / 2f);
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

            // java reads chances[i + 1] unguarded, only overruns when weight = 0,
            // where it throws. guard makes case fall through to 0 default
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
        return array[(int)(Next() * max)];
    }

    public static T Element<T>(ICollection<T> collection)
    {
        var size = collection.Count;
        return size > 0 ? collection.ElementAt(Int(size)) : default;
    }

    public static T OneOf<T>(params T[] array)
    {
        return array[(int)(Next() * array.Length)];
    }

    public static int Index<T>(ICollection<T> collection)
    {
        return (int)(Next() * collection.Count);
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