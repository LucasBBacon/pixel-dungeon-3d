using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Client;

public partial class WndHero : Window
{
    private const string TxtStats = "Stats";
    private const string TxtBuffs = "Buffs";

    private const string TxtExp = "Experience";
    private const string TxtStr = "Strength";
    private const string TxtHealth = "Health";
    private const string TxtGold = "Gold Collected";
    private const string TxtDepth = "Maximum Depth";

    private const string TxtTitle = "Level {0} {1}";

    private const float Width = 240f; // WndHero.WIDTH 100, in the Java's UI units
    private const float GAP = 10f;
    private const float Icon = 32f;

    private static readonly Color TitleColor = new(1f, 1f, 0.27f);

    public static WndHero Create()
    {
        var wnd = new WndHero
        {
            Exclusive = true,
            Transient = true,
            Unresizable = true,
            WrapControls = true,
        };
        wnd.CloseRequested += wnd.Hide;

        var hero = Dungeon.Hero;
        var tabs = new TabContainer { CustomMinimumSize = new Vector2(Width, 0f) };
        tabs.AddChild(StatsTab(hero));
        tabs.AddChild(BuffsTab(hero));
        tabs.CurrentTab = 0;
        wnd.AddChild(tabs);

        return wnd;
    }

    private static Control StatsTab(Hero hero)
    {
        var tab = new VBoxContainer { Name = TxtStats };

        var title = new Label
        {
            Text = TextUtils.Format(TxtTitle, hero.Lvl, hero.ClassName()).ToUpperInvariant()
        };
        title.AddThemeColorOverride("font_color", TitleColor);
        tab.AddChild(title);

        // TODO: Catalogus button under the title hides window and shows WndCatalogus
        // TODO: Journal button beside Catalogus hides this window and shows WndJournal

        tab.AddChild(Gap());

        StatSlot(tab, TxtStr, hero.Str().ToString());
        StatSlot(tab, TxtHealth, hero.HP + "/" + hero.HT);
        StatSlot(tab, TxtExp, hero.Exp + "/" + hero.MaxExp());

        tab.AddChild(Gap());

        StatSlot(tab, TxtGold, Statistics.GoldCollected.ToString());
        StatSlot(tab, TxtDepth, Statistics.DeepestFloor.ToString());

        tab.AddChild(Gap());

        return tab;
    }

    private static void StatSlot(VBoxContainer tab, string label, string value)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = label,
            CustomMinimumSize = new Vector2(Width * 0.65f, 0f)
        });
        row.AddChild(new Label { Text = value });
        tab.AddChild(row);
    }

    private static Control Gap()
    {
        return new Control
        {
            CustomMinimumSize = new Vector2(0f, GAP),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    private static Control BuffsTab(Hero hero)
    {
        var tab = new VBoxContainer { Name = TxtBuffs };
        foreach (var buff in hero.Buffs())
        {
            BuffSlot(tab, buff);
        }

        return tab;
    }

    private static void BuffSlot(VBoxContainer tab, Buff buff)
    {
        var index = buff.Icon();

        if (index != BuffIndicator.None)
        {
            var row = new HBoxContainer();
            row.AddChild(BuffIndicatorView.CreateTile(index, Icon));
            row.AddChild(new Label
            {
                Text = buff.ToString(),
                VerticalAlignment = VerticalAlignment.Center
            });
            tab.AddChild(row);
        }
    }
}