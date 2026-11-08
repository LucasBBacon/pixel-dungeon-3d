using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Potions;

public class PotionOfPurity : Potion
{
    private const string TxtFreshness = "You feel uncommon freshness in the air.";
    private const string TxtNoSmell = "You've stopped sensing any smells!";

    private const int Distance = 2;

    public PotionOfPurity()
    {
        NameValue = "Potion of Purification";
    }

    public override void Shatter(int cell)
    {
        PathFinder.BuildDistanceMap(cell, BArray.Not(Levels.Level.LosBlocking, null), Distance);

        var procd = false;

        Blob[] blobs =
        [
            Dungeon.Level.Blobs.GetValueOrDefault(typeof(ToxicGas)),
            Dungeon.Level.Blobs.GetValueOrDefault(typeof(ParalyticGas))
        ];

        for (var j = 0; j < blobs.Length; j++)
        {
            var blob = blobs[j];
            if (blob == null)
            {
                continue;
            }

            for (var i = 0; i < Levels.Level.Length; i++)
            {
                if (PathFinder.Distance[i] < int.MaxValue)
                {
                    var value = blob.Cur[i];
                    if (value > 0)
                    {
                        blob.Cur[i] = 0;
                        blob.Volume -= value;
                        procd = true;

                        if (Dungeon.Visible[i])
                        {
                            GameScene.Effect(EffectKind.Discover, i);
                        }
                    }
                }
            }
        }

        var heroAffected = PathFinder.Distance[Dungeon.Hero.Pos] < int.MaxValue;

        if (procd)
        {
            if (Dungeon.Visible[cell])
            {
                Splash(cell);
                Sample.Play(Assets.SndShatter);
            }

            SetKnown();

            if (heroAffected)
            {
                GLog.P(TxtFreshness);
            }
        }
        else
        {
            base.Shatter(cell);

            if (heroAffected)
            {
                GLog.I(TxtFreshness);
                SetKnown();
            }
        }
    }

    protected override void Apply(Hero hero)
    {
        GLog.W(TxtNoSmell);
        Buff.Prolong<GasesImmunity>(hero, GasesImmunity.Duration);
        SetKnown();
    }

    public override string Desc() =>
        "This reagent will quickly neutralize all harmful gases in the area of effect. " +
        "Drinking it will give you a temporary immunity to such gases.";

    public override int Price() => IsKnown() ? 50 * QuantityValue : base.Price();
}