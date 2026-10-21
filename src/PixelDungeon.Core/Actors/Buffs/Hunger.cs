using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Buffs;

public class Hunger : Buff, Hero.Hero.IDoom
{
    private const float Step = 10f;

    public const float Hungry = 260f;
    public const float Starving = 360f;

    private const string TxtHungry = "You are hungry.";
    private const string TxtStarving = "You are starving!";
    private const string TxtDeath = "You starved to death...";

    private float _level;

    public float HungerLevel => _level;

    private const string TagLevel = "level";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        _level = bundle.GetFloat(TagLevel);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagLevel, _level);
    }

    protected override bool Act()
    {
        if (Target.IsAlive())
        {
            var hero = (Hero.Hero)Target;
            
            if (IsStarving())
            {
                if (Random.Float() < 0.3f && (Target.HP > 1 || !Target.Paralysed))
                {
                    GLog.N(TxtStarving);
                    hero.Damage(1, this);

                    hero.Interrupt();
                }
            }
            else
            {
                var bonus = 0;
                // TODO: each RingOfSatiety.Satiety buff adds level to bonus

                var newLevel = _level + Step - bonus;
                switch (newLevel)
                {
                    case >= Starving:
                        GLog.N(TxtStarving);
                        hero.Interrupt();
                        break;
                    case >= Hungry when _level < Hungry:
                        GLog.W(TxtHungry);
                        break;
                }

                _level = newLevel;
            }

            var step = hero.HeroClass == HeroClass.Rogue ? Step * 1.2f : Step;
            // TODO: Shadows buff on the target stretches step by 1.5
            Spend(step);
        }
        else
        {
            Diactivate();
        }

        return true;
    }

    public void Satisfy(float energy)
    {
        _level -= energy;
        _level = _level switch
        {
            < 0 => 0,
            > Starving => Starving,
            _ => _level
        };

        // BuffIndicator.refreshHero() in java, Godot buff row polls Hero.Buffs() every frame
    }

    public bool IsStarving() => _level >= Starving;

    public override int Icon() =>
        _level switch
        {
            < Hungry => BuffIndicator.None,
            < Starving => BuffIndicator.Hunger,
            _ => BuffIndicator.Starvation
        };

    public override string ToString() => _level < Starving ? "Hungry" : "Starving";

    public void OnDeath()
    {
        // TODO: Badges.validateDeathFromHunger()

        Dungeon.Fail(TextUtils.Format(ResultDescriptions.Hunger, Dungeon.Depth));
        GLog.N(TxtDeath);
    }
}