using System;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Client;

public partial class WndBag : Window
{
    private const int Cols = 6;

    private static readonly Color Normal = CharView.FromArgb(0xFF4A4D44); // ItemButton.NORMAL
    private static readonly Color Equipped = CharView.FromArgb(0xFF63665B); // ItemButton.NORMAL

    private const int NBars = 3; // ItemButton.NBARS
    private static readonly Color BarFull = CharView.FromArgb(0xFF00EE00);
    private static readonly Color BarEmpty = CharView.FromArgb(0xFFCC0000);

    private static Bag _lastBag;
    private static BagMode _lastMode;

    private Bag _bag;
    private BagMode _mode;
    private Action<Item> _listener;

    private bool _chosen;

    // clicking an item with no listener opens its WndItem, GameScene does that
    public event Action<WndBag, Item> ItemClicked;

    // A tab replaces this window with one on another bag
    public event Action<WndBag> Reopened;

    public static WndBag Create(Bag bag, Action<Item> listener, BagMode mode, string title)
    {
        var wnd = new WndBag
        {
            Title = title ?? TextUtils.Capitalize(bag.Name()),
            Exclusive = true,
            Transient = true,
            Unresizable = true,
            WrapControls = true,
            _bag = bag,
            _listener = listener,
            _mode = mode,
        };
        _lastMode = mode;
        _lastBag = bag;
        wnd.CloseRequested += wnd.Hide;
        // windows/WndBag.java onBackPressed: any close that isn't a choice hands the listener null
        // VisibilityChanged covers ui_cancel and the title bar's close button, which both
        // just Hide() the window (GameScene.OpenWindow), as well as CloseRequested above
        wnd.VisibilityChanged += wnd.OnVisibilityChanged;
        wnd.Build();
        return wnd;
    }

    public static WndBag LastBag(Action<Item> listener, BagMode mode, string title)
    {
        var backpack = Dungeon.Hero.Belongings.Backpack;
        if (mode == _lastMode && _lastBag != null && backpack.Contains(_lastBag))
        {
            return Create(_lastBag, listener, mode, title);
        }

        return Create(backpack, listener, mode, title);
    }

    private void Build()
    {
        var root = new VBoxContainer();
        AddChild(root);

        var grid = new GridContainer { Columns = Cols };
        root.AddChild(grid);

        var stuff = Dungeon.Hero.Belongings;
        var backpack = _bag == stuff.Backpack;

        // equipped items
        AddSlot(grid, stuff.Weapon, ItemSpriteSheet.Weapon);
        AddSlot(grid, stuff.Armor, ItemSpriteSheet.Armor);
        AddSlot(grid, stuff.Ring1, ItemSpriteSheet.Ring);
        AddSlot(grid, stuff.Ring2, ItemSpriteSheet.Ring);
        var count = 4;

        if (!backpack)
        {
            // another bag's items start on the second row
            for (; count < Cols; count++)
            {
                grid.AddChild(new Control { CustomMinimumSize = new Vector2(ItemSlot.SlotSize, ItemSlot.SlotSize) });
            }
        }

        // items in teh bag
        foreach (var item in _bag.Items)
        {
            AddSlot(grid, item, -1);
            count++;
        }

        // free space
        var start = backpack ? 4 : Cols;
        while (count - start < _bag.Size)
        {
            AddSlot(grid, null, -1);
            count++;
        }

        // gold in backpack, in the grid's last cell
        if (backpack)
        {
            var cells = Belongings.BackpackSize + 4 + 1;
            var rows = cells / Cols + (cells % Cols > 0 ? 1 : 0);
            while (count < rows * Cols - 1)
            {
                AddSlot(grid, null, -1);
                count++;
            }

            AddSlot(grid, new Gold(Dungeon.Gold), -1);
        }

        var tabs = new HBoxContainer();
        root.AddChild(tabs);
        Bag[] bags =
        [
            stuff.Backpack,
            // TODO: SeedPouch tab
            // TODO: ScrollHolder tab
            // TODO: WandHolster tab,
            stuff.GetItem<Keyring>()
        ];
        foreach (var bag in bags)
        {
            if (bag == null)
            {
                continue;
            }

            var tab = new Button
            {
                Text = TextUtils.Capitalize(bag.Name()),
                Disabled = bag == _bag,
                FocusMode = Control.FocusModeEnum.None,
            };
            var target = bag;
            tab.Pressed += () =>
            {
                _chosen = true;
                Hide();
                Reopened?.Invoke(Create(target, _listener, _mode, _listener == null ? null : Title));
            };
            tabs.AddChild(tab);
        }
    }

