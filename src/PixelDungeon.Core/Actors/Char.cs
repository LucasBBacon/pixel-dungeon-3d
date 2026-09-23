using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Actors;

public abstract class Char : Actor
{
    protected const string TxtHit = "{0} hit {1}";
    protected const string TxtKill = "{0} killed you...";
    protected const string TxtDefeat = "{0} defeated {1}";

    private const string TxtYouMissed = "{0} {1} your attack";
    private const string TxtSmbMissed = "{0} {1} {2}'s attack";

    public int Pos = 0;

    public ICharView Sprite = NullCharView.Instance;

    public string Name = "mob";

    public int HP;
    public int HT;

    protected float BaseSpeed = 1;

    public bool Paralysed = false;
    public bool Rooted = false;
    public bool Flying = false;
    public int Invisible = 0;

    public int ViewDistance = 8;

    private readonly HashSet<Buff> _buffs = [];

    protected override bool Act()
    {
        Dungeon.Level.UpdateFieldOfView(this);
        return false;
    }

    private const string TagPos = "pos";
    private const string TagHp = "HP";
    private const string TagHt = "HT";
    private const string TagBuffs = "buffs";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);

        Pos = bundle.GetInt(TagPos);
        HP = bundle.GetInt(TagHp);
        HT = bundle.GetInt(TagHt);

        foreach (var b in bundle.GetCollection(TagBuffs))
        {
            if (b != null)
            {
                ((Buff)b).AttachTo(this);
            }
        }
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);

        bundle.Put(TagPos, Pos);
        bundle.Put(TagHp, HP);
        bundle.Put(TagHt, HT);
        bundle.Put(TagBuffs, _buffs);
    }

    public virtual bool Attack(Char enemy)
    {
        var visibleFight = Dungeon.Visible[Pos] || Dungeon.Visible[enemy.Pos];

        if (Hit(this, enemy, false))
        {
            if (visibleFight)
            {
                GLog.I(TxtHit, Name, enemy.Name);
            }

            var dr = Random.IntRange(0, enemy.Dr());
            var dmg = DamageRoll();
            var effectiveDamage = Math.Max(dmg - dr, 0);

            effectiveDamage = AttackProc(enemy, effectiveDamage);
            effectiveDamage = enemy.DefenseProc(this, effectiveDamage);
            enemy.Damage(effectiveDamage, this);

            if (visibleFight)
            {
                Sample.Play(Assets.SndHit, Random.Float(0.8f, 1.25f));
            }

            if (enemy == Dungeon.Hero)
            {
                Dungeon.Hero.Interrupt();
                if (effectiveDamage > enemy.HT / 4)
                {
                    GameScene.Shake(GameMath.Gate(1,
                        effectiveDamage / (enemy.HT / 4),
                        5), 0.3f);
                }
            }

            enemy.Sprite.BloodBurst(effectiveDamage);
            enemy.Sprite.Flash();

            if (!enemy.IsAlive() && visibleFight)
            {
                if (enemy == Dungeon.Hero)
                {
                    if (Bestiary.IsBoss(this))
                    {
                        Dungeon.Fail(TextUtils.Format(ResultDescriptions.Boss, Name, Dungeon.Depth));
                    }
                    else
                    {
                        Dungeon.Fail(
                            TextUtils.Format(ResultDescriptions.Mob, TextUtils.Indefinite(Name), Dungeon.Depth));
                    }

                    GLog.N(TxtKill, Name);
                }
                else
                {
                    GLog.I(TxtDefeat, Name, enemy.Name);
                }
            }

            return true;
        }
        else
        {
            if (visibleFight)
            {
                var defense = enemy.DefenseVerb();
                enemy.Sprite.ShowStatus(StatusColor.Neutral, defense);
                if (this == Dungeon.Hero)
                {
                    GLog.I(TxtYouMissed, enemy.Name, defense);
                }
                else
                {
                    GLog.I(TxtSmbMissed, enemy.Name, defense, Name);
                }

                Sample.Play(Assets.SndMiss);
            }

            return false;
        }
    }

    public static bool Hit(Char attacker, Char defender, bool magic)
    {
        var acuRoll = Random.Float(attacker.AttackSkill(defender));
        var defRoll = Random.Float(defender.DefenseSkill(attacker));
        return (magic ? acuRoll * 2 : acuRoll) >= defRoll;
    }

    public virtual int AttackSkill(Char target) => 0;

    public virtual int DefenseSkill(Char enemy) => 0;

    public virtual string DefenseVerb() => "dodged";

    public virtual int Dr() => 0;

    public virtual int DamageRoll() => 1;

    public virtual int AttackProc(Char enemy, int damage) => damage;

    public virtual int DefenseProc(Char enemy, int damage) => damage;

    public virtual float Speed() => BaseSpeed;

    public virtual void Damage(int dmg, object src)
    {
        if (HP <= 0)
        {
            return;
        }

        var srcClass = src.GetType();
        if (Immunities().Contains(srcClass))
        {
            dmg = 0;
        }
        else if (Resistances().Contains(srcClass))
        {
            dmg = Random.IntRange(0, dmg);
        }

        HP -= dmg;
        if (dmg > 0 || src is Char)
        {
            Sprite.ShowStatus(HP > HT / 2
                    ? StatusColor.Warning
                    : StatusColor.Negative,
                dmg.ToString());
        }

        if (HP <= 0)
        {
            Die(src);
        }
    }

    public void Destroy()
    {
        HP = 0;
        Actor.Remove(this);
        Actor.FreeCell(Pos);
    }

    public virtual void Die(object src)
    {
        Destroy();
        Sprite.Die();
    }

    public bool IsAlive() => HP > 0;

    public override void Spend(float time)
    {
        var timeScale = 1f;
        base.Spend(time / timeScale);
    }

    public HashSet<Buff> Buffs() => _buffs;

    public HashSet<T> GetBuffs<T>() where T : Buff
    {
        var filtered = new HashSet<T>();
        foreach (var buff in _buffs)
        {
            if (buff is T t)
            {
                filtered.Add(t);
            }
        }

        return filtered;
    }

    public T GetBuff<T>() where T : Buff
    {
        foreach (var buff in _buffs)
        {
            if (buff is T t)
            {
                return t;
            }
        }

        return null;
    }

    public bool IsCharmedBy(Char ch) => false;

    public virtual void Add(Buff buff)
    {
        _buffs.Add(buff);
        Actor.Add(buff);
    }

    public virtual void Remove(Buff buff)
    {
        _buffs.Remove(buff);
        Actor.Remove(buff);
    }

    public void Remove<T>() where T : Buff
    {
        foreach (var buff in GetBuffs<T>())
        {
            Remove(buff);
        }
    }

    protected override void OnRemove()
    {
        foreach (var buff in _buffs.ToArray())
        {
            buff.Detach();
        }
    }

    public void UpdateSpriteState()
    {
    }

    public virtual int Stealth() => 0;

    public virtual void Move(int step)
    {
        if (Dungeon.Level.Map[Pos] == Terrain.OpenDoor)
        {
            Door.Leave(Pos);
        }

        Pos = step;

        if (Flying && Dungeon.Level.Map[Pos] == Terrain.Door)
        {
            Door.Enter(Pos);
        }

        if (this != Dungeon.Hero)
        {
            Sprite.Visible = Dungeon.Visible[Pos];
        }
    }

    public int Distance(Char other) => Level.Distance(Pos, other.Pos);

    public virtual void OnMotionComplete() => Next();

    public virtual void OnAttackComplete() => Next();

    public virtual void OnOperateComplete() => Next();

    private static readonly HashSet<Type> Empty = [];

    public virtual HashSet<Type> Resistances() => Empty;

    public virtual HashSet<Type> Immunities() => Empty;
}