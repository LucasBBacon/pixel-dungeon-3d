using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Mace : MeleeWeapon
{
    public Mace() : base(3, 1f, 0.8f)
    {
        NameValue = "mace";
        ImageValue = ItemSpriteSheet.Mace;
    }

    public override string Desc() => "The iron head of this weapon inflicts substantial damage.";
}