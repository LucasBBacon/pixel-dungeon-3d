using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Actors.Mobs;

public abstract class Mob : Char
{
    private const string TxtDied = "You hear something died in the distance";

    protected const string TxtEcho = "echo of ";

    protected const string TxtNotice1 = "?!";
    protected const string TxtRage = "#$%^";
    protected const string TxtExp = "{0:+#;-#;+0}EXP";

    public AiState SleepingState;
    public AiState HuntingState;
    public AiState WanderingState;
    public AiState FleeingState;
    public AiState PassiveState;
    public AiState State;

    protected int Target = -1;

    protected int DefenseSkillValue = 0;

    protected int Exp = 1;
    protected int MaxLvl = 30;

    protected Char Enemy;
    protected bool EnemySeen;
    protected bool Alerted = false;

    protected const float TimeToWakeUp = 1f;

    public bool Hostile = true;

    private const string TagState = "state";
    private const string TagTarget = "target";

    protected Mob()
    {
        SleepingState = new Sleeping(this);
        HuntingState = new Hunting(this);
        WanderingState = new Wandering(this);
        FleeingState = new Fleeing(this);
        PassiveState = new Passive(this);
        State = SleepingState;
    }

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);

        var state = bundle.GetString(TagState);
        if (state == Sleeping.Tag) State = SleepingState;
        else if (state == Wandering.Tag) State = WanderingState;
        else if (state == Hunting.Tag) State = HuntingState;
        else if (state == Fleeing.Tag) State = FleeingState;
        else if (state == Passive.Tag) State = PassiveState;

        Target = bundle.GetInt(TagTarget);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);

        if (State == SleepingState) bundle.Put(TagState, Sleeping.Tag);
        else if (State == WanderingState) bundle.Put(TagState, Wandering.Tag);
        else if (State == HuntingState) bundle.Put(TagState, Hunting.Tag);
        else if (State == FleeingState) bundle.Put(TagState, Fleeing.Tag);
        else if (State == PassiveState) bundle.Put(TagState, Passive.Tag);

        bundle.Put(TagTarget, Target);
    }

    protected override bool Act()
    {
        base.Act();

        var justAlerted = Alerted;
        Alerted = false;

        Sprite.HideAlert();

        if (Paralysed)
        {
            EnemySeen = false;
            Spend(Tick);
            return true;
        }

        Enemy = ChooseEnemy();

        var enemyInFov = Enemy != null &&
                         Enemy.IsAlive() &&
                         Level.FieldOfView[Enemy.Pos] &&
                         Enemy.Invisible <= 0;

        return State.Act(enemyInFov, justAlerted);
    }

    protected virtual Char ChooseEnemy()
    {
        if (GetBuff<Amok>() != null)
        {
            if (Enemy == Dungeon.Hero || Enemy == null)
            {
                var enemies = new HashSet<Mob>();
                foreach (var mob in Dungeon.Level.Mobs)
                {
                    if (mob != this && Level.FieldOfView[mob.Pos])
                    {
                        enemies.Add(mob);
                    }
                }

                if (enemies.Count > 0)
                {
                    return Random.Element(enemies);
                }
            }
        }

        var terror = GetBuff<Terror>();
        if (terror != null)
        {
            if (Actor.FindById(terror.Object) is Char source)
            {
                return source;
            }
        }

        return Enemy != null && Enemy.IsAlive() ? Enemy : Dungeon.Hero;
    }

    protected virtual bool MoveSprite(int from, int to)
    {
        if (Sprite.Visible && (Dungeon.Visible[from] || Dungeon.Visible[to]))
        {
            Sprite.Move(from, to);
            return true;
        }
        else
        {
            Sprite.Place(to);
            return true;
        }
    }

    public override void Add(Buff buff)
    {
        base.Add(buff);

        if (buff is Amok)
        {
            Sprite.ShowStatus(StatusColor.Negative, TxtRage);
            State = HuntingState;
        }
        else if (buff is Terror)
        {
            State = FleeingState;
        }
        else if (buff is Sleep)
        {
            // TODO: new Flare(4, 32).color(0x44ffff, true).show(sprite, 2f)
            State = SleepingState;
            Postpone(Sleep.SWS);
        }
    }

    public override void Remove(Buff buff)
    {
        base.Remove(buff);

        if (buff is Terror)
        {
            Sprite.ShowStatus(StatusColor.Negative, TxtRage);
            State = HuntingState;
        }
    }

    protected virtual bool CanAttack(Char enemy) => Level.Adjacent(Pos, enemy.Pos) && !IsCharmedBy(enemy);

    protected virtual bool GetCloser(int target)
    {
        if (Rooted)
        {
            return false;
        }

        var step = Dungeon.FindPath(this,
            Pos,
            target,
            Level.Passable,
            Level.FieldOfView);
        if (step != -1)
        {
            Move(step);
            return true;
        }

        return false;
    }

    protected virtual bool GetFurther(int target)
    {
        var step = Dungeon.Flee(this,
            Pos,
            target,
            Level.Passable,
            Level.FieldOfView);
        if (step != -1)
        {
            Move(step);
            return true;
        }

        return false;
    }

    public override void Move(int step)
    {
        base.Move(step);

        if (!Flying)
        {
            Dungeon.Level.MobPress(this);
        }
    }

    protected virtual float AttackDelay() => 1f;

    protected virtual bool DoAttack(Char enemy)
    {
        var visible = Dungeon.Visible[Pos];

        if (visible)
        {
            Sprite.Attack(enemy.Pos);
        }
        else
        {
            Attack(enemy);
        }

        Spend(AttackDelay());

        // returning false when visible is the animation gate
        // scheduler stops until the view calls OnAttackComplete
        return !visible;
    }

    public override void OnAttackComplete()
    {
        Attack(Enemy);
        base.OnAttackComplete();
    }

    public override int DefenseSkill(Char enemy)
    {
        return EnemySeen && !Paralysed ? DefenseSkillValue : 0;
    }

    public override int DefenseProc(Char enemy, int damage)
    {
        // TODO: assassin hero striking unaware mob adds Random.Int(1, damage) and shows a Wound
        return damage;
    }

    public void Aggro(Char ch)
    {
        Enemy = ch;
    }

    public override void Damage(int dmg, object src)
    {
        Terror.Recover(this);

        if (State == SleepingState)
        {
            State = WanderingState;
        }

        Alerted = true;

        base.Damage(dmg, src);
    }

    public override void Destroy()
    {
        base.Destroy();

        Dungeon.Level.Mobs.Remove(this);

        if (Dungeon.Hero.IsAlive())
        {
            if (Hostile)
            {
                Statistics.EnemiesSlain++;
                // TODO: Badges.ValidateMonstersSlain()
                Statistics.QualifiedForNoKilling = false;

                if (Dungeon.NightMode)
                {
                    Statistics.NightHunt++;
                }
                else
                {
                    Statistics.NightHunt = 0;
                }
                // TODO: Badges.ValidateNightHunter()
            }

            var exp = GetExp();
            if (exp > 0)
            {
                Dungeon.Hero.Sprite.ShowStatus(StatusColor.Positive, TextUtils.Format(TxtExp, exp));
                Dungeon.Hero.EarnExp(exp);
            }
        }
    }

    public int GetExp() => Dungeon.Hero.Lvl <= MaxLvl ? Exp : 0;

    public override void Die(object src)
    {
        base.Die(src);

        if (Dungeon.Hero.Lvl <= MaxLvl + 2)
        {
            DropLoot();
        }

        if (Dungeon.Hero.IsAlive() && !Dungeon.Visible[Pos])
        {
            GLog.I(TxtDied);
        }
    }

    protected object Loot = null;
    protected float LootChance = 0;

    protected virtual void DropLoot()
    {
        if (Loot != null && Random.Float() < LootChance)
        {
            Item item;
            if (Loot is Generator.Category category)
            {
                item = Generator.Random(category);
            }
            else if (Loot is Type type)
            {
                // TODO: Generator.Random(class<? extends Item>) picks from class table
                item = Generator.Random();
            }
            else
            {
                item = (Item)Loot;
            }

            // TODO: .sprite.drop() animate heap
            Dungeon.Level.Drop(item, Pos);
        }
    }

    public virtual bool Reset()
    {
        return false;
    }

    public virtual void Beckon(int cell)
    {
        Notice();

        if (State != HuntingState)
        {
            State = WanderingState;
        }

        Target = cell;
    }

    public virtual string Description()
    {
        return "Real description is coming soon!";
    }

    public void Notice()
    {
        Sprite.ShowAlert();
    }

    public void Yell(string str)
    {
        GLog.N("{0}: \"{1}\" ", Name, str);
    }

    public interface AiState
    {
        bool Act(bool enemyInFov, bool justAlerted);
        string Status();
    }

    private sealed class Sleeping : AiState
    {
        public const string Tag = "Sleeping";

        private readonly Mob _mob;
        public Sleeping(Mob mob) => _mob = mob;

        public bool Act(bool enemyInFov, bool justAlerted)
        {
            if (enemyInFov &&
                Random.Int(
                    _mob.Distance(_mob.Enemy) +
                    _mob.Enemy.Stealth() +
                    (_mob.Enemy.Flying ? 2 : 0)
                ) == 0
               )
            {
                _mob.EnemySeen = true;

                _mob.Notice();
                _mob.State = _mob.HuntingState;
                _mob.Target = _mob.Enemy.Pos;

                if (Dungeon.IsChallenged(Challenges.SwarmIntelligence))
                {
                    foreach (var mob in Dungeon.Level.Mobs)
                    {
                        if (mob != _mob)
                        {
                            mob.Beckon(_mob.Target);
                        }
                    }
                }

                _mob.Spend(TimeToWakeUp);
            }
            else
            {
                _mob.EnemySeen = false;
                _mob.Spend(Tick);
            }

            return true;
        }

        public string Status() => TextUtils.Format("This {0} is sleeping", _mob.Name);
    }

    private sealed class Hunting : AiState
    {
        public const string Tag = "Hunting";

        private readonly Mob _mob;
        public Hunting(Mob mob) => _mob = mob;

        public bool Act(bool enemyInFov, bool justAlerted)
        {
            _mob.EnemySeen = enemyInFov;
            if (enemyInFov && _mob.CanAttack(_mob.Enemy))
            {
                return _mob.DoAttack(_mob.Enemy);
            }
            else
            {
                if (enemyInFov)
                {
                    _mob.Target = _mob.Enemy.Pos;
                }

                var oldPos = _mob.Pos;
                if (_mob.Target != -1 && _mob.GetCloser(_mob.Target))
                {
                    _mob.Spend(1 / _mob.Speed());
                    return _mob.MoveSprite(oldPos, _mob.Pos);
                }
                else
                {
                    _mob.Spend(Tick);
                    _mob.State = _mob.WanderingState;
                    _mob.Target = Dungeon.Level.RandomDestination();
                    return true;
                }
            }
        }

        public string Status() => TextUtils.Format("This {0} is hunting", _mob.Name);
    }

    private sealed class Wandering : AiState
    {
        public const string Tag = "Wandering";

        private readonly Mob _mob;
        public Wandering(Mob mob) => _mob = mob;

        public bool Act(bool enemyInFov, bool justAlerted)
        {
            if (enemyInFov &&
                (justAlerted ||
                 Random.Int(_mob.Distance(_mob.Enemy) / 2 + _mob.Enemy.Stealth()
                 ) == 0)
               )
            {
                _mob.EnemySeen = true;

                _mob.Notice();
                _mob.State = _mob.HuntingState;
                _mob.Target = _mob.Enemy.Pos;
            }
            else
            {
                _mob.EnemySeen = false;

                var oldPos = _mob.Pos;
                if (_mob.Target != -1 && _mob.GetCloser(_mob.Target))
                {
                    _mob.Spend(1 / _mob.Speed());
                    return _mob.MoveSprite(oldPos, _mob.Pos);
                }
                else
                {
                    _mob.Target = Dungeon.Level.RandomDestination();
                    _mob.Spend(Tick);
                }
            }

            return true;
        }

        public string Status()
        {
            return TextUtils.Format("This {0} is wandering", _mob.Name);
        }
    }

    private class Fleeing : AiState
    {
        public const string Tag = "Fleeing";

        protected readonly Mob Mob;
        public Fleeing(Mob mob) => Mob = mob;


        public bool Act(bool enemyInFov, bool justAlerted)
        {
            Mob.EnemySeen = enemyInFov;
            if (enemyInFov)
            {
                Mob.Target = Mob.Enemy.Pos;
            }

            var oldPos = Mob.Pos;
            if (Mob.Target != -1 && Mob.GetFurther(Mob.Target))
            {
                Mob.Spend(1 / Mob.Speed());
                return Mob.MoveSprite(oldPos, Mob.Pos);
            }
            else
            {
                Mob.Spend(Tick);
                NowhereToRun();
                return true;
            }
        }

        protected virtual void NowhereToRun()
        {
        }

        public string Status()
        {
            return TextUtils.Format("This {0} is fleeing", Mob.Name);
        }
    }

    private sealed class Passive : AiState
    {
        public const string Tag = "Passive";

        private readonly Mob _mob;
        public Passive(Mob mob) => _mob = mob;

        public bool Act(bool enemyInFov, bool justAlerted)
        {
            _mob.EnemySeen = false;
            _mob.Spend(Tick);
            return true;
        }

        public string Status()
        {
            return TextUtils.Format("This {0} is passive", _mob.Name);
        }
    }
}