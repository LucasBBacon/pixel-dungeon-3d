using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Scenes;
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
    private const int SeedsToPotion = 3;
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

    public void Burn()
    {
        // TODO: MIMIC heap spawns mimic, sets it burning, bursts five flames and is destroyed
        if (Type != HeapType.Heap)
        {
            return;
        }

        var burnt = false;
        var evaporated = false;

        foreach (var item in Items.ToArray())
        {
            if (item is Scrolls.Scroll)
            {
                Items.Remove(item);
                burnt = true;
            }
            else if (item is Dewdrop)
            {
                Items.Remove(item);
                evaporated = true;
            }
            else if (item is Food.MysteryMeat meat)
            {
                Replace(item, Food.ChargrilledMeat.Cook(meat));
                burnt = true;
            }
        }

        if (burnt || evaporated)
        {
            if (Dungeon.Visible[Pos])
            {
                if (burnt)
                {
                    BurnFx(Pos);
                }
                else
                {
                    EvaporateFx(Pos);
                }
            }

            if (IsEmpty())
            {
                Destroy();
            }
            else
            {
                Sprite.View(Image(), Glowing());
            }
        }
    }

    public void Freeze()
    {
        // TODO: MIMIC heap spawns frozen mimic for Frost.Duration(m) * Random.Float(1.0f, 1.5f), destroyed
        if (Type != HeapType.Heap)
        {
            return;
        }

        var frozen = false;
        foreach (var item in Items.ToArray())
        {
            if (item is Food.MysteryMeat meat)
            {
                Replace(item, Food.FrozenCarpaccio.Cook(meat));
                frozen = true;
            }
        }

        if (frozen)
        {
            if (IsEmpty())
            {
                Destroy();
            }
            else
            {
                Sprite.View(Image(), Glowing());
            }
        }
    }

    public static void BurnFx(int pos)
    {
        GameScene.Splash(pos, 0xFF22EE66, 6);
        Sample.Play(Assets.SndBurning);
    }

    public static void EvaporateFx(int pos)
    {
        GameScene.Splash(pos, 0xFFE8E8E8, 5);
    }

    public Item Transmute()
    {
        GameScene.Splash(Pos, 0xFFAADDFF, 3);
        GameScene.Splash(Pos, 0xFFFFFFFF, 3);

        var chances = new float[Items.Count];
        var count = 0;

        var index = 0;
        foreach (var item in Items)
        {
            if (item is Plant.Seed)
            {
                count += item.Quantity();
                chances[index++] = item.Quantity();
            }
            else
            {
                count = 0;
                break;
            }
        }

        if (count >= SeedsToPotion)
        {
            GameScene.Splash(Pos, 0xFFEEEEEE, 6);
            Sample.Play(Assets.SndPuff);

            if (Random.Int(count) == 0)
            {
                GameScene.Splash(Pos, 0xFFFFFFFF, 3);

                Destroy();

                Statistics.PotionsCooked++;
                // TODO: Badges.validatePotionsCooked()

                return Generator.Random(Generator.Category.Potion);
            }
            else
            {
                var proto = (Plant.Seed)Items.ElementAt(Random.Chances(chances));
                var itemClass = proto.AlchemyClass;

                Destroy();

                Statistics.PotionsCooked++;
                // TODO: Badges.validatePotionsCooked()

                if (itemClass == null)
                {
                    return Generator.Random(Generator.Category.Potion);
                }

                return (Item)Activator.CreateInstance(itemClass);
            }
        }
        else
        {
            return null;
        }
    }

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