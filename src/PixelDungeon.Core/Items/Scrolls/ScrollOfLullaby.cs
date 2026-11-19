namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfLullaby : Scroll
{
    public ScrollOfLullaby()
    {
        NameValue = "Scroll of Lullaby";
    }

    protected override void DoRead()
    {
        // TODO: note burst, SndLullaby and dispel, Sleep on every mob in view
        SetKnown();
        ReadAnimation();
    }

    public override string Desc() =>
        "A soothing melody will put all creatures in your field of view into a deep sleep, " +
        "giving you a chance to flee or make a surprise attack on them.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}