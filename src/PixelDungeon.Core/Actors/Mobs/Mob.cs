namespace PixelDungeon.Core.Actors.Mobs;

public abstract class Mob : Char
{
    public bool Hostile = true;

    public virtual bool Reset()
    {
        return false;
    }

    public virtual void Beckon(int cell)
    {
    }
}