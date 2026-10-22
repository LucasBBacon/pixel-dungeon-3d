using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Spear : MeleeWeapon
{
    public Spear() : base(2, 1f, 1.5f)
    {
        NameValue = "spear";
        ImageValue = ItemSpriteSheet.Spear;
    }

    public override string Desc() => "A slender wooden rod tipped with sharpened iron.";
}