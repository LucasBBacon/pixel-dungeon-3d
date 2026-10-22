using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items;

public abstract class EquipableItem : Item
{
    private const string TxtUnequipCursed = "You can't remove cursed {0}!";

    public const string AcEquip = "EQUIP";
    public const string AcUnequip = "UNEQUIP";

    public override void Execute(Hero hero, string action)
    {
        if (action == AcEquip)
        {
            DoEquip(hero);
        }
        else if (action == AcUnequip)
        {
            DoUnequip(hero, true);
        }
        else
        {
            base.Execute(hero, action);
        }
    }

    public override void DoDrop(Hero hero)
    {
        if (!IsEquipped(hero) || DoUnequip(hero, false, false))
        {
            base.DoDrop(hero);
        }
    }

    public override void Cast(Hero user, int dst)
    {
        if (IsEquipped(user))
        {
            if (QuantityValue == 1 && !DoUnequip(user, false, false))
            {
                return;
            }
        }

        base.Cast(user, dst);
    }

    protected static void EquipCursed(Hero hero)
    {
        hero.Sprite.Burst(0xFF000000, 6); // ShadowParticle.CURSE
        Sample.Play(Assets.SndCursed);
    }

    protected internal virtual float Time2Equip(Hero hero) => 1;

    public abstract bool DoEquip(Hero hero);

    public virtual bool DoUnequip(Hero hero, bool collect, bool single)
    {
        if (Cursed)
        {
            GLog.W(TxtUnequipCursed, Name());
            return false;
        }

        if (single)
        {
            hero.SpendAndNext(Time2Equip(hero));
        }
        else
        {
            hero.Spend(Time2Equip(hero));
        }

        if (collect && !Collect(hero.Belongings.Backpack))
        {
            Dungeon.Level.Drop(this, hero.Pos);
        }

        return true;
    }

    public bool DoUnequip(Hero hero, bool collect) => DoUnequip(hero, collect, true);
}