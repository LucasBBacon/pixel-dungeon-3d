using System.Collections;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Bags;

public class Bag : Item, IEnumerable<Item>
{
    // TODO: ACOpen is the default action

    public Char Owner;

    public List<Item> Items = [];

    public int Size = 1;

    public Bag()
    {
        ImageValue = 11;
    }

    public override bool Collect(Bag container)
    {
        if (base.Collect(container))
        {
            Owner = container.Owner;

            foreach (var item in container.Items.ToArray())
            {
                if (Grab(item))
                {
                    item.DetachAll(container);
                    item.Collect(this);
                }
            }

            // TODO: Badges.ValidateAllBagsBought(this)
            return true;
        }
        else
        {
            return false;
        }
    }

    protected override void OnDetach() => Owner = null;

    public override bool IsUpgradable() => false;

    public override bool IsIdentified() => true;

    public void Clear() => Items.Clear();

    private const string TagItems = "inventory";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);
        foreach (var item in bundle.GetCollection(TagItems))
        {
            // bundle keeps a null for unresolvable class name
            ((Item)item)?.Collect(this);
        }
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);
        bundle.Put(TagItems, Items);
    }

    public bool Contains(Item item)
    {
        foreach (var i in Items)
        {
            if (i == item)
            {
                return true;
            }
            else if (i is Bag bag && bag.Contains(item))
            {
                return true;
            }
        }

        return false;
    }

    public virtual bool Grab(Item item) => false;

    public IEnumerator<Item> GetEnumerator()
    {
        foreach (var item in Items)
        {
            yield return item;
            if (item is Bag bag)
            {
                foreach (var nested in bag)
                {
                    yield return nested;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}