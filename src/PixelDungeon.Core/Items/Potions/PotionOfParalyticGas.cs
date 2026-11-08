using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfParalyticGas : Potion
{
    public PotionOfParalyticGas()
    {
        NameValue = "Potion of Paralytic Gas";
    }

    public override void Shatter(int cell)
    {
        if (Dungeon.Visible[cell])
        {
            SetKnown();

            Splash(cell);
            Sample.Play(Assets.SndShatter);
        }

        GameScene.Add(Blob.Seed<ParalyticGas>(cell, 1000));
    }

    public override string Desc() =>
        "Upon exposure to open air, the liquid in this flask will vaporize " +
        "into a numbing yellow haze. Anyone who inhales the cloud will be paralyzed " +
        "instantly, unable to move for some time after the cloud dissipates. This " +
        "item can be thrown at distant enemies to catch them within the effect of the gas.";

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}