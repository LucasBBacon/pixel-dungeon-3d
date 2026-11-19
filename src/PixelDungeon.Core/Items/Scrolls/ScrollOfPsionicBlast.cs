namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfPsionicBlast : Scroll
{
    public ScrollOfPsionicBlast()
    {
        NameValue = "Scroll of Psionic Blast";
    }

    protected override void DoRead()
    {
        // TODO: the white flash, SndBlast and dispel, blindness + damage on every mob in view
        SetKnown();
        ReadAnimation();
    }

    public override string Desc() =>
        "This scroll contains destructive energy, that can be psionically channeled to inflict a " +
        "massive damage to all creatures within a field of view. An accompanying flash of light will " +
        "temporarily blind everybody in the area of effect including the reader of the scroll.";

    public override int Price() => IsKnown() ? 80 * QuantityValue : base.Price();
}