using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Armor;

public class ScaleArmor : Armor
{
    public ScaleArmor() : base(4)
    {
        NameValue = "scale armor";
        ImageValue = ItemSpriteSheet.ArmorScale;
    }

    public override string Desc() =>
        "The metal scales sewn onto a leather vest create a flexible, yet protective armor.";
}