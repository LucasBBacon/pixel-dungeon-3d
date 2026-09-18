namespace PixelDungeon.Core.View;

public interface ICharView
{
    void Place(int cell);
    void Move(int from, int to);
    void InterruptMotion();
    void Operate(int cell);
    void TurnTo(int from, int to);
    void Idle();
    void Die();
    void ShowStatus(StatusColor color, string text);
    void Burst(uint color, int n);
    void BloodBurst(int damage);
    void Flash();
    bool Visible { get; set; }
    bool IsMoving { get; }
}