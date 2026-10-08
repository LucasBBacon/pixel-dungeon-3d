using PixelDungeon.Core.Items;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class FakeHeapView : IHeapView
{
    public readonly List<Heap> Links = [];
    public int Relinks;
    public readonly List<int> Placed = [];
    public int Drops;
    public readonly List<int> DropsFrom = [];
    public readonly List<(int Image, Glowing Glowing)> Views = [];
    public bool Killed;

    public void Link(Heap heap) => Links.Add(heap);

    public void Link() => Relinks++;

    public void Place(int cell) => Placed.Add(cell);

    public void Drop() => Drops++;

    public void Drop(int from) => DropsFrom.Add(from);

    public void View(int image, Glowing glowing) => Views.Add((image, glowing));

    public void Kill() => Killed = true;
}