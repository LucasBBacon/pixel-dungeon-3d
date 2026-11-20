using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Scrolls;

public class ScrollOfChallenge : Scroll
{
    public ScrollOfChallenge()
    {
        NameValue = "Scroll of Challenge";
    }

    protected override void DoRead()
    {
        foreach (var mob in Dungeon.Level.Mobs.ToArray())
        {
            mob.Beckon(CurUser.Pos);
            if (Dungeon.Visible[mob.Pos])
            {
                Buff.Affect<Rage>(mob, Levels.Level.Distance(CurUser.Pos, mob.Pos));
            }
        }

        GLog.W("The scroll emits a challenging roar that echoes throughout the dungeon!");
        SetKnown();
        
        CurUser.Sprite.Burst(0xFFFFFFFF, 3);
        Sample.Play(Assets.SndChallenge);
        Invisibility.Dispel();
        
        ReadAnimation();
    }

    public override string Desc() =>
        "When read aloud, this scroll will unleash a challenging roar " +
        "that will awaken all monsters and alert them to the reader's location.";
}