namespace PixelDungeon.Core.Items.Potions;

public class PotionOfFrost : Potion
{
    // TODO: DISTANCE = 2, reach of the freezing cloud

    public PotionOfFrost()
    {
        NameValue = "Potion of Frost";
    }

    public override void Shatter(int cell)
    {
        // TODO: Freezing.affect(i, fire)
    }

    public override string Desc() =>
        "Upon exposure to open air, this chemical will evaporate into a freezing cloud, causing " +
        "any creature that contacts it to be frozen in place, unable to act and move.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}