using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Levels;

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

public static class Interlevel
{
    public static InterlevelMode Mode = InterlevelMode.None;
    public static int ReturnDepth;
    public static int ReturnPos;
    public static bool NoStory = false;
    public static bool FallIntoPit;

    // runs the transition for Mode
    // Mode is left set so the view can run the arrival and then clear it, just like GameScene.create in the java
    public static void Run()
    {
        switch (Mode)
        {
            case InterlevelMode.Descend:
                Descend();
                break;
            case InterlevelMode.Ascend:
                Ascend();
                break;
            case InterlevelMode.Fall:
                Fall();
                break;
            default:
                // TODO: Continue, Return, Resurrect
                break;
        }
    }

    private static void Descend()
    {
        Actor.FixTime();
        if (Dungeon.Hero == null)
        {
            Dungeon.Init();
            if (NoStory)
            {
                // TODO: Dungeon.Chapters.Add(WndStory.IdSewers)
                NoStory = false;
            }
            // TODO: GameLog.Wipe()
        }
        else
        {
            Dungeon.SaveLevel();
        }

        Level level;
        if (Dungeon.Depth >= Statistics.DeepestFloor)
        {
            level = Dungeon.NewLevel();
        }
        else
        {
            Dungeon.Depth++;
            level = Dungeon.LoadLevel();
        }

        Dungeon.SwitchLevel(level, level.Entrance);
    }

    private static void Fall()
    {
        Actor.FixTime();
        Dungeon.SaveLevel();

        Level level;
        if (Dungeon.Depth >= Statistics.DeepestFloor)
        {
            level = Dungeon.NewLevel();
        }
        else
        {
            Dungeon.Depth++;
            level = Dungeon.LoadLevel();
        }

        Dungeon.SwitchLevel(level, FallIntoPit ? level.PitCell() : level.RandomRespawnCell());
    }

    private static void Ascend()
    {
        Actor.FixTime();

        Dungeon.SaveLevel();
        Dungeon.Depth--;
        var level = Dungeon.LoadLevel();
        Dungeon.SwitchLevel(level, level.Exit);
    }
}