using Godot;
using System;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Client;

public partial class QuickSlotView : Control
{
    private const string TxtSelectItem = "Select an item for the quickslot";

    private static readonly Color CrossColor = new Color(1f, 0.25f, 0.25f);
    private const float CrossLift = 0.06f; // the ring's tube radius, so the ring rests on the floor

    private bool _primary;
    private ItemSlot _slot;
    private Item _itemInSlot;
    private bool _enabled = true;

    private bool _targeting = false;
    private Control _crossB; // badge centered on slot
    private MeshInstance3D _crossM; // ring on the target's cell

    // GameScene.handleCell(cell). GameScene sets it to CellSelector.HandleCell.
    public Action<int> HandleCell;

    public MeshInstance3D Crosshair => _crossM;

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

        // crossB = Icons.TARGET.get(), centred on slot and hidden
        // added after the ItemSlot, so it draws on top
        // ItemSlot.Enable dims only the slot's own children, so the badge keeps full alpha
        view._crossB = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        view._crossB.SetAnchorsPreset(LayoutPreset.FullRect);
        var badge = view._crossB;
        badge.Draw += () => DrawTarget(badge);
        view.AddChild(view._crossB);

        // crossM, flat ring about 1 cell across, hidden until targeting places it
        view._crossM = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.32f, OuterRadius = 0.44f },
            MaterialOverride = TerrainVisuals.Unshaded(CrossColor),
            Visible = false,
        };
        return view;
    }

    private static void DrawTarget(Control badge)
    {
        var c = badge.Size / 2f;
        badge.DrawArc(c, 12f, 0f, Mathf.Tau, 32, CrossColor, 2f);
        badge.DrawLine(c + new Vector2(0f, -20f), c + new Vector2(0f, -6f), CrossColor, 3f);
        badge.DrawLine(c + new Vector2(0f, 6f), c + new Vector2(0f, 20f), CrossColor, 3f);
        badge.DrawLine(c + new Vector2(-20f, 0f), c + new Vector2(-6f, 0f), CrossColor, 3f);
        badge.DrawLine(c + new Vector2(6f, 0f), c + new Vector2(20f, 0f), CrossColor, 3f);
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
        if (_targeting && QuickSlot.LastTarget != null)
        {
            HandleCell?.Invoke(QuickSlot.LastTarget.Pos);
        }
        else
        {
            UseTargeting();
            Select()?.Execute(Dungeon.Hero);
        }
    }

    private void UseTargeting()
    {
        _targeting = QuickSlot.UseTargeting();
        if (_targeting)
        {
            _crossM.Position = LevelRenderer.CellToWorld(QuickSlot.LastTarget.Pos) + new Vector3(0f, CrossLift, 0f);
            _crossM.Visible = true;
            _crossB.Visible = true;
        }
    }

    // Keys 1 and 2.
    public void CancelTargeting()
    {
        if (_targeting)
        {
            _crossB.Visible = false;
            _crossM.Visible = false;
            _targeting = false;
        }
    }

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