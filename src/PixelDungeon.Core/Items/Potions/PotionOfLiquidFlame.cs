using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfLiquidFlame : Potion
{
    public PotionOfLiquidFlame()
    {
        NameValue = "Potion of Liquid Flame";
    }

    public override void Shatter(int cell)
    {
        if (Dungeon.Visible[cell])
        {
            SetKnown();
            
            Splash(cell);
            Sample.Play(Assets.SndShatter);
        }
        
        // TODO: GameScene.add(Blob.seed(cell, 2, Fire.class))
    }

    public override string Desc() =>
        "This flask contains an unstable compound which will burst " +
        "violently into flame upon exposure to open air.";

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}