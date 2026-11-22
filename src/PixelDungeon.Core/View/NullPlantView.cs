using PixelDungeon.Core.Plants;

namespace PixelDungeon.Core.View;

public sealed class NullPlantView : IPlantView
{
    public static readonly NullPlantView Instance = new();

    public void Reset(Plant plant)
    {
    }

    public void Kill()
    {
    }
}