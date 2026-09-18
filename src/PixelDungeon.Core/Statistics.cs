using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core;

public static class Statistics
{
    public static int GoldCollected;
    public static int DeepestFloor;
    public static int EnemiesSlain;
    public static int FoodEaten;
    public static int PotionsCooked;
    public static int PiranhasKilled;
    public static int NightHunt;
    public static int AnkhsUsed;
    public static float Duration;
    public static bool QualifiedForNoKilling = false;
    public static bool CompletedWithNoKilling = false;
    public static bool AmuletObtained = false;

    public static void Reset()
    {
        GoldCollected = 0;
        DeepestFloor = 0;
        EnemiesSlain = 0;
        FoodEaten = 0;
        PotionsCooked = 0;
        PiranhasKilled = 0;
        NightHunt = 0;
        AnkhsUsed = 0;
        Duration = 0;
        QualifiedForNoKilling = false;
        AmuletObtained = false;
    }

    private const string Gold = "score";
    private const string Deepest = "maxDepth";
    private const string Slain = "enemiesSlain";
    private const string Food = "foodEaten";
    private const string Alchemy = "potionsCooked";
    private const string Piranhas = "piranhas";
    private const string Night = "nightHunt";
    private const string Ankhs = "ankhsUsed";
    private const string DurationConst = "duration";
    private const string Amulet = "amuletObtained";

    public static void StoreInBundle(Bundle bundle)
    {
        bundle.Put(Gold, GoldCollected);
        bundle.Put(Deepest, DeepestFloor);
        bundle.Put(Slain, EnemiesSlain);
        bundle.Put(Food, FoodEaten);
        bundle.Put(Alchemy, PotionsCooked);
        bundle.Put(Piranhas, PiranhasKilled);
        bundle.Put(Night, NightHunt);
        bundle.Put(Ankhs, AnkhsUsed);
        bundle.Put(DurationConst, Duration);
        bundle.Put(Amulet, AmuletObtained);
    }

    public static void RestoreFromBundle(Bundle bundle)
    {
        GoldCollected = bundle.GetInt(Gold);
        DeepestFloor = bundle.GetInt(Deepest);
        EnemiesSlain = bundle.GetInt(Slain);
        FoodEaten = bundle.GetInt(Food);
        PotionsCooked = bundle.GetInt(Piranhas);
        PiranhasKilled = bundle.GetInt(Night);
        NightHunt = bundle.GetInt(Night);
        AnkhsUsed = bundle.GetInt(Ankhs);
        Duration = bundle.GetFloat(DurationConst);
        AmuletObtained = bundle.GetBoolean(Amulet);
    }
}