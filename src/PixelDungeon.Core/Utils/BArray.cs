namespace PixelDungeon.Core.Utils;

public static class BArray
{
    public static bool[] And(bool[] a, bool[] b, bool[] result)
    {
        var length = a.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = a[i] && b[i];
        }

        return result;
    }

    public static bool[] Or(bool[] a, bool[] b, bool[] result)
    {
        var length = a.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = a[i] || b[i];
        }

        return result;
    }

    public static bool[] Not(bool[] a, bool[] result)
    {
        var length = a.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = !a[i];
        }

        return result;
    }

    public static bool[] Is(int[] a, bool[] result, int v1)
    {
        var length = a.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = a[i] == v1;
        }

        return result;
    }

    public static bool[] IsOneOf(int[] a, bool[] result, params int[] v)
    {
        var length = a.Length;
        var nv = v.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = false;
            for (var j = 0; j < nv; j++)
            {
                if (a[i] != v[j]) continue;

                result[i] = true;
                break;
            }
        }

        return result;
    }

    public static bool[] IsNot(int[] a, bool[] result, int v1)
    {
        var length = a.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = a[i] != v1;
        }

        return result;
    }

    public static bool[] IsNotOneOf(int[] a, bool[] result, params int[] v)
    {
        var length = a.Length;
        var nv = v.Length;
        result ??= new bool[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = true;
            for (var j = 0; j < nv; j++)
            {
                if (a[i] != v[j]) continue;

                result[i] = false;
                break;
            }
        }

        return result;
    }
}