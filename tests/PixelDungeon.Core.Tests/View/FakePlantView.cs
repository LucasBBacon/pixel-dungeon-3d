using PixelDungeon.Core.Plants;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public sealed class FakePlantView : IPlantView
{
    public int Resets;

    public int Kills;
    
    public void Reset(Plant plant) => Resets++;

    public void Kill() => Kills++;
}