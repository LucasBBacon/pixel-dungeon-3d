using System.Collections;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Keys;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors.Hero;

public class Belongings : IEnumerable<Item>
{
    public const int BackpackSize = 19;

    public Hero Owner;

    public Bag Backpack;

    public KindOfWeapon Weapon = null;

    // TODO: amor is an Armor
    public Item Armor = null;

    // TODO: ring1 and ring2 are rings
    public Item Ring1 = null;
    public Item Ring2 = null;

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

    private const string TagWeapon = "weapon";
    private const string TagArmor = "armor";
    private const string TagRing1 = "ring1";
    private const string TagRing2 = "ring2";

    public void StoreInBundle(Bundle bundle)
    {
        Backpack.StoreInBundle(bundle);

        bundle.Put(TagWeapon, Weapon);
        bundle.Put(TagArmor, Armor);
        bundle.Put(TagRing1, Ring1);
        bundle.Put(TagRing2, Ring2);
    }

    public void RestoreFromBundle(Bundle bundle)
    {
        Backpack.Clear();
        Backpack.RestoreFromBundle(bundle);

        Weapon = (KindOfWeapon)bundle.Get(TagWeapon);
        Weapon?.Activate(Owner);

        Armor = (Item)bundle.Get(TagArmor);

        Ring1 = (Item)bundle.Get(TagRing1);
        // TODO: ring1.activate(owner) when a ring is restored

        Ring2 = (Item)bundle.Get(TagRing2);
        // TODO: ring2.activate(owner) when a ring is restored
    }

    public T GetItem<T>() where T : Item
    {
        foreach (var item in this)
        {
            if (item is T found)
            {
                return found;
            }
        }

        return null;
    }

    // getItem(Class) with a runtime class, for QuickSlot's class-form slots
    public Item GetItem(Type itemClass)
    {
        return this.FirstOrDefault(itemClass.IsInstanceOfType);
    }

    public T GetKey<T>(int depth) where T : Key
    {
        return Backpack
            .Where(item => item.GetType() == typeof(T) && ((Key)item).Depth == depth)
            .Cast<T>()
            .FirstOrDefault();
    }

    public void CountIronKeys()
    {
        IronKey.CurDepthQuantity = 0;

        foreach (var item in Backpack)
        {
            if (item is IronKey key && key.Depth == Dungeon.Depth)
            {
                IronKey.CurDepthQuantity++;
            }
        }
    }

    public void Identify()
    {
        foreach (var item in this)
        {
            item.Identify();
        }
    }

    public void Observe()
    {
        if (Weapon != null)
        {
            Weapon.Identify();
            // TODO: Badges.validateItemLevelAquired(weapon)
        }

        if (Armor != null)
        {
            Armor.Identify();
            // TODO: Badges.validateItemLevelAquired(armor)
        }

        if (Ring1 != null)
        {
            Ring1.Identify();
            // TODO: Badges.validateItemLevelAquired(ring1)
        }

        if (Ring2 != null)
        {
            Ring2.Identify();
            // TODO: Badges.validateItemLevelAquired(ring2)
        }

        foreach (var item in Backpack)
        {
            item.CursedKnown = true;
        }
    }

    // TODO: uncurseEquipped() calls ScrollOfRemoveCurse.uncurse(owner, armor, weapon, ring1, ring2)

    public Item RandomUnequipped() => Random.Element(Backpack.Items);

    public void Resurrect(int depth)
    {
        foreach (var item in Backpack.Items.ToArray())
        {
            if (item is Key key)
            {
                if (key.Depth == depth)
                {
                    item.DetachAll(Backpack);
                }
            }
            else if (item.Unique)
            {
                // keep unique items
            }
            else if (!item.IsEquipped(Owner))
            {
                item.DetachAll(Backpack);
            }
        }

        if (Weapon != null)
        {
            Weapon.Cursed = false;
            Weapon.Activate(Owner);
        }

        if (Armor != null)
        {
            Armor.Cursed = false;
        }

        if (Ring1 != null)
        {
            Ring1.Cursed = false;
            // TODO: ring1.activate(owner)
        }

        if (Ring2 != null)
        {
            Ring2.Cursed = false;
            // TODO: ring2.activate(owner)
        }
    }

    // TODO: charge(full) and discharge() recharge or drain every Wand held

    // ItemIterator in java, the four slots that are not null, then the backpack
    // its remove() is not ported, nothing in java calls it
    public IEnumerator<Item> GetEnumerator()
    {
        foreach (var item in new[] { Weapon, Armor, Ring1, Ring2 })
        {
            if (item != null)
            {
                yield return item;
            }
        }

        foreach (var item in Backpack)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}