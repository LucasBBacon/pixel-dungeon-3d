namespace PixelDungeon.Core.Actors.Hero;

public class Hero : Char
{
    public const int StartingStr = 10;

    public HeroClass HeroClass = HeroClass.Rogue;
    public HeroSubClass SubClass = HeroSubClass.None;

    public bool Ready = false;

    public HeroAction CurAction = null;
    public HeroAction LastAction = null;

    public bool RestoreHealth = false;

    public int Str;
    public bool Weakened = false;

    public float Awareness;

    public int Lvl = 1;
    public int Exp = 0;

    public Hero()
    {
        Name = "you";

        HP = HT = 20;
        Str = StartingStr;
        Awareness = 0.1f;
    }

    public void Interrupt()
    {
        if (IsAlive() && CurAction != null && CurAction.Dst != Pos)
        {
            LastAction = CurAction;
        }

        CurAction = null;
    }

    public void Resume()
    {
        CurAction = LastAction;
        LastAction = null;
        Act();
    }

    public interface IDoom
    {
        void OnDeath();
    }
}