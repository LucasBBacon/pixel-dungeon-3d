using System.Collections.Generic;
using Godot;
using PixelDungeon.Core;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Sprites;

namespace PixelDungeon.Client;

public partial class LootIndicator : Control
{
    private ItemSlot _slot;

    public static LootIndicator Create()
    {
        var indicator = new LootIndicator
        {
            CustomMinimumSize = new Vector2(ItemSlot.SlotSize, ItemSlot.SlotSize),
            Visible = false,
        };
        indicator._slot = ItemSlot.Create();
        indicator.AddChild(indicator._slot);
        indicator._slot.Pressed += () => Dungeon.Hero.Handle(Dungeon.Hero.Pos);
        return indicator;
    }

    public override void _Process(double delta)
    {
        if (Dungeon.Hero == null || Dungeon.Level == null)
        {
            return;
        }

        if (Dungeon.Hero.Ready)
        {
            var heap = Dungeon.Level.Heaps.GetValueOrDefault(Dungeon.Hero.Pos);
            if (heap != null && heap.Type != HeapType.Hidden)
            {
                var image = heap.Type switch
                {
                    HeapType.Chest or HeapType.Mimic => ItemSpriteSheet.Chest,
                    HeapType.LockedChest => ItemSpriteSheet.LockedChest,
                    HeapType.Tomb => ItemSpriteSheet.Tomb,
                    HeapType.Skeleton => ItemSpriteSheet.Bones,
                    _ => -1,
                };
                if (image >= 0)
                {
                    _slot.ShowPlaceholder(image);
                }
                else
                {
                    _slot.Show(heap.Peek());
                }

                Visible = true;
            }
            else
            {
                Visible = false;
            }
        }

        _slot.Enable(Visible && Dungeon.Hero.Ready);
    }
}