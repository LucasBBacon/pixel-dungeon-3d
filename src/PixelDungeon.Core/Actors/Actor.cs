using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Actors;

public abstract class Actor : IBundlable
{
    public const float Tick = 1f;

    private float _time;

    private int _id = 0;

    protected abstract bool Act();

    public virtual void Spend(float time) => _time += time;

    public void Postpone(float time)
    {
        if (_time < _now + time)
        {
            _time = _now + time;
        }
    }

    protected float Cooldown() => _time - _now;

    protected void Diactivate() => _time = float.MaxValue;

    protected virtual void OnAdd()
    {
    }

    protected virtual void OnRemove()
    {
    }

    private const string TagTime = "time";
    private const string TagId = "id";

    public virtual void RestoreFromBundle(Bundle bundle)
    {
        _time = bundle.GetFloat(TagTime);
        _id = bundle.GetInt(TagId);
    }

    public virtual void StoreInBundle(Bundle bundle)
    {
        bundle.Put(TagTime, _time);
        bundle.Put(TagId, _id);
    }

    public int Id()
    {
        if (_id > 0) return _id;

        var max = _all
            .Select(a => a._id)
            .Prepend(0)
            .Max();

        return _id = max + 1;
    }

    private static readonly HashSet<Actor> _all = [];
    private static Actor _current;

    private static readonly Dictionary<int, Actor> _ids = new();

    private static float _now = 0;

    private static readonly Char[] Chars = new Char[Level.Length];

    public static void Clear()
    {
        _now = 0;

        Array.Fill(Chars, null);
        _all.Clear();
        _ids.Clear();
        _current = null;
    }

    public static void FixTime()
    {
        if (Dungeon.Hero != null && _all.Contains(Dungeon.Hero))
        {
            Statistics.Duration += _now;
        }

        var min = _all
            .Select(actor => actor._time)
            .Prepend(float.MaxValue)
            .Min();

        foreach (var actor in _all)
        {
            actor._time -= min;
        }

        _now = 0;
    }

    public static void Init()
    {
        AddDelayed(Dungeon.Hero, -float.Epsilon);

        foreach (var mob in Dungeon.Level.Mobs)
        {
            Add(mob);
        }

        foreach (var blob in Dungeon.Level.Blobs.Values)
        {
            Add(blob);
        }

        _current = null;
    }

    public static void OccupyCell(Char ch) => Chars[ch.Pos] = ch;

    public static void FreeCell(int pos) => Chars[pos] = null;

    public void Next()
    {
        if (_current == this)
        {
            _current = null;
        }
    }

    public static void Process()
    {
        if (_current != null)
        {
            return;
        }

        bool doNext;

        do
        {
            _now = float.MaxValue;
            _current = null;

            Array.Fill(Chars, null);

            foreach (var actor in _all)
            {
                if (actor._time < _now)
                {
                    _now = actor._time;
                    _current = actor;
                }

                if (actor is Char ch)
                {
                    Chars[ch.Pos] = ch;
                }
            }

            if (_current != null)
            {
                if (_current is Char c && c.Sprite.IsMoving)
                {
                    // If it's character's turn to act, but its sprite
                    // is moving, wait till the movement is over
                    _current = null;
                    break;
                }

                doNext = _current.Act();

                if (!doNext || Dungeon.Hero.IsAlive()) continue;

                doNext = false;
                _current = null;
            }
            else
            {
                doNext = false;
            }
        } while (doNext);
    }

    public static void Add(Actor actor) => Add(actor, _now);

    public static void AddDelayed(Actor actor, float delay) => Add(actor, _now + delay);

    private static void Add(Actor actor, float time)
    {
        if (_all.Contains(actor))
        {
            return;
        }

        if (actor._id > 0)
        {
            _ids[actor._id] = actor;
        }

        _all.Add(actor);
        actor._time += time;
        actor.OnAdd();

        if (actor is Char ch)
        {
            Chars[ch.Pos] = ch;
            foreach (var buff in ch.Buffs())
            {
                _all.Add(buff);
                buff.OnAdd();
            }
        }
    }

    public static void Remove(Actor actor)
    {
        if (actor != null)
        {
            _all.Remove(actor);
            actor.OnRemove();

            if (actor._id > 0)
            {
                _ids.Remove(actor._id);
            }
        }
    }

    public static Char FindChar(int pos) => Chars[pos];

    public static Actor FindById(int id) => _ids.GetValueOrDefault(id);

    public static HashSet<Actor> All() => _all;
}