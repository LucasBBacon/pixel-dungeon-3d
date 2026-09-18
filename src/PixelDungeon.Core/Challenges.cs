namespace PixelDungeon.Core;

public class Challenges
{
    public const int NoFood = 1;
    public const int NoArmor = 2;
    public const int NoHealing = 4;
    public const int NoHerbalism = 8;
    public const int SwarmIntelligence = 16;
    public const int Darkness = 32;
    public const int NoScrolls = 64;

    public static readonly string[] Names =
    [
        "On diet",
        "Faith is my armor",
        "Pharmacophobia",
        "Barren land",
        "Swarm intelligence",
        "Into darkness",
        "Forbidden runes"
    ];

    public static readonly int[] Masks =
    [
        NoFood,
        NoArmor,
        NoHealing,
        NoHerbalism,
        SwarmIntelligence,
        Darkness,
        NoScrolls
    ];
}