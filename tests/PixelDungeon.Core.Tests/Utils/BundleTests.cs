using System.Text;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class BundleTests
{
    [Fact]
    public void Primitives_RoundTrip()
    {
        var b = new Bundle();
        b.Put("flag", true);
        b.Put("count", 42);
        b.Put("ratio", 1.5f);
        b.Put("name", "rat");

        Assert.True(b.GetBoolean("flag"));
        Assert.Equal(42, b.GetInt("count"));
        Assert.Equal(1.5f, b.GetFloat("ratio"));
        Assert.Equal("rat", b.GetString("name"));
    }

    [Fact]
    public void Put_NonFiniteFloat_IsDropped()
    {
        var b = new Bundle();
        b.Put("nan", float.NaN);
        b.Put("inf", float.PositiveInfinity);
        b.Put("ok", 1.5f);

        Assert.False(b.Contains("nan"));
        Assert.False(b.Contains("inf"));
        Assert.Equal(1.5f, b.GetFloat("ok"));

        using var stream = new MemoryStream();
        Assert.True(Bundle.Write(b, stream));
    }

    [Fact]
    public void MissingKeys_ReturnJavaDefaults()
    {
        var b = new Bundle();
        Assert.False(b.GetBoolean("missing"));
        Assert.Equal(0, b.GetInt("missing"));
        Assert.Equal(0f, b.GetFloat("missing"));
        Assert.Equal("", b.GetString("missing"));
        Assert.True(b.GetBundle("missing").IsNull);
        Assert.Null(b.GetIntArray("missing"));
        Assert.Null(b.GetBooleanArray("missing"));
        Assert.Null(b.GetStringArray("missing"));
        Assert.False(b.Contains("missing"));
    }

    [Fact]
    public void MistypedKeys_ReturnFallbacks()
    {
        var b = new Bundle();
        b.Put("text", "abc");
        b.Put("number", 7);
        Assert.Equal(0, b.GetInt("text"));
        Assert.False(b.GetBoolean("text"));
        Assert.Equal("", b.GetString("number"));
        Assert.Null(b.GetIntArray("number"));
    }

    [Fact]
    public void IntStoredAsFloat_ReadsBackAsFloat()
    {
        var b = new Bundle();
        b.Put("n", 7);
        Assert.Equal(7f, b.GetFloat("n"));
    }

    [Fact]
    public void Arrays_RoundTrip()
    {
        var b = new Bundle();
        b.Put("ints", [1, 2, 3]);
        b.Put("bools", [true, false]);
        b.Put("strings", ["a", "b"]);

        Assert.Equal(new[] { 1, 2, 3 }, b.GetIntArray("ints"));
        Assert.Equal(new[] { true, false }, b.GetBooleanArray("bools"));
        Assert.Equal(new[] { "a", "b" }, b.GetStringArray("strings"));
    }

    [Fact]
    public void NestedBundle_RoundTrip()
    {
        var inner = new Bundle();
        inner.Put("depth", 3);
        var outer = new Bundle();
        outer.Put("level", inner);

        var back = outer.GetBundle("level");
        Assert.False(back.IsNull);
        Assert.Equal(3, back.GetInt("depth"));
    }

    [Fact]
    public void SameBundle_CanBePutTwice()
    {
        var inner = new Bundle();
        inner.Put("x", 1);
        var outer = new Bundle();
        outer.Put("a", inner);
        outer.Put("b", inner);
        Assert.Equal(1, outer.GetBundle("a").GetInt("x"));
        Assert.Equal(1, outer.GetBundle("b").GetInt("x"));
    }

    [Fact]
    public void ContainsAndFields_ReflectStoredKeys()
    {
        var b = new Bundle();
        b.Put("one", 1);
        b.Put("two", 2);
        Assert.True(b.Contains("one"));
        Assert.Equal(new[] { "one", "two" }, b.Fields());
    }

    [Fact]
    public void WriteThenRead_RoundTripsThroughStream()
    {
        var b = new Bundle();
        b.Put("depth", 7);
        b.Put("name", "goo");
        b.Put("ints", new[] { 4, 5 });

        using var stream = new MemoryStream();
        Assert.True(Bundle.Write(b, stream));
        stream.Position = 0;

        var back = Bundle.Read(stream);
        Assert.NotNull(back);
        Assert.Equal(7, back.GetInt("depth"));
        Assert.Equal("goo", back.GetString("name"));
        Assert.Equal(new[] { 4, 5 }, back.GetIntArray("ints"));
    }

    [Fact]
    public void Read_Bytes_ParseJson()
    {
        var b = Bundle.Read(Encoding.UTF8.GetBytes("{\"depth\": 9, \"name\": \"tengu\"}"));
        Assert.Equal(9, b.GetInt("depth"));
        Assert.Equal("tengu", b.GetString("name"));
    }

    [Fact]
    public void Read_MalformedOrNonObject_ReturnsNull()
    {
        Assert.Null(Bundle.Read(Encoding.UTF8.GetBytes("not json")));
        Assert.Null(Bundle.Read(Encoding.UTF8.GetBytes("[1, 2]")));
    }

    [Fact]
    public void Write_ClosedStream_ReturnsFalse()
    {
        var stream = new MemoryStream();
        stream.Dispose();
        Assert.False(Bundle.Write(new Bundle(), stream));
    }

    [Fact]
    public void Read_ClosedOrNullStream_ReturnsNull()
    {
        var stream = new MemoryStream();
        stream.Dispose();
        Assert.Null(Bundle.Read(stream));
        Assert.Null(Bundle.Read((Stream)null));
    }

    [Fact]
    public void ToString_IsCompactJson()
    {
        var b = new Bundle();
        b.Put("a", 1);
        Assert.Equal("{\"a\":1}", b.ToString());
    }

    [Fact]
    public void Bundlable_RoundTripsAsConcreteType()
    {
        var b = new Bundle();
        b.Put("thing", new SampleThing { Depth = 4, Name = "crab" });

        var back = b.Get("thing");
        var thing = Assert.IsType<SampleThing>(back);
        Assert.Equal(4, thing.Depth);
        Assert.Equal("crab", thing.Name);
        Assert.Equal(
            typeof(SampleThing).FullName,
            b.GetBundle("thing").GetString("__className")
        );
    }

    [Fact]
    public void Bundlable_Null_IsNotStored()
    {
        var b = new Bundle();
        b.Put("thing", (IBundlable)null);
        Assert.False(b.Contains("thing"));
        Assert.Null(b.Get("thing"));
    }

    [Fact]
    public void Get_UnknownClassName_ReturnsNull()
    {
        var inner = new Bundle();
        inner.Put("__className", "No.Such.Type");
        var b = new Bundle();
        b.Put("thing", inner);
        Assert.Null(b.Get("thing"));
        Assert.Null(b.Get("missing"));
    }

    [Fact]
    public void Collection_RoundTripMixedTypesInOrder()
    {
        var b = new Bundle();
        b.Put("things", new List<IBundlable>
        {
            new SampleThing { Depth = 1, Name = "a" },
            new OtherThing { Flag = true },
            new SampleThing { Depth = 2, Name = "b" }
        });

        var back = b.GetCollection("things");
        Assert.Equal(3, back.Count);
        Assert.Equal("a", Assert.IsType<SampleThing>(back[0]).Name);
        Assert.True(Assert.IsType<OtherThing>(back[1]).Flag);
        Assert.Equal(2, Assert.IsType<SampleThing>(back[2]).Depth);
    }

    [Fact]
    public void Collection_Missing_ReturnsEmptyList()
    {
        Assert.Empty(new Bundle().GetCollection("missing"));
    }

    [Fact]
    public void Collection_AcceptsTypedCollections()
    {
        var b = new Bundle();
        b.Put("set", new HashSet<SampleThing>
        {
            new()
            {
                Depth = 9,
                Name = "s"
            }
        });
        Assert.Equal(9, Assert.IsType<SampleThing>(b.GetCollection("set")[0]).Depth);
    }

    [Fact]
    public void Enum_RoundTripsByName()
    {
        var b = new Bundle();
        b.Put("feeling", Feeling.Water);
        Assert.Equal("Water", b.GetString("feeling"));
        Assert.Equal(Feeling.Water, b.GetEnum<Feeling>("feeling"));
    }

    [Fact]
    public void Enum_BadOrMissingValue_ReturnsFirstConstant()
    {
        var b = new Bundle();
        b.Put("feeling", "Lava");
        b.Put("numeric", "99");
        Assert.Equal(Feeling.None, b.GetEnum<Feeling>("feeling"));
        Assert.Equal(Feeling.None, b.GetEnum<Feeling>("numeric"));
        Assert.Equal(Feeling.None, b.GetEnum<Feeling>("absent"));
    }

    [Fact]
    public void AddAlias_ResolvesOldClassName()
    {
        Bundle.AddAlias(typeof(SampleThing), "com.watabou.pixeldungeon.OldSample");
        var inner = new Bundle();
        inner.Put("__className", "com.watabou.pixeldungeon.OldSample");
        inner.Put("depth", 6);
        var b = new Bundle();
        b.Put("thing", inner);

        Assert.Equal(6, Assert.IsType<SampleThing>(b.Get("thing")).Depth);
    }

    [Fact]
    public void Bundlable_SurvivesStreamRoundTrip()
    {
        var b = new Bundle();
        b.Put("thing", new OtherThing { Flag = true });
        using var stream = new MemoryStream();
        Bundle.Write(b, stream);
        stream.Position = 0;
        Assert.True(Assert.IsType<OtherThing>(Bundle.Read(stream).Get("thing")).Flag);
    }
}