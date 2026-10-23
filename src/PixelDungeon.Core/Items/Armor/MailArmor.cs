using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Armor;

public class MailArmor : Armor
{
    public MailArmor() : base(3)
    {
        NameValue = "mail armor";
        ImageValue = ItemSpriteSheet.ArmorMail;
    }

    public override string Desc() => "Interlocking metal links make for a tough but flexible suit of armor.";
}