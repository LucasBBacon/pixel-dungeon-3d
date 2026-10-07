using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Actors.Mobs.Npcs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Actors.Hero;

public class Hero : Char
{
    private const string TxtLeave = "One does not simply leave Pixel Dungeon.";

    private const string TxtLevelUp = "level up!";

    private const string TxtNewLevel =
        "Welcome to level {0}! Now you are healthier and more focused. It's easier for you to hit enemies and dodge their attacks.";

    // TODO: TxtSomethingElse, TxtLockedChest, TxtLockedDoor
    private const string TxtNoticedSomething = "You noticed something";

    private const string TxtWait = "...";
    private const string TxtSearch = "search";

    public const int StartingStr = 10;

    private const float TimeToRest = 1f;
    private const float TimeToSearch = 2f;

    public HeroClass HeroClass = HeroClass.Rogue;
    public HeroSubClass SubClass = HeroSubClass.None;

    private int _attackSkill = 10;
    private int _defenseSkill = 5;

    public bool Ready = false;

    public HeroAction CurAction = null;
    public HeroAction LastAction = null;

    private Char _enemy;

    // TODO: Armor.Glyph
    // TODO: key item

    public bool RestoreHealth = false;

    public Belongings Belongings;

    public int STR;
    public bool Weakened = false;

    public float Awareness;

    public int Lvl = 1;
    public int Exp = 0;

    private List<Mob> _visibleEnemies;

    public Hero()
    {
        Name = "you";

        HP = HT = 20;
        STR = StartingStr;
        Awareness = 0.1f;

        Belongings = new Belongings(this);

        _visibleEnemies = [];
    }

    public int Str()
    {
        return Weakened ? STR - 2 : STR;
    }

    private const string TagAttack = "attackSkill";
    private const string TagDefense = "defenseSkill";
    private const string TagStrength = "STR";
    private const string TagLevel = "lvl";
    private const string TagExperience = "exp";

    public override void RestoreFromBundle(Bundle bundle)
    {
        base.RestoreFromBundle(bundle);

        HeroClass = HeroClasses.RestoreInBundle(bundle);
        SubClass = HeroSubClasses.RestoreInBundle(bundle);

        _attackSkill = bundle.GetInt(TagAttack);
        _defenseSkill = bundle.GetInt(TagDefense);

        STR = bundle.GetInt(TagStrength);
        UpdateAwareness();

        Lvl = bundle.GetInt(TagLevel);
        Exp = bundle.GetInt(TagExperience);

        Belongings.RestoreFromBundle(bundle);
    }

    public override void StoreInBundle(Bundle bundle)
    {
        base.StoreInBundle(bundle);

        HeroClass.StoreInBundle(bundle);
        SubClass.StoreInBundle(bundle);

        bundle.Put(TagAttack, _attackSkill);
        bundle.Put(TagDefense, _defenseSkill);

        bundle.Put(TagStrength, STR);

        bundle.Put(TagLevel, Lvl);
        bundle.Put(TagExperience, Exp);

        Belongings.StoreInBundle(bundle);
    }

    // TODO: Preview reads level

    public string ClassName()
    {
        return SubClass == HeroSubClass.None ? HeroClass.Title() : SubClass.Title();
    }

    public void Live()
    {
        Buff.Affect<Regeneration>(this);
        // TODO: Buff.affect(this, Hunger.Class)
    }

    public int Tier()
    {
        // TODO: belongings.armor.tier when armor is worn
        return 0;
    }

    public override int AttackSkill(Char target)
    {
        var bonus = 0;
        // TODO: ring of accuracy levels add to bonus
        var accuracy = (bonus == 0) ? 1 : (float)Math.Pow(1.4, bonus);
        // TODO: ranged weapon at distance 1 halves accuracy, accuracyFactor scales result
        return (int)(_attackSkill * accuracy);
    }

    public override int DefenseSkill(Char enemy)
    {
        var bonus = 0;
        // TODO: ring of evasion levels add to bonus
        var evasion = bonus == 0 ? 1 : (float)Math.Pow(1.2, bonus);

        if (Paralysed)
        {
            evasion /= 2;
        }

        var aEnc = 0;
        // TODO: aEnc = belongings.armor.STR - STR() when armor is worn
        if (aEnc > 0)
        {
            return (int)(_defenseSkill * evasion / Math.Pow(1.5, aEnc));
        }
        else
        {
            if (HeroClass == HeroClass.Rogue)
            {
                if (CurAction != null && SubClass == HeroSubClass.Freerunner && !IsStarving())
                {
                    evasion *= 2;
                }

                return (int)((_defenseSkill - aEnc) * evasion);
            }
            else
            {
                return (int)(_defenseSkill * evasion);
            }
        }
    }

