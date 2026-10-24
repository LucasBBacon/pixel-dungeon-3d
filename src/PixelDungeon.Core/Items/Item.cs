using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Items;

public class Item : IBundlable
{
    private const string TxtBroken = "Because of frequent use, your {0} has broken.";
    private const string TxtGonnaBreak = "Because of frequent use, your {0} is going to break soon.";

    private const string TxtToString = "{0}";
    private const string TxtToStringX = "{0} x{1}";
    private const string TxtToStringLvl = "{0}{1:+#;-#;+0}";
    private const string TxtToStringLvlX = "{0}{1:+#;-#;+0} x{2}";

    private const float DurabilityWarningLevel = 1 / 6f;

    private const string TxtPackFull = "Your pack is too full for the {0}";

    protected const float TimeToPickUp = 1.0f;

    protected const float TimeToThrow = 1.0f;
    protected const float TimeToDrop = 0.5f;

    public const string AcDrop = "DROP";
    public const string AcThrow = "THROW";

    public string DefaultAction;

    // name, image and quantity fields behind same-named methods in java
    // fields here take the value suffix
    protected internal string NameValue = "smth";
    protected internal int ImageValue = 0;

    public bool Stackable = false;
    protected internal int QuantityValue = 1;

    private int _level = 0;
    private int _durability;
    public bool LevelKnown;

    public bool Cursed;
    public bool CursedKnown;

    public bool Unique = false;

    public Item()
    {
        _durability = MaxDurability(); // copying java's field initializer
    }

    public virtual List<string> Actions(Hero hero)
    {
        var actions = new List<string>
        {
            AcDrop,
            AcThrow
        };
        return actions;
    }

