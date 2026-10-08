namespace PixelDungeon.Core.View;

public interface ICharView
{
    void Place(int cell);
    void Move(int from, int to); // must call Char.OnMotionComplete() when motion ends
    void InterruptMotion(); // must leave IsMoving false and never call OnMotionComplete
    void Operate(int cell); // must call Char.OnOperateComplete() when done
    void Attack(int cell); // must call Char.OnAttackComplete() when the swing ends
    void Zap(int cell); // turn toward the cell and play zap
    void ShowAlert();
    void HideAlert();
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