using PixelDungeon.Core.Items;

namespace PixelDungeon.Core.View;

public interface IHeapView
{
    void Link(Heap heap); // read Image() and Glowing(), place at heap.Pos
    void Link(); // relink the heap already linked, as Heap.OPen do
    void Place(int cell);
    void Drop(); // hop in place, nothing when the heap is empty
    void Drop(int from); // arc in from another cell
    void View(int image, Glowing glowing);
    void Kill();
}