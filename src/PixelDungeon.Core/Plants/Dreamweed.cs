using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Plants;

public class Dreamweed : Plant
{
    private const string TxtDesc =
        "Upon touching a Dreamweed it secretes a glittering cloud of confusing gas.";

    public Dreamweed()
    {
        Image = 3;
        PlantName = "Dreamweed";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        if (ch != null)
        {
            GameScene.Add(Blob.Seed<ConfusionGas>(Pos, 400));
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Dreamweed";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedDreamweed;

            PlantClass = typeof(Dreamweed);
            AlchemyClass = typeof(PotionOfInvisibility);
        }

        public override string Desc() => TxtDesc;
    }
}