namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfTerror : Scroll
{
    public ScrollOfTerror()
    {
        NameValue = "Scroll of Terror";
    }

    protected override void DoRead()
    {
        // TODO: red flare, SndRead + dispel, Terror on every mob in view
        SetKnown();
        ReadAnimation();
    }

    public override string Desc() =>
        "A flash of red light will overwhelm all creatures in your field of view with terror, " +
        "and they will turn and flee. Attacking a fleeing enemy will dispel the effect.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}