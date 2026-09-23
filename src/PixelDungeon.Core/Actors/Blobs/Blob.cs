namespace PixelDungeon.Core.Actors.Blobs;

public class Blob : Actor
{
    protected override bool Act()
    {
        Diactivate();
        return true;
    }
}