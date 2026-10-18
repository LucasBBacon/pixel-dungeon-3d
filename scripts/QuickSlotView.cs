using Godot;
using System;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Client;

public partial class QuickSlotView : Control
{
    private const string TxtSelectItem = "Select an item for the quickslot";

    private bool _primary;
    private ItemSlot _slot;
    private Item _itemInSlot;
    private bool _enabled = true;

    public event Action<QuickSlotView> ChooseRequested;

    public string SelectPrompt => TxtSelectItem;

    public static QuickSlotView Create(bool primary)
    {
        var view = new QuickSlotView
        {
            _primary = primary,
            CustomMinimumSize = new Vector2(ItemSlot.SlotSize, ItemSlot.SlotSize),
        };
        view._slot = ItemSlot.Create();
        view.AddChild(view._slot);
        view._slot.Pressed += view.OnSlotPressed;
        view._slot.RightClicked += _ => view.RequestChoose();
        view.GuiInput += view.OnGuiInput;
        return view;
    }

    // QuickSlot.select(), an instance, the first held item of a slotted class, or a virtual one.
    private Item Select()
    {
        var content = _primary ? QuickSlot.PrimaryValue : QuickSlot.SecondaryValue;
        switch (content)
        {
            case Item item:
                return item;
            case Type type:
            {
                Item held = Dungeon.Hero.Belongings.GetItem(type);
                return held ?? Item.Virtual(type);
            }
            default:
                return null;
        }
    }

    // QuickSlot.onSelect, stackable are slotted by class, everything else by instance.
    public void Assign(Item item)
    {
        if (item == null)
        {
            return;
        }

        if (_primary)
        {
            QuickSlot.PrimaryValue = item.Stackable ? item.GetType() : item;
        }
        else
        {
            QuickSlot.SecondaryValue = item.Stackable ? item.GetType() : item;
        }

        QuickSlot.Refresh();
    }

    // QuickSlot.refresh() → item(select())
    public void Refresh()
    {
        if (Dungeon.Hero == null)
        {
            return;
        }

        _itemInSlot = Select();
        _slot.Show(_itemInSlot);
        EnableSlot();
    }

    public void SetEnabled(bool value)
    {
        if (_enabled != value)
        {
            _enabled = value;
            EnableSlot();
        }
    }

    // QuickSlot.enableSlot: usable only while the item is actually held.
    private void EnableSlot()
    {
        var usable = _itemInSlot != null
                     && _itemInSlot.Quantity() > 0
                     && (Dungeon.Hero.Belongings.Backpack.Contains(_itemInSlot) ||
                         _itemInSlot.IsEquipped(Dungeon.Hero));
        _slot.Enable(_enabled && usable);
    }

    // The slot's onClick.
    private void OnSlotPressed()
    {
        // TODO: useTargeting() — with a target locked, a click throws at QuickSlot.LastTarget and draws the crosshair
        Select()?.Execute(Dungeon.Hero);
    }

    // Keys 1 and 2.
    public void Use()
    {
        if (_enabled && !_slot.Disabled)
        {
            OnSlotPressed();
        }
    }

    // QuickSlot.onClick: a click on an empty or unusable slot chooses an item.
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left &&
            _slot.Disabled)
        {
            RequestChoose();
            AcceptEvent();
        }
    }

    private void RequestChoose()
    {
        if (_enabled)
        {
            ChooseRequested?.Invoke(this);
        }
    }
}