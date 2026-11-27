using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Plants;

public class Icecap : Plant
{
    private const string TxtDesc =
        "Upon touching an Icecap excretes a pollen, which freezes everything in its vicinity.";

    public Icecap()
    {
        Image = 1;
        PlantName = "Icecap";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        PathFinder.BuildDistanceMap(Pos, BArray.Not(Level.LosBlocking, null), 1);

        var fire = Dungeon.Level.Blobs.GetValueOrDefault(typeof(Fire)) as Fire;

        for (var i = 0; i < Level.Length; i++)
        {
            if (PathFinder.Distance[i] < int.MaxValue)
            {
                Freezing.Affect(i, fire);
            }
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Icecap";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedIcecap;

            PlantClass = typeof(Icecap);
            AlchemyClass = typeof(PotionOfFrost);
        }

        public override string Desc() => TxtDesc;
    }
}