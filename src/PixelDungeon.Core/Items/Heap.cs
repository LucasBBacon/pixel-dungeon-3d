using PixelDungeon.Core.Utils;

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
    public HeapType Type = HeapType.Heap;
    public int Pos = 0;
    public LinkedList<Item> Items = new();

    public int Size()
    {
        return Items.Count;
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

        Items.AddFirst(item);
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