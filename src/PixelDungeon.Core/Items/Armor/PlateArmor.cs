using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Armor;

public class PlateArmor : Armor
{
    public PlateArmor() : base(5)
    {
        NameValue = "plate armor";
        ImageValue = ItemSpriteSheet.ArmorPlate;
    }

    public override string Desc() =>
        "Enormous plates of metal are joined together into a suit that provides " +
        "unmatched protection to any adventurer strong enough to bear its staggering weight.";
}