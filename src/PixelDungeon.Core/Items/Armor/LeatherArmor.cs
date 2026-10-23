using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Armor;

public class LeatherArmor : Armor
{
    public LeatherArmor() : base(2)
    {
        NameValue = "leather armor";
        ImageValue = ItemSpriteSheet.ArmorLeather;
    }

    public override string Desc() =>
        "Armor made from tanned monster hide. Not as light as cloth armor but provides better protection.";
}