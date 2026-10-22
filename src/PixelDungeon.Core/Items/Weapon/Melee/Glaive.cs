using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Glaive : MeleeWeapon
{
    public Glaive() : base(5, 1f, 1f)
    {
        NameValue = "glaive";
        ImageValue = ItemSpriteSheet.Glaive;
    }

    public override string Desc() => "A polearm consisting of a sword blade on the end of a pole.";
}