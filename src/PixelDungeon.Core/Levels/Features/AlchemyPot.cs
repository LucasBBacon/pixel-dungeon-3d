using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Levels.Features;

public static class AlchemyPot
{
    public const string TxtSelectSeed = "Select a seed to throw";

    private static Hero _hero;
    private static int _pos;

    public static void Operate(Hero hero, int pos)
    {
        _hero = hero;
        _pos = pos;

        GameScene.SelectItem(ItemSelector, BagMode.Seed, TxtSelectSeed);
    }

    private static void ItemSelector(Item item) => item?.Cast(_hero, _pos);
}