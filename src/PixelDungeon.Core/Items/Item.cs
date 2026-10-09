using PixelDungeon.Core.Actors.Hero;
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

    // TODO: TxtPackFull, TimeToThrow/PickUp/Drop, AcDrop/AcThrow, actions, pick up, drop, throw, collect, detach

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

    public void RestoreFromBundle(Bundle bundle)
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

    public void StoreInBundle(Bundle bundle)
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
}