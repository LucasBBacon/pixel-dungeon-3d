using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public sealed class SampleThing : IBundlable
{
    public int Depth;
    public string Name;

    public void RestoreFromBundle(Bundle bundle)
    {
        Depth = bundle.GetInt("depth");
        Name = bundle.GetString("name");
    }

    public void StoreInBundle(Bundle bundle)
    {
        bundle.Put("depth", Depth);
        bundle.Put("name", Name);
    }
}

public sealed class OtherThing : IBundlable
{
    public bool Flag;

    public void RestoreFromBundle(Bundle bundle)
    {
        Flag = bundle.GetBoolean("flag");
    }

    public void StoreInBundle(Bundle bundle)
    {
        bundle.Put("flag", Flag);
    }
}

public enum Feeling
{
    None,
    Chasm,
    Water,
    Grass
}