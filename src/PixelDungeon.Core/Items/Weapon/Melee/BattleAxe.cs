using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class BattleAxe : MeleeWeapon
{
    public BattleAxe() : base(4, 1.2f, 1f)
    {
        NameValue = "battle axe";
        ImageValue = ItemSpriteSheet.BattleAxe;
    }

    public override string Desc() =>
        "The enormous steel head of this battle axe puts considerable heft behind each stroke.";
}