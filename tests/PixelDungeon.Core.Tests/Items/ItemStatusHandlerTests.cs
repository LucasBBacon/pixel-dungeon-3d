using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Items;

public class ItemStatusHandlerTests : DungeonFixture
{
    public class TableItemA : Item
    {
    }

    public class TableItemB : Item
    {
    }

    public class TableItemC : Item
    {
    }

    public class TableItemD : Item
    {
    }

    private static readonly Type[] Classes =
    [
        typeof(TableItemA),
        typeof(TableItemB),
        typeof(TableItemC),
        typeof(TableItemD)
    ];

    private static readonly string[] Labels =
    [
        "KAUNAN",
        "SOWILO",
        "LAGUZ",
        "YNGVI"
    ];

    private static readonly int[] Images = [40, 41, 42, 43];

    private static ItemStatusHandler<Item> NewHandler() => new(Classes, Labels, Images);

    private static Item Instance(Type type) => (Item)Activator.CreateInstance(type);

    [Fact]
    public void Constructor_DrawsOneIndexPerClassInTableOrder_AndTakesTheLabelAndImageAtIt()
    {
        Random.Seed(7);
        var handler = NewHandler();
        var nextDraw = Random.Int(1000);

        // replay ItemStatusHandler.java's loop: one Random.Int(labelsLeft.size()) per class, in order
        Random.Seed(7);
        var labelsLeft = new List<string>(Labels);
        var imagesLeft = new List<int>(Images);
        foreach (var type in Classes)
        {
            var index = Random.Int(labelsLeft.Count);
            Assert.Equal(labelsLeft[index], handler.Label(Instance(type)));
            Assert.Equal(imagesLeft[index], handler.Image(Instance(type)));
            labelsLeft.RemoveAt(index);
            imagesLeft.RemoveAt(index);
        }

        Assert.Equal(Random.Int(1000), nextDraw); // exactly 4 draws
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Construct_AssignsAPermutation_KeepingEachLabelWithItsImage(int seed)
    {
        Random.Seed(seed);
        var handler = NewHandler();
        var items = Classes.Select(Instance).ToArray();

        Assert.Equal(Labels.OrderBy(l => l), items.Select(handler.Label).OrderBy(l => l));
        foreach (var item in items)
        {
            // both lists lose the same index on every draw, so they stay aligned
            Assert.Equal(Images[Array.IndexOf(Labels, handler.Label(item))], handler.Image(item));
        }
    }

    [Fact]
    public void Know_MarksTheWholeClass_NotJustTheInstance()
    {
        var handler = NewHandler();

        handler.Know(new TableItemB());

        Assert.True(handler.IsKnown(new TableItemB()));
        Assert.False(handler.IsKnown(new TableItemA()));
    }

    [Fact]
    public void Know_WithOnlyOneClassLeftUnknown_KnowsThatClassToo()
    {
        var handler = NewHandler();
        handler.Know(new TableItemA());
        handler.Know(new TableItemD());
        Assert.Equal(2, handler.Known().Count);

        handler.Know(new TableItemB());

        Assert.True(handler.IsKnown(new TableItemC()));
        Assert.Equal(Classes, handler.Known());
        Assert.Empty(handler.Unknown());
    }

    [Fact]
    public void KnownAndUnknown_ListClassesInTableOrder()
    {
        var handler = NewHandler();
        handler.Know(new TableItemC());
        handler.Know(new TableItemA());

        Assert.Equal(new[] { typeof(TableItemA), typeof(TableItemC) }, handler.Known());
        Assert.Equal(new[] { typeof(TableItemB), typeof(TableItemD) }, handler.Unknown());
    }

    [Fact]
    public void Save_WritesImageLabelAndKnownForEveryClass_KeyedByTheTypeFullName()
    {
        var handler = NewHandler();
        handler.Know(new TableItemB());
        var bundle = new Bundle();

        handler.Save(bundle);

        foreach (var type in Classes)
        {
            var item = Instance(type);
            Assert.Equal(handler.Label(item), bundle.GetString(type.FullName + "_label"));
            Assert.Equal(handler.Image(item), bundle.GetInt(type.FullName + "_image"));
            Assert.Equal(type == typeof(TableItemB), bundle.GetBoolean(type.FullName + "_known"));
        }
    }

    [Fact]
    public void Restore_FromASave_KeepsLabelsImagesAndKnowledge_WithoutDrawing()
    {
        Random.Seed(3);
        var saved = NewHandler();
        saved.Know(new TableItemD());
        var bundle = new Bundle();
        saved.Save(bundle);

        Random.Seed(11);
        var restored = new ItemStatusHandler<Item>(Classes, Labels, Images, bundle);
        var nextDraw = Random.Int(1000);

        foreach (var type in Classes)
        {
            var item = Instance(type);
            Assert.Equal(saved.Label(item), restored.Label(item));
            Assert.Equal(saved.Image(item), restored.Image(item));
            Assert.Equal(saved.IsKnown(item), restored.IsKnown(item));
        }

        Random.Seed(11);
        Assert.Equal(Random.Int(1000), nextDraw);
    }

    [Fact]
    public void Restore_WithAClassMissingFromTheSave_RollsItFromTheLabelsAndImagesLeft()
    {
        Random.Seed(5);
        var threeClasses = new ItemStatusHandler<Item>([.. Classes.Take(3)], Labels, Images);
        var bundle = new Bundle();
        threeClasses.Save(bundle);
        var savedLabels = Classes.Take(3)
            .Select(t => threeClasses.Label(Instance(t)))
            .ToArray();
        
        var restored =  new ItemStatusHandler<Item>(Classes, Labels, Images, bundle);

        var left = Labels.Except(savedLabels).Single();
        Assert.Equal(left, restored.Label(new TableItemD()));
        Assert.Equal(Images[Array.IndexOf(Labels, left)], restored.Image(new TableItemD()));
        Assert.False(restored.IsKnown(new TableItemD()));
    }
}