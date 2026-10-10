using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Bags;

public class Keyring : Bag
{
    public Keyring()
    {
        NameValue = "key ring";
        ImageValue = ItemSpriteSheet.Keyring;

        Size = 12;
    }

    public override bool Grab(Item item) => item is Key;

    public override int Price() => 50;

    public override string Info() =>
        "This is a copper key ring, that lets you keep all your keys " +
        "separately from the rest of your belongings.";
}