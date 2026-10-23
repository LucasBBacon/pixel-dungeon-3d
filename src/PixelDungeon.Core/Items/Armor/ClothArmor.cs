using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Armor;

public class ClothArmor : Armor
{
    public ClothArmor() : base(1)
    {
        NameValue = "cloth armor";
        ImageValue = ItemSpriteSheet.ArmorCloth;
    }

    public override string Desc() => "This lightweight armor offers basic protection.";
}