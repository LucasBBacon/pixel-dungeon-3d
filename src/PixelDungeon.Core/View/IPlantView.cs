using PixelDungeon.Core.Plants;

namespace PixelDungeon.Core.View;

public interface IPlantView
{
    void Reset(Plant plant); // PlantSprite.reset(Plant): place the plant and grow it over 0.2s
    void Kill(); // PlantSprite.kill(): wither over 0.2s, then free view
}