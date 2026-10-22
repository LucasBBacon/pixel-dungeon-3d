using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Longsword : MeleeWeapon
{
    public Longsword() : base(4, 1f, 1f)
    {
        NameValue = "longsword";
        ImageValue = ItemSpriteSheet.LongSword;
    }

    public override string Desc() => "This towering blade inflicts heavy damage by investing its heft into every cut.";
}