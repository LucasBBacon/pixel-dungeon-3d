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
    void UpdateArmor(int tier); // HeroSprite.updateArmor(), show armor tier 0 (none) to 5; Armor's equip hooks call it
    void Read(); // HeroSprite.read(), must call Char.OnOperateComplete() when done, as Operate does
    void Add(CharState state); // CharSprite.add(State)
    void Remove(CharState state); // CharSprite.remove(State)
    void FadeIn(); // WandOfBlink.appear: alpha from 0 to 1 over 0.4 s
    bool Visible { get; set; }
    bool IsMoving { get; }
}