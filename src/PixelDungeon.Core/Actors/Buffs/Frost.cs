using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Actors.Buffs;

public class Frost : FlavorBuff
{
    private const float DurationConst = 5f;
    
    public override bool AttachTo(Char target)
    {
        if (!base.AttachTo(target)) return false;
        
        target.Paralysed = true;
        Detach<Burning>(target);

        if (target is not Hero.Hero hero) return true;
        
        var item = hero.Belongings.RandomUnequipped();
        
        if (item is not MysteryMeat) return true;
        
        item.Detach(hero.Belongings.Backpack);
        var carpaccio = new FrozenCarpaccio();
        if (!carpaccio.Collect(hero.Belongings.Backpack))
        {
            Dungeon.Level.Drop(carpaccio, target.Pos).Sprite.Drop();
        }

        return true;

    }

    public override void Detach()
    {
        base.Detach();
        Paralysis.Unfreeze(Target);
    }

    public override int Icon() => BuffIndicator.Frost;

    public override string ToString() => "Frozen";

    public static float Duration(Char ch)
    {
        // TODO: RingOfElements.Resistance buff
        return DurationConst;
    }
}