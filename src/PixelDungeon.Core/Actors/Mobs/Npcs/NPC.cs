using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Actors.Mobs.Npcs;

public abstract class Npc : Mob
{
    protected Npc()
    {
        HP = HT = 1;
        Exp = 0;

        Hostile = false;
        State = PassiveState;
    }

    protected void ThrowItem()
    {
        var heap = Dungeon.Level.Heaps.GetValueOrDefault(Pos);
        if (heap != null)
        {
            int n;
            do
            {
                n = Pos + Level.Neighbours8[Random.Int(8)];
            } while (!Level.Passable[n] && !Level.Avoid[n]);

            Dungeon.Level.Drop(heap.PickUp(), n).Sprite.Drop(Pos);
        }
    }

    public override void Beckon(int cell)
    {
    }

    public abstract void Interact();
}