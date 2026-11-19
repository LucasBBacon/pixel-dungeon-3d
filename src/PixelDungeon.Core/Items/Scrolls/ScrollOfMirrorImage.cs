namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfMirrorImage : Scroll
{
    public ScrollOfMirrorImage()
    {
        NameValue = "Scroll of Mirror Image";
    }

    protected override void DoRead()
    {
        // TODO: 3x images on free neighbouring cells
        SetKnown();
        ReadAnimation();
    }

    public override string Desc() => 
        "The incantation on this scroll will create illusionary twins of the reader, which will chase his enemies.";
}