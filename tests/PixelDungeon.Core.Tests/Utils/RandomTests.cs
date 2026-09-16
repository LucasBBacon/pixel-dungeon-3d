namespace PixelDungeon.Core.Tests.Utils;

public class RandomTests
{
    private const int Samples = 10_000;

    public RandomTests()
    {
        Random.Seed(12345);
    }

    [Fact]
    public void Int_NonPositiveMax_ReturnsZero()
    {
        Assert.Equal(0, Random.Int(0));
        Assert.Equal(0, Random.Int(-3));
    }

    [Fact]
    public void Int_Max_StaysBelowMax()
    {
        for (var i = 0; i < Samples; i++)
        {
            Assert.InRange(Random.Int(5), 0, 4);
        }
    }

    [Fact]
    public void Int_MinMax_IsExclusiveOfMax()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < Samples; i++)
        {
            var v = Random.Int(2, 5);
            Assert.InRange(v, 2, 4);
            seen.Add(v);
        }

        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void IntRange_IsInclusiveOfMax()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < Samples; i++)
        {
            var v = Random.IntRange(2, 5);
            Assert.InRange(v, 2, 5);
            seen.Add(v);
        }

        Assert.Contains(5, seen);
        Assert.Equal(2, Random.IntRange(2, 2));
    }

    [Fact]
    public void NormalIntRange_StaysInRangeWithMidpointMean()
    {
        long total = 0;
        for (var i = 0; i < Samples; i++)
        {
            var v = Random.NormalIntRange(0, 10);
            Assert.InRange(v, 0, 10);
            total += v;
        }

        var mean = total / (double)Samples;
        Assert.InRange(mean, 4.8, 5.2);
    }

    [Fact]
    public void Float_StaysWithinRange()
    {
        // upper bounds are inclusive on purpose, casting a double just below 1.0
        // can round up to exactly 1.0f
        for (var i = 0; i < Samples; i++)
        {
            Assert.InRange(Random.Float(), 0f, 1f);
            Assert.InRange(Random.Float(3f), 0f, 3f);
            Assert.InRange(Random.Float(2f, 3f), 2f, 3f);
        }
    }

    [Fact]
    public void Chances_Array_NeverPicksZeroWeight()
    {
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(1, Random.Chances([0f, 1f, 0f]));
        }
    }

    [Fact]
    public void Chances_Array_PicksEachPositiveWeight()
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < 1000; i++)
        {
            seen.Add(Random.Chances([1f, 2f, 3f]));
        }

        Assert.Equal(new HashSet<int> { 0, 1, 2 }, seen);
    }

    [Fact]
    public void Chances_Dictionary_NeverPicksZeroWeight()
    {
        var table = new Dictionary<string, float> { ["a"] = 0f, ["b"] = 1f };
        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal("b", Random.Chances(table));
        }
    }

    [Fact]
    public void Element_EmptyCollection_ReturnsNull()
    {
        Assert.Null(Random.Element(new List<string>()));
    }

    [Fact]
    public void ElementOneOfIndex_PickFromTheGivenItems()
    {
        var items = new[] { "x", "y", "z" };
        for (var i = 0; i < 100; i++)
        {
            Assert.Contains(Random.Element(items), items);
            Assert.Contains(Random.Element(items, 2), new[] { "x", "y" });
            Assert.Contains(Random.Element(new List<string>(items)), items);
            Assert.Contains(Random.OneOf("p", "q"), new[] { "p", "q" });
            Assert.InRange(Random.Index(new List<int> { 1, 2, 3 }), 0, 2);
        }
    }

    [Fact]
    public void Shuffle_IsAPermutation()
    {
        var array = Enumerable.Range(0, 20).ToArray();
        Random.Shuffle(array);
        Assert.Equal(Enumerable.Range(0, 20), array.OrderBy((v => v)));
        Assert.NotEqual(Enumerable.Range(0, 20), array);
    }

    [Fact]
    public void Shuffle_Paired_KeepsPairsAligned()
    {
        var u = Enumerable.Range(0, 20).ToArray();
        var v = Enumerable.Range(0, 20).Select(i => i * 10).ToArray();
        Random.Shuffle(u, v);
        for (var i = 0; i < u.Length; i++)
        {
            Assert.Equal(u[i] * 10, v[i]);
        }
    }

    [Fact]
    public void Seed_SameSeed_SameSequence()
    {
        Random.Seed(42);
        var first = Enumerable.Range(0, 20).Select(_ => Random.Int(1000)).ToArray();
        Random.Seed(42);
        var second = Enumerable.Range(0, 20).Select(_ => Random.Int(1000)).ToArray();
        Assert.Equal(first, second);
    }
}