using Godot;
using System;
using PixelDungeon.Core;

namespace PixelDungeon.Client;

public partial class Toolbar : CanvasLayer
{
    private Button _wait;
    private Button _search;
    private Button _examine;
    private Button _backpack;

    public Button Backpack => _backpack;
    public QuickSlotView Primary { get; private set; }
    public QuickSlotView Secondary { get; private set; }

    // set by GameScene.UpdateSelector.
    public bool Blocked;

    public Action OnExamine;
    public Action OnBackpack;
    public Action<QuickSlotView> OnChooseQuickSlot;

    public override void _Ready()
    {
        Layer = 4;

        var bar = new HBoxContainer();
        bar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        bar.OffsetTop = -64f;
        AddChild(bar);

        _wait = AddTool(bar, "Wait");
        _wait.Pressed += () => Dungeon.Hero.Rest(false);
        OnRightClick(_wait, () => Dungeon.Hero.Rest(true));

        _search = AddTool(bar, "Search");
        _search.Pressed += () => Dungeon.Hero.Search(true);

        _examine = AddTool(bar, "Examine");
        _examine.Pressed += () => OnExamine?.Invoke();

        bar.AddChild(new Control
            { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });

        _backpack = AddTool(bar, "Backpack");
        _backpack.Pressed += () => OnBackpack?.Invoke();
        OnRightClick(_backpack, () =>
        {
            // TODO: long-clicking the backpack button shows WndCatalogus
        });

        Secondary = QuickSlotView.Create(primary: false);
        bar.AddChild(Secondary);
        Primary = QuickSlotView.Create(primary: true);
        bar.AddChild(Primary);
        Primary.ChooseRequested += slot => OnChooseQuickSlot?.Invoke(slot);
        Secondary.ChooseRequested += slot => OnChooseQuickSlot?.Invoke(slot);
    }

    private static Button AddTool(HBoxContainer bar, string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(96f, 56f),
            FocusMode = Control.FocusModeEnum.None,
        };
        bar.AddChild(button);
        return button;
    }

    // Right-click stands in for the Java's long click.
    private static void OnRightClick(Button button, Action action)
    {
        button.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right }
                && !button.Disabled)
            {
                action();
                button.AcceptEvent();
            }
        };
    }

    // Toolbar.update: every tool follows Dungeon.hero.ready; the backpack stays usable after death.
    public override void _Process(double delta)
    {
        if (Dungeon.Hero == null)
        {
            return;
        }

        var enabled = Dungeon.Hero.Ready && !Blocked;
        _wait.Disabled = !enabled;
        _search.Disabled = !enabled;
        _examine.Disabled = !enabled;
        _backpack.Disabled = !enabled && (Dungeon.Hero.IsAlive() || Blocked);
        Primary.SetEnabled(enabled);
        Secondary.SetEnabled(enabled);
    }
}