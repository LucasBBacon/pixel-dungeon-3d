using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.View;

public class FakeCharView : ICharView
{
    public bool IsMovingFlag;
    public readonly List<(int From, int To)> Moves = [];
    public readonly List<int> Operations = [];
    public readonly List<(StatusColor Color, string Text)> Statuses = [];
    public readonly List<int> Placed = [];
    public int Interrupts;
    public int Bursts;
    public bool Died;
    public readonly List<int> Attacks = [];
    public readonly List<int> Zaps = [];
    public bool AlertShown;

    public void Place(int cell) => Placed.Add(cell);

    public void Move(int from, int to) => Moves.Add((from, to));


    public void InterruptMotion()
    {
        Interrupts++;
        IsMovingFlag = false;
    }

    public void Operate(int cell) => Operations.Add(cell);

    public void Attack(int cell) => Attacks.Add(cell);

    public void Zap(int cell) => Zaps.Add(cell);

    public void ShowAlert() => AlertShown = true;

    public void HideAlert() => AlertShown = false;

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