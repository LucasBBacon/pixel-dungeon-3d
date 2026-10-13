using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items;

public class DewVial : Item
{
    private const int MaxVolume = 10;

    private const string AcDrink = "DRINK";

    private const float TimeToDrink = 1f;

    private const string TxtValue = "{0:+#;-#;+0}HP";
    private const string TxtStatus = "{0}/{1}";

    private const string TxtAutoDrink = "The dew vial was emptied to heal your wounds.";
    private const string TxtCollected = "You collected a dewdrop into your dew vial.";
    private const string TxtFull = "Your dew vial is full!";
    private const string TxtEmpty = "Your dew vial is empty!";

    public DewVial()
    {
        NameValue = "dew vial";
        ImageValue = ItemSpriteSheet.Vial;

        DefaultAction = AcDrink;

        Unique = true;
    }

    private int _volume = 0;

    private const string TagVolume = "volume";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        _volume = bundle.GetInt(TagVolume);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagVolume, _volume);
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        if (_volume > 0)
        {
            actions.Add(AcDrink);
        }

        return actions;
    }

    private const double Num = 20;

    private static readonly double Pow = Math.Log10(Num);

    public override void Execute(Hero hero, string action)
    {
        if (action == AcDrink)
        {
            if (_volume > 0)
            {
                var value = (int)Math.Ceiling(Math.Pow(_volume, Pow) / Num * hero.HT);
                var effect = Math.Min(hero.HT - hero.HP, value);
                if (effect > 0)
                {
                    hero.HP += effect;
                    hero.Sprite.Burst(0xFF33FF33, _volume > 5 ? 2 : 1); // speck.Healing
                    hero.Sprite.ShowStatus(StatusColor.Positive, TextUtils.Format(TxtValue, effect));
                }

                _volume = 0;

                hero.Spend(TimeToDrink);
                hero.Busy();

                Sample.Play(Assets.SndDrink);
                hero.Sprite.Operate(hero.Pos);

                UpdateQuickslot();
            }
            else
            {
                GLog.W(TxtEmpty);
            }
        }
        else
        {
            base.Execute(hero, action);
        }
    }

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public bool IsFull() => _volume >= MaxVolume;

    public void CollectDew(Dewdrop dewdrop)
    {
        GLog.I(TxtCollected);
        _volume += dewdrop.Quantity();
        if (_volume >= MaxVolume)
        {
            _volume = MaxVolume;
            GLog.P(TxtFull);
        }

        UpdateQuickslot();
    }

    public void Fill()
    {
        _volume = MaxVolume;
        UpdateQuickslot();
    }

    public static void AutoDrink(Hero hero)
    {
        var vial = hero.Belongings.GetItem<DewVial>();
        if (vial != null && vial.IsFull())
        {
            vial.Execute(hero);
            hero.Sprite.Burst(0xFFFFFF66, 3); // ShaftParticle, emitted 3x over 0.2s
            GLog.W(TxtAutoDrink);
        }
    }

    private static readonly Glowing White = new(0xFFFFCC);

    public override Glowing Glowing()
    {
        return IsFull() ? White : null;
    }

    public override string Status()
    {
        return TextUtils.Format(TxtStatus, _volume, MaxVolume);
    }

    public override string Info() =>
        "You can store excess dew in this tiny vessel for drinking it later. " +
        "If the vial is full, in a moment of deadly peril the dew will be " +
        "consumed automatically.";

    public override string ToString()
    {
        return base.ToString() + " (" + Status() + ")";
    }
}