    public virtual bool DoPickUp(Hero hero)
    {
        if (Collect(hero.Belongings.Backpack))
        {
            GameScene.PickUp(this);
            Sample.Play(Assets.SndItem);
            hero.SpendAndNext(TimeToPickUp);
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual void DoDrop(Hero hero)
    {
        hero.SpendAndNext(TimeToDrop);
        Dungeon.Level
            .Drop(DetachAll(hero.Belongings.Backpack), hero.Pos)
            .Sprite
            .Drop(hero.Pos);
    }

    public virtual void DoThrow(Hero hero)
    {
        GameScene.SelectCell(Thrower);
    }

    public virtual void Execute(Hero hero, string action)
    {
        CurUser = hero;
        CurItem = this;

        if (action == AcDrop)
        {
            DoDrop(hero);
        }
        else if (action == AcThrow)
        {
            DoThrow(hero);
        }
    }

    public virtual void Execute(Hero hero)
    {
        Execute(hero, DefaultAction);
    }

    protected virtual void OnThrow(int cell)
    {
        var heap = Dungeon.Level.Drop(this, cell);
        if (!heap.IsEmpty())
        {
            heap.Sprite.Drop(cell);
        }
    }

    public virtual bool Collect(Bag container)
    {
        var items = container.Items;

        if (items.Contains(this))
        {
            return true;
        }

        foreach (var item in items)
        {
            if (item is Bag bag && bag.Grab(this))
            {
                return Collect(bag);
            }
        }

        if (Stackable)
        {
            var c = GetType();
            foreach (var item in items.Where(item => item.GetType() == c))
            {
                item.QuantityValue += QuantityValue;
                item.UpdateQuickslot();
                return true;
            }
        }

        if (items.Count < container.Size)
        {
            // TODO: Badges.validateItemLevelAquired(this) while the hero is alive

            items.Add(this);
            QuickSlot.Refresh();

            // Collections.sort is a stable merge sort; List.Sort is not. OrderBy is stable.
            var sorted = items.OrderBy(Generator.Order).ToList();
            items.Clear();
            items.AddRange(sorted);
            return true;
        }
        else
        {
            GLog.N(TxtPackFull, Name());
            return false;
        }
    }

    public virtual bool Collect() => Collect(Dungeon.Hero.Belongings.Backpack);

    public Item Detach(Bag container)
    {
        switch (QuantityValue)
        {
            case <= 0:
                return null;
            case 1:
                return DetachAll(container);
            default:
                QuantityValue--;
                UpdateQuickslot();

                try
                {
                    var detached = (Item)Activator.CreateInstance(GetType());
                    detached.OnDetach();
                    return detached;
                }
                catch (Exception)
                {
                    return null;
                }
        }
    }

    public Item DetachAll(Bag container)
    {
        foreach (var item in container.Items)
        {
            if (item == this)
            {
                container.Items.Remove(this);
                item.OnDetach();
                QuickSlot.Refresh();
                return this; // returns before enumerator advances past removal
            }

            if (item is not Bag bag) continue;

            if (bag.Contains(this))
            {
                return DetachAll(bag);
            }
        }

        return this;
    }

    protected virtual void OnDetach()
    {
    }

    public int Level() => _level;

    public void Level(int value) => _level = value;

    public virtual int EffectiveLevel() => IsBroken() ? 0 : _level;

    public virtual Item Upgrade()
    {
        Cursed = false;
        CursedKnown = true;

        _level++;
        Fix();

        return this;
    }

    public Item Upgrade(int n)
    {
        for (var i = 0; i < n; i++)
        {
            Upgrade();
        }

        return this;
    }

    public virtual Item Degrade()
    {
        _level--;
        Fix();

        return this;
    }

    public Item Degrade(int n)
    {
        for (var i = 0; i < n; i++)
        {
            Degrade();
        }

        return this;
    }

    public virtual void Use()
    {
        if (_level > 0 && !IsBroken())
        {
            var threshold = (int)(MaxDurability() * DurabilityWarningLevel);
            if (_durability-- >= threshold && threshold > _durability && LevelKnown)
            {
                GLog.W(TxtGonnaBreak, Name());
            }

            if (IsBroken())
            {
                GetBroken();
                if (LevelKnown)
                {
                    GLog.N(TxtBroken, Name());
                    Dungeon.Hero.Interrupt();

                    // TODO: Degradation.weapon/armor/ring/wand shows breaking item over hero sprite
                    Sample.Play(Assets.SndDegrade);
                }
            }
        }
    }

    public virtual bool IsBroken() => _durability <= 0;

    public virtual void GetBroken()
    {
    }

    public virtual void Fix() => _durability = MaxDurability();

    public virtual void Polish()
    {
        if (_durability < MaxDurability())
        {
            _durability++;
        }
    }

    public int Durability() => _durability;

    public virtual int MaxDurability(int lvl) => 1;

    public int MaxDurability() => MaxDurability(_level);

    public virtual int VisiblyUpgraded() => LevelKnown ? _level : 0;

    public virtual bool VisiblyCursed() => Cursed && CursedKnown;

    public virtual bool VisiblyBroken() => LevelKnown && IsBroken();

    public virtual bool IsUpgradable() => true;

    public virtual bool IsIdentified() => LevelKnown && CursedKnown;

    public virtual bool IsEquipped(Hero hero) => false;

    public virtual Item Identify()
    {
        LevelKnown = true;
        CursedKnown = true;

        return this;
    }

    public static void Evoke(Hero hero)
    {
        hero.Sprite.Burst(0xFFFFFF, 5); // speck.evoke
    }

    public override string ToString()
    {
        if (LevelKnown && _level != 0)
        {
            if (QuantityValue > 1)
            {
                return TextUtils.Format(TxtToStringLvlX, Name(), _level, QuantityValue);
            }
            else
            {
                return TextUtils.Format(TxtToStringLvl, Name(), _level);
            }
        }
        else
        {
            if (QuantityValue > 1)
            {
                return TextUtils.Format(TxtToStringX, Name(), QuantityValue);
            }
            else
            {
                return TextUtils.Format(TxtToString, Name());
            }
        }
    }

    public virtual string Name() => NameValue;

    public string TrueName() => NameValue;

    public virtual int Image() => ImageValue;

    public virtual Glowing Glowing() => null;

    public virtual string Info() => Desc();

    public virtual string Desc() => "";

    public virtual int Quantity() => QuantityValue;

    public virtual void Quantity(int value) => QuantityValue = value;

    public virtual int Price()
    {
        return 0;
    }

    public virtual int ConsiderState(int price)
    {
        if (Cursed && CursedKnown)
        {
            price /= 2;
        }

        if (LevelKnown)
        {
            if (_level > 0)
            {
                price *= (_level + 1);
                if (IsBroken())
                {
                    price /= 2;
                }
            }
            else if (_level < 0)
            {
                price /= (1 - _level);
            }
        }

        if (price < 1)
        {
            price = 1;
        }

        return price;
    }

    public static Item Virtual(Type cl)
    {
        try
        {
            var item = (Item)Activator.CreateInstance(cl);
            item.QuantityValue = 0;
            return item;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public virtual Item Random() => this;

    public virtual string Status() => QuantityValue != 1 ? QuantityValue.ToString() : null;

    public virtual void UpdateQuickslot()
    {
        if (Stackable)
        {
            var cl = GetType();
            if (ReferenceEquals(QuickSlot.PrimaryValue, cl) || ReferenceEquals(QuickSlot.SecondaryValue, cl))
            {
                QuickSlot.Refresh();
            }
        }
        else if (ReferenceEquals(QuickSlot.PrimaryValue, this) || ReferenceEquals(QuickSlot.SecondaryValue, this))
        {
            QuickSlot.Refresh();
        }
    }

    private const string TagQuantity = "quantity";
    private const string TagLevel = "level";
    private const string TagLevelKnown = "levelKnown";
    private const string TagCursed = "cursed";
    private const string TagCursedKnown = "cursedKnown";
    private const string TagDurability = "durability";

    public virtual void RestoreFromBundle(Bundle bundle)
    {
        QuantityValue = bundle.GetInt(TagQuantity);
        LevelKnown = bundle.GetBoolean(TagLevelKnown);
        CursedKnown = bundle.GetBoolean(TagCursedKnown);

        var level = bundle.GetInt(TagLevel);
        switch (level)
        {
            case > 0:
                Upgrade(level);
                break;
            case < 0:
                Degrade(-level);
                break;
        }

        Cursed = bundle.GetBoolean(TagCursed);

        if (IsUpgradable())
        {
            _durability = bundle.GetInt(TagDurability);
        }

        QuickSlot.Restore(bundle, this);
    }

    public virtual void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagQuantity, QuantityValue);
        bundle.Put(TagLevel, _level);
        bundle.Put(TagLevelKnown, LevelKnown);
        bundle.Put(TagCursed, Cursed);
        bundle.Put(TagCursedKnown, CursedKnown);
        if (IsUpgradable())
        {
            bundle.Put(TagDurability, _durability);
        }

        QuickSlot.Save(bundle, this);
    }

    public virtual void Cast(Hero user, int dst)
    {
        var cell = Ballistica.Cast(user.Pos, dst, false, true);
        user.Sprite.Zap(cell);
        user.Busy();

        Sample.Play(Assets.SndMiss, 1.5f);

        var enemy = Actor.FindChar(cell);
        QuickSlot.Target(this, enemy);

        // TODO: FIX THIS
        var delay = TimeToThrow;
        if (this is MissileWeapon)
        {
            delay *= ((MissileWeapon)this).SpeedFactor(user);
            // TODO: a SnipersMark on that enemy halves it and is removed
        }

        var finalDelay = delay;

        // hero is Actor._current and stays so until SpendAndNext below, nothing acts while it flies
        GameScene.Missile(user.Pos, cell, this, () =>
        {
            Detach(user.Belongings.Backpack).OnThrow(cell);
            user.SpendAndNext(finalDelay);
        });
    }

    protected static Hero CurUser = null;
    protected static Item CurItem = null;

    protected static readonly ICellListener Thrower = new ThrowListener();


    private sealed class ThrowListener : ICellListener
    {
        public void OnSelect(int? target)
        {
            if (target != null)
            {
                CurItem.Cast(CurUser, target.Value);
            }
        }

        public string Prompt()
        {
            return "Choose direction of throw";
        }
    }
}