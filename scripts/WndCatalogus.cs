using System;
using System.Collections.Generic;
using Godot;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Client;

public partial class WndCatalogus : Window
{
    private const string TxtPotions = "Potions";
    private const string TxtScrolls = "Scrolls";
    private const string TxtTitle = "Catalogus";

    private const float Width = 256f; // WIDTH_L 128 in java
    private const float Height = 256f; // HEIGHT_L 128 in java
    private const float ItemHeight = 36f; // ITEM_HEIGHT 18 in java
    private const float Swatch = 24f;

    private static readonly Color UnknownColor = CharView.FromArgb(0xFFCCCCCC);

    private static bool _showPotions = true;

    public event Action<WndCatalogus, Item> InfoRequested;

    public static WndCatalogus Create()
    {
        var wnd = new WndCatalogus
        {
            Title = TxtTitle,
            Exclusive = true,
            Transient = true,
            Unresizable = true,
            WrapControls = true,
        };
        wnd.CloseRequested += wnd.Hide;

        var tabs = new TabContainer { CustomMinimumSize = new Vector2(Width, Height) };
        tabs.AddChild(wnd.TabList(TxtPotions, Potion.GetKnown(), Potion.GetUnknown()));
        tabs.AddChild(wnd.TabList(TxtScrolls, Scroll.GetKnown(), Scroll.GetUnknown()));
        tabs.Ready += () => tabs.CurrentTab = _showPotions ? 0 : 1;
        wnd.AddChild(tabs);

        return wnd;
    }

    private Control TabList(string title, List<Type> known, List<Type> unknown)
    {
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach (var type in known)
        {
            rows.AddChild(Row(type));
        }

        foreach (var type in unknown)
        {
            rows.AddChild(Row(type));
        }

        var list = new ScrollContainer { Name = title, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        list.AddChild(rows);
        return list;
    }

    private Control Row(Type type)
    {
        var item = (Item)Activator.CreateInstance(type);
        var identified = item.IsIdentified();

        var row = new Button
        {
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0f, ItemHeight),
            MouseFilter = identified
                ? Control.MouseFilterEnum.Stop
                : Control.MouseFilterEnum.Ignore, // unknown row returns false
        };

        var content = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        row.AddChild(content);

        content.AddChild(new ColorRect
        {
            Color = ItemVisuals.ColorOf(identified ? item.Image() : ItemSpriteSheet.Smth),
            CustomMinimumSize = new Vector2(Swatch, Swatch),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });

        var label = new Label
        {
            Text = identified ? item.Name() : item.TrueName(),
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        if (!identified)
        {
            label.AddThemeColorOverride("font_color", UnknownColor);
        }

        content.AddChild(label);

        if (identified)
        {
            row.Pressed += () => InfoRequested?.Invoke(this, item);
        }

        return row;
    }

    public static AcceptDialog InfoWindow(Item item)
    {
        return new AcceptDialog
        {
            Title = TextUtils.Capitalize(item.Name()),
            DialogText = item.Info(),
            OkButtonText = "OK",
            Exclusive = true,
        };
    }
}