    public override int Dr()
    {
        var dr = 0;
        // TODO: armor worn
        // TODO: barkskin level adds to dr
        return dr;
    }

    public override int DamageRoll()
    {
        // TODO: equipped weapon's DamageRoll(this) replaces bare-hands attack
        var dmg = Str() > 10 ? Random.IntRange(1, Str() - 9) : 1;
        // TODO: Fury multiplier x 1.5
        return dmg;
    }

    public override float Speed()
    {
        var aEnc = 0;
        // TODO: aEnc calculation with armor!
        if (aEnc > 0)
        {
            return (float)(base.Speed() * Math.Pow(1.3, -aEnc));
        }
        else
        {
            var speed = base.Speed();
            // TODO: sprinting freerunner
            return speed;
        }
    }

    public float AttackDelay()
    {
        // TODO: equipped weapon's speedFactor(this)
        return 1f;
    }

    public override void Spend(float time)
    {
        // TODO: ring of haste scales time by 1.1^-level
        base.Spend(time);
    }

    public void SpendAndNext(float time)
    {
        Busy();
        Spend(time);
        Next();
    }

    protected override bool Act()
    {
        base.Act();

        if (Paralysed)
        {
            CurAction = null;

            SpendAndNext(Tick);
            return false;
        }

        CheckVisibleMobs();
        // TODO: AttackIndicator.UpdateState()

        if (CurAction == null)
        {
            if (RestoreHealth)
            {
                if (IsStarving() || HP >= HT)
                {
                    RestoreHealth = false;
                }
                else
                {
                    Spend(TimeToRest);
                    Next();
                    return false;
                }
            }

            MakeReady();
            return false;
        }
        else
        {
            RestoreHealth = false;

            Ready = false;

            if (CurAction is HeroAction.Move move)
            {
                return ActMove(move);
            }
            // TODO: all other individual actions, interact, buy, pick up, open chest, unlock, descend, ascend, attack, cook

            else if (CurAction is HeroAction.Interact interact)
            {
                return ActInteract(interact);
            }

            else if (CurAction is HeroAction.Buy buy)
            {
                return ActBuy(buy);
            }

            else if (CurAction is HeroAction.PickUp pickUp)
            {
                return ActPickUp(pickUp);
            }

            else if (CurAction is HeroAction.OpenChest openChest)
            {
                return ActOpenChest(openChest);
            }

            else if (CurAction is HeroAction.Unlock unlock)
            {
                return ActUnlock(unlock);
            }

            else if (CurAction is HeroAction.Descend descend)
            {
                return ActDescend(descend);
            }

            else if (CurAction is HeroAction.Ascend ascend)
            {
                return ActAscend(ascend);
            }

            else if (CurAction is HeroAction.Attack attack)
            {
                return ActAttack(attack);
            }

            else if (CurAction is HeroAction.Cook cook)
            {
                return ActCook(cook);
            }
        }

        return false;
    }

    public void Busy()
    {
        Ready = false;
    }

