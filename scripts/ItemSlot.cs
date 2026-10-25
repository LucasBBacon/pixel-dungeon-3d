using System;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Weapon;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Client;

public partial class ItemSlot : Button
{
    public const float SlotSize = 56f;

    public static readonly Color Degraded = new(1f, 0.27f, 0.27f); // ItemSlot.DEGRADED 0xFF4444
    public static readonly Color Upgraded = new(0.27f, 1f, 0.27f); // ItemSlot.UPGRADED 0x44FF44
    public static readonly Color Warning = new(1f, 0.53f, 0f); // ItemSlot.WARNING 0xFF8800

    public static float ENABLED = 1.0f;
    public static float DISABLED = 0.3f;

    private const string TxtStrength = ":{0}";
    private const string TxtTypicalStr = "{0}?";

    // Key.status() ends in character 0x7F, the key glyph of the Java's bitmap font.
    private static readonly string KeyGlyph = ((char)0x7F).ToString();

    private ColorRect _swatch;
    private Label _topLeft;
    private Label _topRight;
    private Label _bottomRight;

    public Item Item { get; private set; }

    public event Action<ItemSlot> RightClicked;

    public static ItemSlot Create()
    {
        var slot = new ItemSlot
        {
            CustomMinimumSize = new Vector2(SlotSize, SlotSize),
            FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Pass,
        };

        slot._swatch = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        slot._swatch.SetAnchorsPreset(LayoutPreset.FullRect);
        slot._swatch.OffsetLeft = 14f;
        slot._swatch.OffsetTop = 14f;
        slot._swatch.OffsetRight = -14f;
        slot._swatch.OffsetBottom = -14f;
        slot.AddChild(slot._swatch);

        slot._topLeft = new Label { MouseFilter = MouseFilterEnum.Ignore };
        slot._topLeft.AddThemeFontSizeOverride("font_size", 12);
        slot.AddChild(slot._topLeft);

        slot._topRight = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        slot._topRight.SetAnchorsPreset(LayoutPreset.FullRect);
        slot._topRight.OffsetRight = -3f;
        slot._topRight.AddThemeFontSizeOverride("font_size", 12);
        slot.AddChild(slot._topRight);

        slot._bottomRight = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        slot._bottomRight.SetAnchorsPreset(LayoutPreset.FullRect);
        slot._bottomRight.OffsetRight = -3f;
        slot._bottomRight.AddThemeFontSizeOverride("font_size", 12);
        slot.AddChild(slot._bottomRight);

        slot.GuiInput += slot.OnGuiInput;
        slot.Show(null);
        return slot;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            RightClicked?.Invoke(this);
            AcceptEvent();
        }
    }

    public void Show(Item item)
    {
        Item = item;
        if (item == null)
        {
            _swatch.Visible = false;
            _topLeft.Text = "";
            _topRight.Text = "";
            _bottomRight.Text = "";
            return;
        }

        _swatch.Visible = true;
        _swatch.Color = ItemVisuals.ColorOf(item.Image());

        var status = item.Status();
        _topLeft.Text = status == null ? "" : status.Replace(KeyGlyph, "");

        var isArmor = item is Armor;
        var isWeapon = item is Weapon;
        if (isArmor || isWeapon)
        {
            // A missile is a Weapon but not a MeleeWeapon, show strength regardless of level
            if (item.LevelKnown || (isWeapon && item is not MeleeWeapon))
            {
                var str = isArmor ? ((Armor)item).Str : ((Weapon)item).Str;
                _topRight.Text = TextUtils.Format(TxtStrength, str);
                if (str > Dungeon.Hero.Str())
                {
                    _topRight.AddThemeColorOverride("font_color", Degraded);
                }
                else
                {
                    _topRight.RemoveThemeColorOverride("font_color");
                }
            }
            else
            {
                _topRight.Text = TextUtils.Format(TxtTypicalStr,
                    isArmor
                        ? ((Armor)item).TypicalStr()
                        : ((MeleeWeapon)item).TypicalStr());
                _topRight.AddThemeColorOverride("font_color", Warning);
            }
        }
        else
        {
            _topRight.Text = "";
        }

        var level = item.VisiblyUpgraded();
        if (level != 0 || (item.Cursed && item.CursedKnown))
        {
            _bottomRight.Text = item.LevelKnown ? level.ToString("+#;-#;+0") : "";
            _bottomRight.AddThemeColorOverride("font_color",
                level > 0
                    ? (item.IsBroken()
                        ? Warning
                        : Upgraded)
                    : Degraded);
        }
        else
        {
            _bottomRight.Text = "";
        }
    }

    // WndBag.Placeholder and ItemSlot.Skeleton/Tomb/Chest, an image with no item behind it
    public void ShowPlaceholder(int image)
    {
        Item = null;
        _swatch.Visible = true;
        _swatch.Color = ItemVisuals.ColorOf(image);
        _topLeft.Text = "";
        _topRight.Text = "";
        _bottomRight.Text = "";
    }

    public void SetBackground(Color color)
    {
        var flat = new StyleBoxFlat();
        AddThemeStyleboxOverride("normal", flat);
        AddThemeStyleboxOverride("hover", flat);
        AddThemeStyleboxOverride("disabled", flat);
        AddThemeStyleboxOverride("pressed",
            new StyleBoxFlat
            {
                BgColor = new Color(color.R * 1.5f,
                    color.G * 1.5f,
                    color.B * 1.5f)
            });
    }

    // ItemButton sets bg.visible = false behind gold slot.
    public void HideBackground()
    {
        var empty = new StyleBoxEmpty();
        AddThemeStyleboxOverride("pressed", empty);
        AddThemeStyleboxOverride("hover", empty);
        AddThemeStyleboxOverride("disabled", empty);
        AddThemeStyleboxOverride("pressed", empty);
    }

    public void Enable(bool value)
    {
        Disabled = !value;
        var alpha = new Color(1f, 1f, 1f, value ? ENABLED : DISABLED);
        _swatch.Modulate = alpha;
        _topLeft.Modulate = alpha;
        _topRight.Modulate = alpha;
        _bottomRight.Modulate = alpha;
    }
}