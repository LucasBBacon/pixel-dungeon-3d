using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Sword : MeleeWeapon
{
    public Sword() : base(3, 1f, 1f)
    {
        NameValue = "sword";
        ImageValue = ItemSpriteSheet.Sword;
    }

    public override string Desc() => "The razor-sharp length of steel blade shines reassuringly.";
}