using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Weapon.Melee;

public class Dagger : MeleeWeapon
{
    public Dagger() : base(1, 1.2f, 1f)
    {
        NameValue = "dagger";
        ImageValue = ItemSpriteSheet.Dagger;
    }

    public override string Desc() => "A simple iron dagger with a well worn wooden handle.";
}