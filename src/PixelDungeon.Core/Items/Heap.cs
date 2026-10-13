using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items;

public enum HeapType
{
    Heap,
    ForSale,
    Chest,
    LockedChest,
    CrystalChest,
    Tomb,
    Skeleton,
    Mimic,
    Hidden
}

public class Heap : IBundlable
{
    // TODO: TXT_MIMIC = "This is a mimic!" (logged when Mimic.spawnAt succeeds)
    // TODO: SEEDS_TO_POTION = 3 (transmute)
    // FADE_TIME belongs to the view's hidden-heap fade.

    public HeapType Type = HeapType.Heap;
    public int Pos = 0;
    public IHeapView Sprite = NullHeapView.Instance;

    public LinkedList<Item> Items = new();

    public int Image()
    {
        return Type switch
        {
            HeapType.Heap or HeapType.ForSale => Size() > 0 ? Items.First.Value.Image() : 0,
            HeapType.Chest or HeapType.Mimic => ItemSpriteSheet.Chest,
            HeapType.LockedChest => ItemSpriteSheet.LockedChest,
            HeapType.CrystalChest => ItemSpriteSheet.CrystalChest,
            HeapType.Tomb => ItemSpriteSheet.Tomb,
            HeapType.Skeleton => ItemSpriteSheet.Bones,
            HeapType.Hidden => ItemSpriteSheet.Hidden,
            _ => 0
        };
    }

    public Glowing Glowing()
    {
        return Type is HeapType.Heap or HeapType.ForSale && Items.Count > 0
            ? Items.First.Value.Glowing()
            : null;
    }

    public void Open(Hero hero)
    {
        switch (Type)
        {
            case HeapType.Mimic:
                // TODO: Mimic.spawnAt(pos, items) returns a mimic, GLog.n(TXT_MIMIC) and destroy()
                Type = HeapType.Chest;
                break;
            case HeapType.Tomb:
                // TODO: Wraith.spawnAround(hero.pos)
                break;
            case HeapType.Skeleton:
                // TODO: CellEmitter.center(pos).start(Speck.factory(Speck.RATTLE), 0.1f, 3)
                foreach (var item in Items)
                {
                    if (item.Cursed)
                    {
                        // TODO: Wraith.spawnAt(pos) == null guards these three lines
                        hero.Sprite.Burst(0xFF000000, 6); // ShadowParticle.Curse
                        hero.Damage(hero.HP / 2, this);
                        Sample.Play(Assets.SndCursed);
                        break;
                    }
                }

                break;
            case HeapType.Hidden:
                // TODO: sprite fades in from alpha 0 over FADE_TIME
                break;

            default:
                break;
        }

        if (Type != HeapType.Mimic)
        {
            Type = HeapType.Heap;
            Sprite.Link();
            Sprite.Drop();
        }
    }

    public int Size()
    {
        return Items.Count;
    }

    public Item PickUp()
    {
        var item = Items.First.Value;
        Items.RemoveFirst();
        if (Items.Count == 0)
        {
            Destroy();
        }
        else
        {
            Sprite.View(Image(), Glowing());
        }

        return item;
    }

    public Item Peek()
    {
        return Items.First?.Value;
    }

    public void Drop(Item item)
    {
        if (item == null)
        {
            return; // generator returns null until implementation
        }

        if (item.Stackable)
        {
            var c = item.GetType();
            foreach (var i in Items)
            {
                if (i.GetType() == c)
                {
                    i.QuantityValue += item.QuantityValue;
                    item = i;
                    break;
                }
            }

            Items.Remove(item);
        }

        if (item is Dewdrop)
        {
            Items.AddLast(item);
        }
        else
        {
            Items.AddFirst(item);
        }

        Sprite.View(Image(), Glowing());
    }

    public void Replace(Item a, Item b)
    {
        var node = Items.Find(a);
        if (node != null)
        {
            Items.AddBefore(node, b);
            Items.Remove(node);
        }
    }

    // TODO: burn(), freeze(), transmute(), burnFX(), evaporateFX()

    public bool IsEmpty() => Items == null || Items.Count == 0;


    public void Destroy()
    {
        Dungeon.Level.Heaps.Remove(Pos);
        Sprite.Kill();
        Items.Clear();
        Items = null;
    }

    private const string PosConst = "pos";
    private const string TypeConst = "type";
    private const string ItemsConst = "items";

    public void RestoreFromBundle(Bundle bundle)
    {
        Pos = bundle.GetInt(PosConst);
        Type = bundle.GetEnum<HeapType>(TypeConst);
        Items = new LinkedList<Item>();
        foreach (
            var item in bundle.GetCollection(ItemsConst)
                .Where(item => item != null)
        )
        {
            Items.AddLast((Item)item);
        }
    }

    public void StoreInBundle(Bundle bundle)
    {
        bundle.Put(PosConst, Pos);
        bundle.Put(TypeConst, Type);
        bundle.Put(ItemsConst, Items);
    }
}