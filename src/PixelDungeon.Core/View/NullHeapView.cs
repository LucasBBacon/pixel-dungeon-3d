using PixelDungeon.Core.Items;

namespace PixelDungeon.Core.View;

public class NullHeapView : IHeapView
{
    public static readonly NullHeapView Instance = new();

    public void Link(Heap heap)
    {
    }

    public void Link()
    {
    }

    public void Place(int cell)
    {
    }

    public void Drop()
    {
    }

    public void Drop(int from)
    {
    }

    public void View(int image, Glowing glowing)
    {
    }

    public void Kill()
    {
    }
}