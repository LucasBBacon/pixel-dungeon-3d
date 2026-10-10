using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Hero;

public class Belongings
{
    public const int BackpackSize = 19;

    public Hero Owner;

    public Bag Backpack;

    public Item Weapon = null; // KindOfWeapon in java
    public Item Armor = null;


    // TODO: backpack, ring1, ring2, Iterable<Item>, getKey, countIronKeys, observe, uncurseEquipped, discharge, WEAPON/ARMOR/RING1/RING2 bundle keys

    public Belongings(Hero owner)
    {
        Owner = owner;

        Backpack = new Bag
        {
            NameValue = "backpack",
            Size = BackpackSize,
            Owner = owner
        };
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