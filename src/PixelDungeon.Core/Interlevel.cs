namespace PixelDungeon.Core;

public enum InterlevelMode
{
    Descend,
    Ascend,
    Continue,
    Resurrect,
    Return,
    Fall,
    None
}

public class Interlevel
{
    public static InterlevelMode Mode = InterlevelMode.None;
    public static int ReturnDepth;
    public static int ReturnPos;
    public static bool NoStory = false;
    public static bool FallIntoPit;
}