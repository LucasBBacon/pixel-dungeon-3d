using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Bags;

public class SeedPouch : Bag
{
    public SeedPouch()
    {
        NameValue = "seed pouch";
        ImageValue = ItemSpriteSheet.Pouch;

        Size = 8;
    }

    public override bool Grab(Item item) => item is Plant.Seed;

    public override int Price() => 50;

    public override string Info() =>
        "This small velvet pouch allows you to store any number of seeds in it. Very convenient.";
}