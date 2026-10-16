using System.Collections;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Items.Bags;

public class Bag : Item, IEnumerable<Item>
{
    public const string AcOpen = "OPEN";

    public Char Owner;

    public List<Item> Items = [];

    public int Size = 1;

    public Bag()
    {
        ImageValue = 11;
        DefaultAction = AcOpen;
    }

    public override List<string> Actions(Hero hero)
    {
        var actions = base.Actions(hero);
        return actions;
    }

    public override void Execute(Hero hero, string action)
    {
        if (action == AcOpen)
        {
            GameScene.ShowBag(this);
        }
        else
        {
            base.Execute(hero, action);
        }
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