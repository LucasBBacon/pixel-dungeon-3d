using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items;

public abstract class KindOfWeapon : EquipableItem
{
    private const string TxtEquipCursed = "you wince as your grip involuntarily tightens around your {0}";

    protected const float TimeToEquip = 1f;

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        actions.Add(IsEquipped(hero) ? AcUnequip : AcEquip);
        return actions;
    }

    public override bool IsEquipped(Hero hero) => hero.Belongings.Weapon == this;

    public override bool DoEquip(Hero hero)
    {
        DetachAll(hero.Belongings.Backpack);

        if (hero.Belongings.Weapon == null || hero.Belongings.Weapon.DoUnequip(hero, true))
        {
            hero.Belongings.Weapon = this;
            Activate(hero);

            CursedKnown = true;
            if (Cursed)
            {
                EquipCursed(hero);
                GLog.N(TxtEquipCursed, Name());
            }

            hero.SpendAndNext(TimeToEquip);
            return true;
        }
        else
        {
            Collect(hero.Belongings.Backpack);
            return false;
        }
    }

    public override bool DoUnequip(Hero hero, bool collect, bool single)
    {
        if (base.DoUnequip(hero, collect, single))
        {
            hero.Belongings.Weapon = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual void Activate(Hero hero)
    {
    }

    public abstract int Min();

    public abstract int Max();

    public virtual int DamageRoll(Hero owner) => Utils.Random.NormalIntRange(Min(), Max());

    public virtual float AccuracyFactor(Hero hero) => 1f;

    public virtual float SpeedFactor(Hero hero) => 1f;

    public virtual void Proc(Char attacker, Char defender, int damage)
    {
    }
}