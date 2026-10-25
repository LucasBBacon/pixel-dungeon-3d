using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Food;

public class Pasty : Food
{
    public Pasty()
    {
        NameValue = "pasty";
        ImageValue = ItemSpriteSheet.Pasty;
        Energy = Hunger.Starving;
    }

    public override string Info() => "This is authentic Cornish pasty with traditional filling of beef and potato.";

    public override int Price() => 20 * QuantityValue;
}