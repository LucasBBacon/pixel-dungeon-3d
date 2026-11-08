using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items.Potions;

public class Potion : Item
{
    public const string AcDrink = "DRINK";

    private const string TxtHarmful = "Harmful potion!";
    private const string TxtBeneficial = "Beneficial potion";
    private const string TxtYes = "Yes, I know what I'm doing";
    private const string TxtNo = "No, I changed my mind";

    private const string TxtRUSureDrink =
        "Are you sure you want to drink it? In most cases you should throw such potions at your enemies.";

    private const string TxtRUSureThrow =
        "Are you sure you want to throw it? In most cases it makes sense to drink it.";

    private const float TimeToDrink = 1f;

    private static readonly Type[] Potions =
    [
        typeof(PotionOfHealing),
        typeof(PotionOfExperience),
        typeof(PotionOfToxicGas),
        typeof(PotionOfLiquidFlame),
        typeof(PotionOfStrength),
        typeof(PotionOfParalyticGas),
        typeof(PotionOfLevitation),
        typeof(PotionOfMindVision),
        typeof(PotionOfPurity),
        typeof(PotionOfInvisibility),
        typeof(PotionOfMight),
        typeof(PotionOfFrost)
    ];

    private static readonly string[] Colors =
    [
        "turquoise",
        "crimson",
        "azure",
        "jade",
        "golden",
        "magenta",
        "charcoal",
        "ivory",
        "amber",
        "bistre",
        "indigo",
        "silver"
    ];

    private static readonly int[] Images =
    [
        ItemSpriteSheet.PotionTurquoise,
        ItemSpriteSheet.PotionCrimson,
        ItemSpriteSheet.PotionAzure,
        ItemSpriteSheet.PotionJade,
        ItemSpriteSheet.PotionGolden,
        ItemSpriteSheet.PotionMagenta,
        ItemSpriteSheet.PotionCharcoal,
        ItemSpriteSheet.PotionIvory,
        ItemSpriteSheet.PotionAmber,
        ItemSpriteSheet.PotionBistre,
        ItemSpriteSheet.PotionIndigo,
        ItemSpriteSheet.PotionSilver
    ];

    private static ItemStatusHandler<Potion> _handler;

    private readonly string _color;

    public Potion()
    {
        Stackable = true;
        DefaultAction = AcDrink;

        ImageValue = _handler.Image(this);
        _color = _handler.Label(this);
    }

    public static void InitColors()
    {
        _handler = new ItemStatusHandler<Potion>(Potions, Colors, Images);
    }

    public static void Save(Bundle bundle)
    {
        _handler.Save(bundle);
    }

    public static void Restore(Bundle bundle)
    {
        _handler = new ItemStatusHandler<Potion>(Potions, Colors, Images, bundle);
    }

    public override List<string> Actions(Hero hero)
    {
        List<string> actions = base.Actions(hero);
        actions.Add(AcDrink);
        return actions;
    }

    public override void Execute(Hero hero, string action)
    {
        if (action == AcDrink)
        {
            if (IsKnown() && (
                    this is PotionOfLiquidFlame ||
                    this is PotionOfToxicGas ||
                    this is PotionOfParalyticGas))
            {
                GameScene.Show(new WindowRequest(TxtHarmful,
                    TxtRUSureDrink,
                    [TxtYes, TxtNo],
                    index =>
                    {
                        if (index == 0)
                        {
                            Drink(hero);
                        }
                    }));
            }
            else
            {
                Drink(hero);
            }
        }
        else
        {
            base.Execute(hero, action);
        }
    }

    public override void DoThrow(Hero hero)
    {
        if (IsKnown() && (
                this is PotionOfExperience ||
                this is PotionOfHealing ||
                this is PotionOfLevitation ||
                this is PotionOfMindVision ||
                this is PotionOfStrength ||
                this is PotionOfInvisibility ||
                this is PotionOfMight))
        {
            GameScene.Show(new WindowRequest(TxtBeneficial,
                TxtRUSureThrow,
                [TxtYes, TxtNo],
                index =>
                {
                    if (index == 0)
                    {
                        ThrowPlainly(hero);
                    }
                }));
        }
        else
        {
            base.DoThrow(hero);
        }
    }

    private void ThrowPlainly(Hero hero)
    {
        base.DoThrow(hero);
    }

    protected void Drink(Hero hero)
    {
        Detach(hero.Belongings.Backpack);

        hero.Spend(TimeToDrink);
        hero.Busy();
        OnThrow(hero.Pos);

        Sample.Play(Assets.SndDrink);

        hero.Sprite.Operate(hero.Pos);
    }

    protected override void OnThrow(int cell)
    {
        if (Dungeon.Hero.Pos == cell)
        {
            Apply(Dungeon.Hero);
        }
        else if (Dungeon.Level.Map[cell] == Terrain.Well || Levels.Level.Pit[cell])
        {
            base.OnThrow(cell);
        }
        else
        {
            Shatter(cell);
        }
    }

    protected virtual void Apply(Hero hero)
    {
        Shatter(hero.Pos);
    }

    public virtual void Shatter(int cell)
    {
        if (Dungeon.Visible[cell])
        {
            GLog.I("The flask shatters and " + Color() + " liquid splashes harmlessly");
            Sample.Play(Assets.SndShatter);
            Splash(cell);
        }
    }

    public bool IsKnown()
    {
        return _handler.IsKnown(this);
    }

    public void SetKnown()
    {
        if (!IsKnown())
        {
            _handler.Know(this);
        }

        // TODO: Badges.validateAllPotionsIdentified()
    }

    public override Item Identify()
    {
        SetKnown();
        return this;
    }

    protected string Color()
    {
        return _color;
    }

    public override string Name()
    {
        return IsKnown() ? NameValue : _color + " potion";
    }

    public override string Info()
    {
        return IsKnown()
            ? Desc()
            : "This flask contains a swirling " + _color + " liquid. " +
              "Who knows what it will do when drunk or thrown?";
    }

    public override bool IsIdentified()
    {
        return IsKnown();
    }

    public override bool IsUpgradable()
    {
        return false;
    }

    public static List<Type> GetKnown()
    {
        return _handler.Known();
    }

    public static List<Type> GetUnknown()
    {
        return _handler.Unknown();
    }

    public static bool AllKnown()
    {
        return _handler.Known().Count == Potions.Length;
    }

    protected void Splash(int cell)
    {
        GameScene.Splash(cell, SplashColor(ImageValue), 5);
    }

    public static uint SplashColor(int image)
    {
        return image switch
        {
            ItemSpriteSheet.PotionTurquoise => 0xFF40E0D0,
            ItemSpriteSheet.PotionCrimson => 0xFFDC143C,
            ItemSpriteSheet.PotionAzure => 0xFF007FFF,
            ItemSpriteSheet.PotionJade => 0xFF00A86B,
            ItemSpriteSheet.PotionGolden => 0xFFFFD700,
            ItemSpriteSheet.PotionMagenta => 0xFFFF00FF,
            ItemSpriteSheet.PotionCharcoal => 0xFF36454F,
            ItemSpriteSheet.PotionIvory => 0xFFFFFFF0,
            ItemSpriteSheet.PotionAmber => 0xFFFFBF00,
            ItemSpriteSheet.PotionBistre => 0xFF3D2B1F,
            ItemSpriteSheet.PotionIndigo => 0xFF4B0082,
            ItemSpriteSheet.PotionSilver => 0xFFC0C0C0,
            _ => 0xFFFFFFFF,
        };
    }

    public override int Price()
    {
        return 20 * QuantityValue;
    }
}