using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Knuckles : MeleeWeapon
{
    public Knuckles() : base(1, 1f, 0.5f)
    {
        NameValue = "knuckleduster";
        ImageValue = ItemSpriteSheet.Knuckleduster;
    }

    public override string Desc() => "A piece of iron shaped to fit around the knuckles.";
}