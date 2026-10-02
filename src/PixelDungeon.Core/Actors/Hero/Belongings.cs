using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Hero;

public class Belongings
{
    public Hero Owner;

    public Item Weapon = null; // KindOfWeapon in java
    public Item Armor = null;

    // TODO: backpack, ring1, ring2, Iterable<Item>, getKey, countIronKeys, observe, uncurseEquipped, discharge, WEAPON/ARMOR/RING1/RING2 bundle keys

    public Belongings(Hero owner)
    {
        Owner = owner;
    }

    public T GetItem<T>() where T : Item
    {
        return null;
    }

    public void Identify()
    {
    }

    public void Resurrect(int depth)
    {
    }

    public void StoreInBundle(Bundle bundle)
    {
    }

    public void RestoreFromBundle(Bundle bundle)
    {
    }
}