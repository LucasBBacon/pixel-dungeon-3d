using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Levels.Features;

public static class Chasm
{
    private const string TxtChasm = "Chasm";
    private const string TxtYes = "Yes, I know what I'm doing";
    private const string TxtNo = "No, I changed my mind";
    private const string TxtJump = "Do you really want to jump into the chasm? You can probably die.";

    public static bool JumpConfirmed = false;

    public static void HeroJump(Hero hero)
    {
        GameScene.Show(new WindowRequest(TxtChasm,
            TxtJump,
            [TxtYes, TxtNo],
            index =>
            {
                if (index == 0)
                {
                    JumpConfirmed = true;
                    hero.Resume();
                }
            }));
    }

    public static void HeroFall(int pos)
    {
        JumpConfirmed = false;

        Sample.Play(Assets.SndFalling);

        if (Dungeon.Hero.IsAlive())
        {
            Dungeon.Hero.Interrupt();
            Interlevel.Mode = InterlevelMode.Fall;
            if (Dungeon.Level is RegularLevel regular)
            {
                var room = regular.RoomAt(pos);
                Interlevel.FallIntoPit = room is { Type: RoomType.WeakFloor };
            }
            else
            {
                Interlevel.FallIntoPit = false;
            }

            GameScene.SwitchLevel((InterlevelMode.Fall));
        }
        else
        {
            Dungeon.Hero.Sprite.Visible = false;
        }
    }

    public static void HeroLand()
    {
        var hero = Dungeon.Hero;

        hero.Sprite.Burst(0xFFBB0000, 10); // CharSprite.Blood() default colour
        GameScene.Shake(4, 0.2f);

        // TODO: Buff.Prolong(hero, Cripple.Class, Cripple.Duration)
        hero.Damage(Random.IntRange(hero.HT / 3, hero.HT / 2), new FallDoom());
    }

    private sealed class FallDoom : Hero.IDoom
    {
        public void OnDeath()
        {
            // TODO: Badges.ValidateDeathFromFalling()
            Dungeon.Fail(TextUtils.Format(ResultDescriptions.Fall, Dungeon.Depth));
            GLog.N("You fell to death...");
        }
    }

    public static void MobFall(Mob mob)
    {
        mob.Destroy();
        // TODO: ((MobSprite)Mob.Sprite).Fall() plays the falling animation before hiding
        mob.Sprite.Die();
    }
}