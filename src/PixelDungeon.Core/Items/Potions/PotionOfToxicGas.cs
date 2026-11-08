using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfToxicGas : Potion
{
    public PotionOfToxicGas()
    {
        NameValue = "Potion of Toxic Gas";
    }

    public override void Shatter(int cell)
    {
        if (Dungeon.Visible[cell])
        {
            SetKnown();

            Splash(cell);
            Sample.Play(Assets.SndShatter);
        }

        GameScene.Add(Blob.Seed<ToxicGas>(cell, 1000));
    }

    public override string Desc() =>
        "Uncorking or shattering this pressurized glass will cause " +
        "its contents to explode into a deadly cloud of toxic green gas. " +
        "You might choose to fling this potion at distant enemies " +
        "instead of uncorking it by hand.";

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}