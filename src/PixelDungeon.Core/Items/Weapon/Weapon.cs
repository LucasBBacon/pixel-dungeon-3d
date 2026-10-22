using PixelDungeon.Core.Actors.Hero;

namespace PixelDungeon.Core.Items.Weapon;

public abstract class Weapon : KindOfWeapon
{
    public int Str = 10;
    public float Acu = 1;
    public float Dly = 1f;

    public enum Imbue
    {
        None,
        Speed,
        Accuracy
    }

    public Imbue ImbueValue = Imbue.None;
}