    private void MakeReady()
    {
        Sprite.Idle();
        CurAction = null;
        Ready = true;

        GameScene.Ready();
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

    private bool ActMove(HeroAction.Move action)
    {
        if (GetCloser(action.Dst))
        {
            return true;
        }
        else
        {
            if (Dungeon.Level.Map[Pos] == Terrain.Sign)
            {
                Sign.Read(Pos);
            }

            MakeReady();
            return false;
        }
    }

    private bool ActInteract(HeroAction.Interact action)
    {
        // TODO: adjacent NPC: ready(), Sprite.TurnTo(Pos, Npc.Pos), Npc.Interact(), otherwise GetCloser()
        MakeReady();
        return false;
    }

    private bool ActBuy(HeroAction.Buy action)
    {
        // TODO: at the heap show WndTradeItem for a ForSale heap
        MakeReady();
        return false;
    }


    private bool ActCook(HeroAction.Cook action)
    {
        // TODO: AlchemyPot.Operate(this, dst)
        MakeReady();
        return false;
    }

    private bool ActPickUp(HeroAction.PickUp action)
    {
        // TODO: at heap pick up top item, log
        MakeReady();
        return false;
    }

    private bool ActOpenChest(HeroAction.OpenChest action)
    {
        // TODO: adjacent chest find key, Sprite.Operate
        MakeReady();
        return false;
    }

    private bool ActUnlock(HeroAction.Unlock action)
    {
        // TODO: adjacent door or exit, find key, Sprite.Operate
        MakeReady();
        return false;
    }

    private bool ActDescend(HeroAction.Descend action)
    {
        var stairs = action.Dst;
        if (Pos == stairs && Pos == Dungeon.Level.Exit)
        {
            CurAction = null;

            // TODO: Hunger.Satisfy(-HUnger.Starving / 10) unless starving

            Interlevel.Mode = InterlevelMode.Descend;
            GameScene.SwitchLevel(InterlevelMode.Descend);

            return false;
        }

        if (GetCloser(stairs))
        {
            return true;
        }

        MakeReady();
        return false;
    }

    private bool ActAscend(HeroAction.Ascend action)
    {
        var stairs = action.Dst;
        if (Pos == stairs && Pos == Dungeon.Level.Entrance)
        {
            if (Dungeon.Depth == 1)
            {
                // TODO: with amulet in belongings, win the game, delete the game, surface scene
                GameScene.Show(WindowRequest.Message(TxtLeave));
                MakeReady();
            }
            else
            {
                CurAction = null;

                // TODO: Hunger.Satisfy(-Hunger.Starving / 10) unless starving

                Interlevel.Mode = InterlevelMode.Ascend;
                GameScene.SwitchLevel(InterlevelMode.Ascend);
            }

            return false;
        }

        if (GetCloser(stairs))
        {
            return true;
        }

        MakeReady();
        return false;
    }

    private bool ActAttack(HeroAction.Attack action)
    {
        _enemy = action.Target;

        if (Level.Adjacent(Pos, _enemy.Pos) && _enemy.IsAlive() && !IsCharmedBy(_enemy))
        {
            Spend(AttackDelay());
            Sprite.Attack(_enemy.Pos);

            // false holds the scheduler until the view calls OnAttackComplete
            return false;
        }
        else
        {
            if (Level.FieldOfView[_enemy.Pos] && GetCloser(_enemy.Pos))
            {
                return true;
            }
            else
            {
                MakeReady();
                return false;
            }
        }
    }

    public void Rest(bool tillHealthy)
    {
        SpendAndNext(TimeToRest);
        if (!tillHealthy)
        {
            Sprite.ShowStatus(StatusColor.Default, TxtWait);
        }

        RestoreHealth = tillHealthy;
    }

    public override int AttackProc(Char enemy, int damage)
    {
        // TODO: weapon's proc, gladiator combo, battlemage...
        return damage;
    }

    public override int DefenseProc(Char enemy, int damage)
    {
        // TODO: ring of thorns, earthroot armour
        return damage;
    }

    public override void Damage(int dmg, object src)
    {
        RestoreHealth = false;
        base.Damage(dmg, src);

        // TODO: beserker gains fury below HT * Fury.Level
    }

    private void CheckVisibleMobs()
    {
        var visible = new List<Mob>();
        var newMob = false;

        foreach (var mob in Dungeon.Level.Mobs)
        {
            if (Level.FieldOfView[mob.Pos] && mob.Hostile)
            {
                visible.Add(mob);
                if (!_visibleEnemies.Contains(mob))
                {
                    newMob = true;
                }
            }
        }

        if (newMob)
        {
            Interrupt();
            RestoreHealth = false;
        }

        _visibleEnemies = visible;
    }

    public int VisibleEnemies() => _visibleEnemies.Count;

    public Mob VisibleEnemy(int index) => _visibleEnemies[index % _visibleEnemies.Count];

    private bool GetCloser(int target)
    {
        if (Rooted)
        {
            GameScene.Shake(1, 1f);
            return false;
        }

        var step = -1;
        if (Level.Adjacent(Pos, target))
        {
            if (Actor.FindChar(target) == null)
            {
                if (Level.Pit[target] && !Flying && !Chasm.JumpConfirmed)
                {
                    Chasm.HeroJump(this);
                    Interrupt();
                    return false;
                }

                if (Level.Passable[target] || Level.Avoid[target])
                {
                    step = target;
                }
            }
        }
        else
        {
            const int len = Level.Length;
            var p = Level.Passable;
            var v = Dungeon.Level.Visited;
            var m = Dungeon.Level.Mapped;
            var passable = new bool[len];
            for (var i = 0; i < len; i++)
            {
                passable[i] = p[i] && (v[i] || m[i]);
            }

            step = Dungeon.FindPath(this, Pos, target, passable, Level.FieldOfView);
        }

        if (step != -1)
        {
            var oldPos = Pos;
            Move(step);
            Sprite.Move(oldPos, Pos);
            Spend(1 / Speed());

            return true;
        }

        return false;
    }

    public bool Handle(int cell)
    {
        if (cell == -1)
        {
            return false;
        }

        Char ch;
        Heap heap;

        if (Dungeon.Level.Map[cell] == Terrain.Alchemy && cell != Pos)
        {
            CurAction = new HeroAction.Cook(cell);
        }
        else if (Level.FieldOfView[cell] && (ch = Actor.FindChar(cell)) is Mob)
        {
            if (ch is NPC npc)
            {
                CurAction = new HeroAction.Interact(npc);
            }
            else
            {
                CurAction = new HeroAction.Attack(ch);
            }
        }
        else if (Level.FieldOfView[cell] && (heap = Dungeon.Level.Heaps.GetValueOrDefault(cell)) != null &&
                 heap.Type != HeapType.Hidden)
        {
            switch (heap.Type)
            {
                case HeapType.Heap:
                    CurAction = new HeroAction.PickUp(cell);
                    break;
                case HeapType.ForSale:
                    CurAction = heap.Size() == 1 && heap.Peek().Price() > 0
                        ? new HeroAction.Buy(cell)
                        : new HeroAction.PickUp(cell);
                    break;
                default:
                    CurAction = new HeroAction.OpenChest(cell);
                    break;
            }
        }
        else if (Dungeon.Level.Map[cell] == Terrain.LockedDoor ||
                 Dungeon.Level.Map[cell] == Terrain.LockedExit)
        {
            CurAction = new HeroAction.Unlock(cell);
        }
        else if (cell == Dungeon.Level.Exit)
        {
            CurAction = new HeroAction.Descend(cell);
        }
        else if (cell == Dungeon.Level.Entrance)
        {
            CurAction = new HeroAction.Ascend(cell);
        }
        else
        {
            CurAction = new HeroAction.Move(cell);
            LastAction = null;
        }

        return Act();
    }

    public void EarnExp(int exp)
    {
        Exp += exp;

        var levelUp = false;
        while (Exp >= MaxExp())
        {
            Exp -= MaxExp();
            Lvl++;

            HT += 5;
            HP += 5;
            _attackSkill++;
            _defenseSkill++;

            if (Lvl < 10)
            {
                UpdateAwareness();
            }

            levelUp = true;
        }

        if (levelUp)
        {
            GLog.P(TxtNewLevel, Lvl);
            Sprite.ShowStatus(StatusColor.Positive, TxtLevelUp);
            Sample.Play(Assets.SndLevelUp);

            // TODO: Badges.ValidateLevelReached()
        }

        // TODO: warlock heals min(HT - HP, 1 + (depth - 1) / 5) and satisfies hunger by 10
    }

    public int MaxExp() => 5 + Lvl * 5;

    internal void UpdateAwareness() => Awareness =
        (float)(1 - Math.Pow(HeroClass == HeroClass.Rogue ? 0.85 : 0.9, (1 + Math.Min(Lvl, 9)) * 0.5));


    public bool IsStarving()
    {
        // TODO: hunger buffs
        return false;
    }

    public override void Add(Buff buff)
    {
        base.Add(buff);

        if (buff is Cripple)
        {
            GLog.W("You are crippled!");
        }
        else if (buff is Bleeding)
        {
            GLog.W("You are bleeding!");
        }

        // TODO: Burning, Paralysis, Poison, Ooze, Roots, Weakness, Blindness, Fury, Charm, Vertigo warnings and interrupts
        // TODO: BuffIndicator.RefreshHero()
    }

    public override void Remove(Buff buff)
    {
        base.Remove(buff);

        // TODO: light sprite state removal, BuffIndicator.RefreshHero()
    }

    public override int Stealth()
    {
        var stealth = base.Stealth();
        // TODO: ring of shadows
        return stealth;
    }

    public override void Die(object cause)
    {
        CurAction = null;

        // TODO: DewVial.AutoDrink(this)

        Actor.FixTime();
        base.Die(cause);

        // TODO: an Ankh in the belongings shows WndResurrect instead of ReallyDie
        ReallyDie(cause);
    }

    public static void ReallyDie(object cause)
    {
        var length = Level.Length;
        var map = Dungeon.Level.Map;
        var visited = Dungeon.Level.Visited;
        var discoverable = Level.Discoverable;

        for (var i = 0; i < length; i++)
        {
            var terr = map[i];

            if (discoverable[i])
            {
                visited[i] = true;
                if ((Terrain.Flags[terr] & Terrain.Secret) != 0)
                {
                    Level.Set(i, Terrain.Discover(terr));
                    GameScene.UpdateMap(i);
                }
            }
        }

        // TODO: Bones.Leave()

        Dungeon.Observe();

        Dungeon.Hero.Belongings.Identify();

        // TODO: scatter backpack over shuffled passable neighbours without heaps

        // TODO: GameScene.StartOver

        if (cause is IDoom doom)
        {
            doom.OnDeath();
        }

        // TODO: Dungeon.DeleteGame(HeroClass, True)
    }

    public override void Move(int step)
    {
        base.Move(step);

        if (!Flying)
        {
            if (Level.Water[Pos])
            {
                Sample.Play(Assets.SndWater, Random.Float(0.8f, 1.25f));
            }
            else
            {
                Sample.Play(Assets.SndStep);
            }

            Dungeon.Level.Press(Pos, this);
        }
    }

    public override void OnMotionComplete()
    {
        Dungeon.Observe();
        Search(false);

        base.OnMotionComplete();
    }

    public override void OnAttackComplete()
    {
        // TODO: AttackIndicator.Target(enemy)
        Attack(_enemy);

        CurAction = null;

        // TODO: Invisibility.Dispel()

        base.OnAttackComplete();
    }

    public override void OnOperateComplete()
    {
        // TODO: unlock consumes key and opens the door or exit

        CurAction = null;

        base.OnOperateComplete();
    }

    public bool Search(bool intentional)
    {
        var somethingFound = false;

        var positive = 0;
        var negative = 0;

        // TODO: ring of detection levels raise positive or lower negative

        var distance = 1 + positive + negative;

        var level = intentional
            ? 2 * Awareness - Awareness * Awareness
            : Awareness;
        if (distance <= 0)
        {
            level /= 2 - distance;
            distance = 1;
        }

        var cx = Pos % Level.Width;
        var cy = Pos / Level.Width;

        var ax = cx - distance;
        if (ax < 0)
        {
            ax = 0;
        }

        var bx = cx + distance;
        if (bx >= Level.Width)
        {
            bx = Level.Width - 1;
        }

        var ay = cy - distance;
        if (ay < 0)
        {
            ay = 0;
        }

        var by = cy + distance;
        if (by >= Level.Height)
        {
            by = Level.Height - 1;
        }

        for (var y = ay; y <= by; y++)
        {
            for (int x = ax, p = ax + y * Level.Width; x <= bx; x++, p++)
            {
                if (Dungeon.Visible[p])
                {
                    if (intentional)
                    {
                        GameScene.Effect(EffectKind.CheckedCell, p);
                    }

                    if (Level.Secret[p] && (intentional || Random.Float() < level))
                    {
                        var oldValue = Dungeon.Level.Map[p];

                        GameScene.DiscoverTile(p, oldValue);
                        Level.Set(p, Terrain.Discover(oldValue));
                        GameScene.UpdateMap(p);

                        // TODO: ScrollOfMagicMapping.Discover(p)

                        somethingFound = true;
                    }

                    if (intentional)
                    {
                        // TODO: a hidden heap at p is opened
                    }
                }
            }
        }

        if (intentional)
        {
            Sprite.ShowStatus(StatusColor.Default, TxtSearch);
            Sprite.Operate(Pos);
            if (somethingFound)
            {
                SpendAndNext(Random.Float() < level ? TimeToSearch : TimeToSearch * 2);
            }
            else
            {
                SpendAndNext(TimeToSearch);
            }
        }

        if (somethingFound)
        {
            GLog.W(TxtNoticedSomething);
            Sample.Play(Assets.SndSecret);
            Interrupt();
        }

        return somethingFound;
    }

    public void Resurrect(int resetLevel)
    {
        HP = HT;
        Dungeon.Gold = 0;
        Exp = 0;

        Belongings.Resurrect(resetLevel);

        Live();
    }

    public override HashSet<Type> Resistances()
    {
        // TODO: ring of elements
        return base.Resistances();
    }

    public override HashSet<Type> Immunities()
    {
        // TODO: GasesImmunity.Immunities

        return base.Immunities();
    }

    public interface IDoom
    {
        void OnDeath();
    }
}