using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class HeapTest
{
    [Fact]
    public void Drop_IgnoresNull_AndPutsItemsFirst()
    {
        var heap = new Heap();
        heap.Drop(null);
        Assert.Equal(0, heap.Size());
        Assert.Null(heap.Peek());
        var first = new Item { Name = "first" };
        var second = new Item { Name = "second" };
        heap.Drop(first);
        heap.Drop(second);
        Assert.Equal(2, heap.Size());
        Assert.Same(second, heap.Peek());
    }

    [Fact]
    public void Bundle_RoundTripPosAndType()
    {
        var heap = new Heap { Pos = 77, Type = HeapType.Chest };
        var bundle = new Bundle();
        bundle.Put("heap", heap);
        var restored = (Heap)bundle.Get("heap");
        Assert.Equal(77, restored.Pos);
        Assert.Equal(HeapType.Chest, restored.Type);
        Assert.Equal(0, restored.Size());
    }

    [Fact]
    public void Generator_SkeletonReturnsNull()
    {
        Assert.Null(Generator.Random());
        Assert.Null(Generator.Random(Generator.Category.Food));
    }
}