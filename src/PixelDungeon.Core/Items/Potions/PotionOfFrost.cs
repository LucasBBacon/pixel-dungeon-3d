using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfFrost : Potion
{
    private const int Distance = 2;

    public PotionOfFrost()
    {
        NameValue = "Potion of Frost";
    }

    public override void Shatter(int cell)
    {
        PathFinder.BuildDistanceMap(cell, BArray.Not(Levels.Level.LosBlocking, null), Distance);

        var fire = Dungeon.Level.Blobs.GetValueOrDefault(typeof(Fire)) as Fire;
        
        var visible = false;
        for (var i = 0; i < Levels.Level.Length; i++)
        {
            if (PathFinder.Distance[i] < int.MaxValue)
            {
                visible = Freezing.Affect(i, fire) || visible;
            }
        }
        
        if (visible)
        {
            Splash(cell);
            Sample.Play(Assets.SndShatter);

            SetKnown();
        }
    }

    public override string Desc() =>
        "Upon exposure to open air, this chemical will evaporate into a freezing cloud, causing " +
        "any creature that contacts it to be frozen in place, unable to act and move.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}