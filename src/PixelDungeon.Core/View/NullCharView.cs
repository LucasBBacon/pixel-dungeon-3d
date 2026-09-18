namespace PixelDungeon.Core.View;

public sealed class NullCharView : ICharView
{
    public static readonly NullCharView Instance = new();

    public void Place(int cell)
    {
    }

    public void Move(int from, int to)
    {
    }

    public void InterruptMotion()
    {
    }

    public void Operate(int cell)
    {
    }

    public void TurnTo(int from, int to)
    {
    }

    public void Idle()
    {
    }

    public void Die()
    {
    }

    public void ShowStatus(StatusColor color, string text)
    {
    }

    public void Burst(uint color, int n)
    {
    }

    public void BloodBurst(int damage)
    {
    }

    public void Flash()
    {
    }

    public bool Visible { get; set; } = true;
    public bool IsMoving => false;
}