using System.Collections.Generic;
using PixelDungeon.Core;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;
using CoreScene = PixelDungeon.Core.Scenes.GameScene;

namespace PixelDungeon.Client;

public sealed class ExamineListener : ICellListener
{
    private const string TxtNothing = "There is nothing here.";

    // WndInfoItem
    private const string TxtChest = "Chest";
    private const string TxtLockedChest = "Locked chest";
    private const string TxtCrystalChest = "Crystal chest";
    private const string TxtTomb = "Tomb";
    private const string TxtSkeleton = "Skeletal remains";
    private const string TxtWontKnow = "You won't know what's inside until you open it!";
    private const string TxtNeedKey = TxtWontKnow + " But to open it you need a golden key.";
    private const string TxtInside = "You can see {0} inside, but to open the chest you need a golden key.";

    private const string TxtOwner =
        "This ancient tomb may contain something useful, " +
        "but its owner will most certainly object to checking.";

    private const string TxtRemains =
        "This is all that's left from one of your predecessors. " +
        "Maybe it's worth checking for any valuables.";

    public void OnSelect(int? target)
    {
        if (target == null)
        {
            return;
        }

        var cell = target.Value;

        if (cell < 0 || cell > Level.Length ||
            (!Dungeon.Level.Visited[cell] && !Dungeon.Level.Mapped[cell]))
        {
            CoreScene.Show(WindowRequest.Message("You don't know what is there."));
            return;
        }

        if (!Dungeon.Visible[cell])
        {
            CoreScene.Show(InfoCell(cell));
            return;
        }

        if (cell == Dungeon.Hero.Pos)
        {
            // TODO: GameScene.show(new WndHero())
        }
        else if (Actor.FindChar(cell) is Mob mob)
        {
            CoreScene.Show(InfoMob(mob));
            return;
        }

        var heap = Dungeon.Level.Heaps.GetValueOrDefault(cell);
        if (heap != null && heap.Type != HeapType.Hidden)
        {
            // TODO: a FOR_SALE heap of one priced item shows WndTradeItem(heap, false) instead
            CoreScene.Show(InfoHeap(heap));
            return;
        }

        // TODO: a plant on the cell shows WndInfoPlant
        CoreScene.Show(InfoCell(cell));
    }

    public string Prompt() => "Select a cell to examine";

    private WindowRequest InfoCell(int cell)
    {
        var tile = Dungeon.Level.Map[cell];
        if (Level.Water[cell])
        {
            tile = Terrain.Water;
        }
        else if (Level.Pit[cell])
        {
            tile = Terrain.Chasm;
        }

        var desc = Dungeon.Level.TileDesc(tile);
        // TODO: each blob present on the cell appends its tileDesc()
        return Info(Dungeon.Level.TileName(tile), desc.Length > 0 ? desc : TxtNothing);
    }

    private WindowRequest InfoMob(Mob mob)
    {
        // TODO: MobTitle's BuffIndicator row of the mob's buffs
        var body = $"HP {mob.HP}/{mob.HT}\n\n" + mob.Description() + "\n\n" + mob.State.Status() + ".";
        return Info(mob.Name, body);
    }

    private WindowRequest InfoHeap(Heap heap)
    {
        if (heap.Type is HeapType.Heap or HeapType.ForSale)
        {
            var item = heap.Peek();
            return Info(item.ToString(), item.Info());
        }

        string title;
        string info;

        if (heap.Type is HeapType.Chest or HeapType.Mimic)
        {
            title = TxtChest;
            info = TxtWontKnow;
        }
        else if (heap.Type == HeapType.Tomb)
        {
            title = TxtTomb;
            info = TxtOwner;
        }
        else if (heap.Type == HeapType.Skeleton)
        {
            title = TxtSkeleton;
            info = TxtRemains;
        }
        else if (heap.Type == HeapType.CrystalChest)
        {
            title = TxtCrystalChest;
            info = TextUtils.Format(TxtInside, TextUtils.Indefinite(heap.Peek().Name()));
        }
        else
        {
            title = TxtLockedChest;
            info = TxtNeedKey;
        }

        return Info(title, info);
    }

    private WindowRequest Info(string title, string body) =>
        new(TextUtils.Capitalize(title), body, ["OK"], null);
}