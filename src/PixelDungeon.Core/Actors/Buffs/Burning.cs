using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Buffs;

public class Burning : Buff, Hero.Hero.IDoom
{
    private const string TxtBurnsUp = "{0} burns up!";
    private const string TxtBurnedToDeath = "You burned to death...";
    
    private const float DurationConst = 8f;
    
    private float _left;

    private const string LeftTag = "left";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        _left = bundle.GetFloat(LeftTag);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(LeftTag, _left);
    }

    protected override bool Act()
    {
        if (Target.IsAlive())
        {
            // TODO: a hero target gets Buff.prolong(target, Light.class, TICK * 1.01f)

            Target.Damage(Random.Int(1, 5), this);

            if (Target is Hero.Hero hero)
            {
                var item = hero.Belongings.RandomUnequipped();
                if (item is Scroll)
                {
                    item = item.Detach(hero.Belongings.Backpack);
                    GLog.W(TxtBurnsUp, item.ToString());

                    Heap.BurnFx(Target.Pos);
                }
                
                else if (item is MysteryMeat)
                {
                    item = item.Detach(hero.Belongings.Backpack);
                    var steak = new ChargrilledMeat();
                    if (!steak.Collect(hero.Belongings.Backpack))
                    {
                        Dungeon.Level.Drop(steak, Target.Pos).Sprite.Drop();
                    }
                    GLog.W(TxtBurnsUp, item.ToString());
                    Heap.BurnFx(Target.Pos);
                }
            }
        }
        else
        {
            Detach();
        }

        if (Level.Flammable[Target.Pos])
        {
            GameScene.Add(Blob.Seed<Fire>(Target.Pos, 4));
        }

        if (_left <= 0 || 
            Random.Float() > (2 + (float)Target.HP / Target.HT) / 3 || 
            (Level.Water[Target.Pos] && !Target.Flying))
        {
            Detach();
        }

        return true;
    }

    public void Reignite(Char ch) => _left = Duration(ch);

    public override int Icon() => BuffIndicator.Fire;

    public override string ToString() => "Burning";

    private static float Duration(Char ch)
    {
        // TODO: RingOfElements.Resistance buff
        return DurationConst;
    }

    public void OnDeath()
    {
        // TODO: Badges.validateDeathFromFire()
        
        Dungeon.Fail(TextUtils.Format(ResultDescriptions.Burning, Dungeon.Depth));
        GLog.N(TxtBurnedToDeath);
    }
}