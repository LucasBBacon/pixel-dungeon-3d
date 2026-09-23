namespace PixelDungeon.Core.Actors.Buffs;

public class Buff : Actor
{
    public Char Target;

    public virtual bool AttachTo(Char target)
    {
        if (target.Immunities().Contains(GetType()))
        {
            return false;
        }

        Target = target;
        target.Add(this);

        return true;
    }

    public virtual void Detach()
    {
        Target.Remove(this);
    }

    protected override bool Act()
    {
        Diactivate();
        return true;
    }

    public static T Append<T>(Char target) where T : Buff, new()
    {
        var buff = new T();
        buff.AttachTo(target);
        return buff;
    }

    public static T Append<T>(Char target, float duration) where T : FlavorBuff, new()
    {
        var buff = Append<T>(target);
        buff.Spend(duration);
        return buff;
    }

    public static T Affect<T>(Char target) where T : Buff, new()
    {
        var buff = target.GetBuff<T>();
        return buff ?? Append<T>(target);
    }

    public static T Affect<T>(Char target, float duration) where T : FlavorBuff, new()
    {
        var buff = Affect<T>(target);
        buff.Spend(duration);
        return buff;
    }

    public static T Prolong<T>(Char target, float duration) where T : FlavorBuff, new()
    {
        var buff = Affect<T>(target);
        buff.Postpone(duration);
        return buff;
    }

    public static void Detach(Buff buff) => buff?.Detach();

    public static void Detach<T>(Char target) where T : Buff => Detach(target.GetBuff<T>());
}