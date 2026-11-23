using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Core.Items.Food;

public class ChargrilledMeat : Food
{
    public ChargrilledMeat()
    {
        NameValue = "chargrilled meat";
        ImageValue = ItemSpriteSheet.Steak;
        Energy = Hunger.Starving - Hunger.Hungry;
    }
    
    public override string Info() => "It looks like a decent steak.";

    public override int Price() => 5 * QuantityValue;

    public static Food Cook(MysteryMeat ingredient)
    {
        var result = new ChargrilledMeat
        {
            QuantityValue = ingredient.Quantity()
        };
        return result;
    }
}