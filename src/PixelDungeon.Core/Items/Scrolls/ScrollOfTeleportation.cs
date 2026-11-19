using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items.Wands;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfTeleportation : Scroll
{
    public const string TxtTeleported = "In a blink of an eye you were teleported to another location of the level.";
    
    public const string TxtNoTeleport = "Strong magic aura of this place prevents you from teleporting!";
    
    public ScrollOfTeleportation()
    {
        NameValue = "Scroll of Teleportation";
    }
    
    protected override void DoRead()
    {
        Sample.Play(Assets.SndRead);
        Invisibility.Dispel();

        TeleportHero(CurUser);
        SetKnown();
        
        ReadAnimation();
    }

    public static void TeleportHero(Hero hero)
    {
        var count = 10;
        int pos;
        do
        {
            pos = Dungeon.Level.RandomRespawnCell();
            if (count-- <= 0)
            {
                break;
            }
        } while (pos == -1);

        if (pos == -1)
        {
            GLog.W(TxtNoTeleport);
        }
        else
        {
            WandOfBlink.Appear(hero, pos);
            Dungeon.Level.Press(pos, hero);
            Dungeon.Observe();
            
            GLog.I(TxtTeleported);
        }
    }

    public override string Desc() =>
        "The spell on this parchment instantly transports the reader " +
        "to a random location on the dungeon level. It can be used " +
        "to escape a dangerous situation, but the unlucky reader might " +
        "find himself in an even more dangerous place.";

    public override int Price() => IsKnown() ? 40 * QuantityValue : base.Price();
}