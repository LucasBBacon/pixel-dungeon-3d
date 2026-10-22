using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Quarterstaff : MeleeWeapon
{
    public Quarterstaff() : base(2, 1f, 1f)
    {
        NameValue = "quarterstaff";
        ImageValue = ItemSpriteSheet.Quarterstaff;
    }

    public override string Desc() => "A staff of hardwood, its ends are shod with iron.";
}