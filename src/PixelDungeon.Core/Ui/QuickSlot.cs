using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Mechanics;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Ui;

public static class QuickSlot
{
    // an item instance or type of stackable item
    public static object PrimaryValue;
    public static object SecondaryValue;

    // lastTarget is a static widget on Java
    // core holds it because Item.cast writes it
    public static Char LastTarget = null;

    // Toolbar.secondQuickslot() is a preference in java
    private const bool SecondQuickslot = true;

    public static bool UseTargeting()
    {
        var targeting = LastTarget != null && LastTarget.IsAlive() && Dungeon.Visible[LastTarget.Pos];

        if (targeting)
        {
            var pos = Ballistica.Cast(Dungeon.Hero.Pos,
                LastTarget.Pos,
                false,
                true);
            if (pos != LastTarget.Pos)
            {
                LastTarget = null;
                targeting = false;
            }
        }

        if (!targeting)
        {
            var n = Dungeon.Hero.VisibleEnemies();

            for (var i = 0; i < n; i++)
            {
                var enemy = Dungeon.Hero.VisibleEnemy(i);
                var pos = Ballistica.Cast(Dungeon.Hero.Pos,
                    enemy.Pos,
                    false,
                    true);
                if (pos == enemy.Pos)
                {
                    LastTarget = enemy;
                    targeting = true;
                    break;
                }
            }
        }

        // target still among the actors, Java puts crossM on its cell and shows crossB, view draws both
        if (targeting && !Actor.All().Contains(LastTarget))
        {
            LastTarget = null;
            targeting = false; // Deviation, Java leaves targeting on with lastTarget null, so its next click dereferences null
        }

        return targeting;
    }

    // refresh() re-resolves both buttons, only the view has buttons
    public static void Refresh()
    {
        GameScene.RefreshQuickSlots();
    }

    public static void Target(Item item, Char target)
    {
        if (target != Dungeon.Hero)
        {
            LastTarget = target;

            HealthIndicator.SetTarget(target);
        }
    }

    private const string TagQuickslot1 = "quickslot";

    private const string TagQuickslot2 = "quickslot2";

    // TODO: Dungeon.saveGame and loadGAme call Save(bundle) and Restore(bundle)

    public static void Save(Bundle bundle)
    {
        var stuff = Dungeon.Hero.Belongings;

        if (PrimaryValue is Type primary && stuff.GetItem(primary) != null)
        {
            bundle.Put(TagQuickslot1, primary.FullName);
        }

        if (SecondaryValue is Type secondary && stuff.GetItem(secondary) != null && SecondQuickslot)
        {
            bundle.Put(TagQuickslot2, secondary.FullName);
        }
    }

    public static void Restore(Bundle bundle)
    {
        PrimaryValue = null;
        SecondaryValue = null;

        // Class.forName in the Java
        // an unknown name leaves the slot empty, as its catch does.
        var qsClass = bundle.GetString(TagQuickslot1);
        if (qsClass != null)
        {
            PrimaryValue = Bundle.ResolveType(qsClass);
        }

        qsClass = bundle.GetString(TagQuickslot2);
        if (qsClass != null)
        {
            SecondaryValue = Bundle.ResolveType(qsClass);
        }
    }

    public static void Save(Bundle bundle, Item item)
    {
        if (ReferenceEquals(item, PrimaryValue))
        {
            bundle.Put(TagQuickslot1, true);
        }

        if (ReferenceEquals(item, SecondaryValue) && SecondQuickslot)
        {
            bundle.Put(TagQuickslot2, true);
        }
    }

    public static void Restore(Bundle bundle, Item item)
    {
        if (bundle.GetBoolean(TagQuickslot1))
        {
            PrimaryValue = item;
        }

        if (bundle.GetBoolean(TagQuickslot2))
        {
            SecondaryValue = item;
        }
    }

    public static void Compress()
    {
        if ((PrimaryValue == null && SecondaryValue != null) || (PrimaryValue == SecondaryValue))
        {
            PrimaryValue = SecondaryValue;
            SecondaryValue = null;
        }
    }
}