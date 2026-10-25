using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Client;

internal partial class WndItem : Window
{
    private static readonly Color TitleColor = new(1f, 1f, 0.27f);

    public static WndItem Create(WndBag owner, Item item)
    {
        var wnd = new WndItem
        {
            Title = TextUtils.Capitalize(item.ToString()),
            Exclusive = true,
            Transient = true,
            Unresizable = true,
            WrapControls = true,
        };
        if (item.LevelKnown)
        {
            if (item.Level() < 0)
            {
                wnd.AddThemeColorOverride("title_color", ItemSlot.Degraded);
            }
            else if (item.Level() > 0)
            {
                wnd.AddThemeColorOverride("title_color",
                    item.IsBroken()
                        ? ItemSlot.Warning
                        : ItemSlot.Upgraded);
            }
        }

        wnd.CloseRequested += wnd.Hide;

        var root = new VBoxContainer { CustomMinimumSize = new Vector2(320f, 0f) };
        wnd.AddChild(root);

        if (item.IsUpgradable() && item.LevelKnown)
        {
            root.AddChild(new ProgressBar
            {
                MinValue = 0,
                MaxValue = item.MaxDurability(),
                Value = item.Durability(),
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0f, 6f),
            });
        }

        root.AddChild(new Label
        {
            Text = item.Info(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(320f, 0f),
        });

        if (Dungeon.Hero.IsAlive() && owner != null)
        {
            var buttons = new HFlowContainer();
            root.AddChild(buttons);
            foreach (var action in item.Actions(Dungeon.Hero))
            {
                var button = new Button { Text = action, FocusMode = Control.FocusModeEnum.None };
                if (action == item.DefaultAction)
                {
                    button.AddThemeColorOverride("font_color", TitleColor);
                }

                var chosen = action;
                button.Pressed += () =>
                {
                    item.Execute(Dungeon.Hero, chosen);
                    wnd.Hide();
                    owner.Hide();
                };
                buttons.AddChild(button);
            }
        }

        return wnd;
    }
}