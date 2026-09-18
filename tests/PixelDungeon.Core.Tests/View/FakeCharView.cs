using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class FakeCharView : ICharView
{
    public bool IsMovingFlag;
    public readonly List<(int From, int To)> Moves = new();
    public readonly List<int> Operations = new();
    public readonly List<(StatusColor Color, string Text)> Statuses = new();
    public readonly List<int> Placed = new();
    public int Interrupts;
    public int Bursts;
    public bool Died;

    public void Place(int cell) => Placed.Add(cell);

    public void Move(int from, int to) => Moves.Add((from, to));


    public void InterruptMotion()
    {
        Interrupts++;
        IsMovingFlag = false;
    }

    public void Operate(int cell) => Operations.Add(cell);


    public void TurnTo(int from, int to)
    {
    }

    public void Idle()
    {
    }

    public void Die() => Died = true;

    public void ShowStatus(StatusColor color, string text) => Statuses.Add((color, text));

    public void Burst(uint color, int n) => Bursts += n;

    public void BloodBurst(int damage)
    {
    }

    public void Flash()
    {
    }

    public bool Visible { get; set; } = true;
    public bool IsMoving => IsMovingFlag;
}