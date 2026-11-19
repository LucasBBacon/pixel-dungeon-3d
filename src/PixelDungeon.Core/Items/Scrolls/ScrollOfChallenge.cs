namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfChallenge : Scroll
{
    public ScrollOfChallenge()
    {
        NameValue = "Scroll of Challenge";
    }

    protected override void DoRead()
    {
        // TODO: beckon every mob, enrage visible ones, log the roar, scream burst, SndChallenge, dispel before setKnown and read animation
        SetKnown();
        ReadAnimation();
    }

    public override string Desc() =>
        "When read aloud, this scroll will unleash a challenging roar " +
        "that will awaken all monsters and alert them to the reader's location.";
}