    private void AddSlot(GridContainer grid, Item item, int placeholderImage)
    {
        var slot = ItemSlot.Create();
        grid.AddChild(slot);
        if (item == null)
        {
            if (placeholderImage >= 0)
            {
                slot.ShowPlaceholder(placeholderImage);
                slot.SetBackground(Equipped);
                slot.Enable(false);
            }
            else
            {
                slot.SetBackground(Normal);
                slot.Disabled = true;
            }

            return;
        }

        slot.Show(item);
        if (item is Gold)
        {
            slot.HideBackground();
        }
        else
        {
            var bg = item.IsEquipped(Dungeon.Hero) ? Equipped : Normal;
            if (item.Cursed && item.CursedKnown)
            {
                bg = new Color(bg.R + 0.2f, bg.G - 0.1f, bg.B); // bg.ra = +0.2f, bg.ga = -0.1f
            }

            // TODO: tint the slot background when !item.isIdentified()
            slot.SetBackground(bg);
        }

        if (_bag.Owner.IsAlive() && item.IsUpgradable() && item.LevelKnown)
        {
            AddDurability(slot, item);
        }

        slot.Enable(IsSelectable(item));
        slot.Pressed += () => OnSlotPressed(item);
        slot.RightClicked += _ => OnSlotRightClicked(item);
    }

    private static void AddDurability(ItemSlot slot, Item item)
    {
        // GameMath.gate(0, Math.round((float)NBARS * durability / maxDurability), NBARS), Math.round rounds half up
        var nBars = (int)GameMath.Gate(0,
            MathF.Floor((float)NBars * item.Durability() / item.MaxDurability() + 0.5f),
            NBars);
        for (var i = 0; i < NBars; i++)
        {
            slot.AddChild(new ColorRect
            {
                Color = i < nBars ? BarFull : BarEmpty,
                Position = new Vector2(2f + i * 6f, ItemSlot.SlotSize - 6f),
                Size = new Vector2(4f, 4f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
    }

    private bool IsSelectable(Item item)
    {
        switch (_mode)
        {
            case BagMode.All:
                return true;
            case BagMode.Quickslot:
                return item.DefaultAction != null;
            default:
                // TODO: UPGRADEABLE, UNIDENTIFIED< WEAPON, ARMOR, and ENCHANTABLE
                // TODO: WAND and SEED, FOR_SALE
                return false;
        }
    }

    // mode rule in WndBag.ItemButton.item()
    private void OnSlotPressed(Item item)
    {
        if (_listener != null)
        {
            _chosen = true;
            Hide();
            _listener(item);
        }
        else
        {
            ItemClicked?.Invoke(this, item);
        }
    }

    // windows/WndBag.java onBackPressed, listener.onSelect(null), then hide. Every close this
    // window did not already account for as a choice (ui_cancel, the title bar's close button,
    // CloseRequested) lands here instead, exactly once, since _chosen is set right before any
    // choice's own Hide() call above
    private void OnVisibilityChanged()
    {
        if (!Visible && !_chosen && _listener != null)
        {
            _chosen = true;
            _listener(null);
        }
    }

    // ItemButton.onLongClick. with no listener, put an item with a default action in quick slot 1
    private void OnSlotRightClicked(Item item)
    {
        if (_listener == null && item.DefaultAction != null)
        {
            Hide();
            QuickSlot.PrimaryValue = item.Stackable ? item.GetType() : item;
            QuickSlot.Refresh();
        }
    }
}