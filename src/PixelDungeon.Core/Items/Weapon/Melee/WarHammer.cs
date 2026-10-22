using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class WarHammer : MeleeWeapon
{
    public WarHammer() : base(5, 1.2f, 1f)
    {
        NameValue = "war hammer";
        ImageValue = ItemSpriteSheet.WarHammer;
    }

    public override string Desc() =>
        "Few creatures can withstand the crushing blow of this towering mass of lead and steel, " +
        "but only the strongest of adventurers can use it effectively.";
}