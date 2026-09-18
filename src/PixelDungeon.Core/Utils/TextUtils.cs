using System.Globalization;

namespace PixelDungeon.Core.Utils;

public static class TextUtils
{
    public static string Capitalize(string str)
    {
        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    public static string Format(string format, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, format, args);
    }

    public const string Vowels = "aoeiu";

    public static string Indefinite(string noun)
    {
        if (noun.Length == 0)
        {
            return "a";
        }

        return (Vowels.IndexOf(char.ToLowerInvariant(noun[0])) != -1 ? "an " : "a ") + noun;
    }
}