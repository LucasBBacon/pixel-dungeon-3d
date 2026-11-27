using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Plants;

public class Firebloom : Plant
{
    private const string TxtDesc = "When something touches a Firebloom, it bursts into flames.";

    public Firebloom()
    {
        Image = 0;
        PlantName = "Firebloom";
    }

    public override void Activate(Char ch)
    {
        base.Activate(ch);

        GameScene.Add(Blob.Seed<Fire>(Pos, 2));

        if (Dungeon.Visible[Pos])
        {
            GameScene.Splash(Pos, 0xFFEE7722, 5);
        }
    }

    public override string Desc() => TxtDesc;

    public new class Seed : Plant.Seed
    {
        public Seed()
        {
            PlantName = "Firebloom";

            NameValue = "seed of " + PlantName;
            ImageValue = ItemSpriteSheet.SeedFirebloom;

            PlantClass = typeof(Firebloom);
            AlchemyClass = typeof(PotionOfLiquidFlame);
        }

        public override string Desc() => TxtDesc;
    }
}