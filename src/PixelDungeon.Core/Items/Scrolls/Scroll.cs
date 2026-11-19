using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public abstract class Scroll : Item
{
    private const string TxtBlinded = "You can't read a scroll while blinded";
    
    public const string AcRead = "READ";

    protected const float TimeToRead = 1f;
    
    private static readonly Type[] Scrolls =
    [
        typeof(ScrollOfIdentify),
        typeof(ScrollOfMagicMapping),
        typeof(ScrollOfRecharging),
        typeof(ScrollOfRemoveCurse),
        typeof(ScrollOfTeleportation),
        typeof(ScrollOfChallenge),
        typeof(ScrollOfTerror),
        typeof(ScrollOfLullaby),
        typeof(ScrollOfPsionicBlast),
        typeof(ScrollOfMirrorImage),
        typeof(ScrollOfUpgrade),
        typeof(ScrollOfEnchantment),
    ];
    private static readonly string[] Runes =
    [
        "KAUNAN",
        "SOWLIO",
        "LAGUZ",
        "YNGVI",
        "GYFU",
        "RAIDO",
        "ISAZ",
        "MANNAZ",
        "NAUDIZ",
        "BERKANAN",
        "ODAL",
        "TIWAZ"
    ];
    private static readonly int[] Images =
    [
        ItemSpriteSheet.ScrollKaunan,
        ItemSpriteSheet.ScrollSowilo,
        ItemSpriteSheet.ScrollLaguz,
        ItemSpriteSheet.ScrollYngvi,
        ItemSpriteSheet.ScrollGyfu,
        ItemSpriteSheet.ScrollRaido,
        ItemSpriteSheet.ScrollIsaz,
        ItemSpriteSheet.ScrollMannaz,
        ItemSpriteSheet.ScrollNaudiz,
        ItemSpriteSheet.ScrollBerkanan,
        ItemSpriteSheet.ScrollOdal,
        ItemSpriteSheet.ScrollTiwaz
    ];
    
    private static ItemStatusHandler<Scroll> _handler;

    private readonly string _rune;

    protected Scroll()
    {
        Stackable = true;
        DefaultAction = AcRead;
        
        ImageValue = _handler.Image(this);
        _rune = _handler.Label(this);
    }

    public static void InitLabels()
    {
        _handler = new ItemStatusHandler<Scroll>(Scrolls, Runes, Images);
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        actions.Add(AcRead);
        return actions;
    }

    public override void Execute(Hero hero, string action)
    {
        if (action == AcRead)
        {
            if (hero.GetBuff<Blindness>() != null)
            {
                GLog.W(TxtBlinded);
            }
            else
            {
                CurUser = hero;
                CurItem = Detach(hero.Belongings.Backpack);
                DoRead();
            }
        }
        else
        {
            base.Execute(hero, action);
        }
    }

    protected abstract void DoRead();

    protected void ReadAnimation()
    {
        CurUser.Spend(TimeToRead);
        CurUser.Busy();
        CurUser.Sprite.Read();
    }

    public bool IsKnown() => _handler.IsKnown(this);

    public void SetKnown()
    {
        if (!IsKnown())
        {
            _handler.Know(this);
        }
        
        // TODO: Badges.validateAllScrollsIdentified()
    }

    public override Item Identify()
    {
        SetKnown();
        return base.Identify();
    }

    public override string Name() => IsKnown() ? NameValue : "scroll \"" + _rune + "\"";

    public override string Info() => IsKnown() 
        ? Desc() 
        : "This parchment is covered with indecipherable writing, and bears a title of rune " +
          _rune + ". Who knows what it will do when read aloud?";

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => IsKnown();

    public static List<Type> GetKnown() => _handler.Known();

    public static List<Type> GetUnknown() => _handler.Unknown();
    
    public static bool AllKnown() => _handler.Known().Count == Scrolls.Length;

    public override int Price() => 15 * QuantityValue;
}