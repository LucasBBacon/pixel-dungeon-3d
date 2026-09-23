using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using Xunit.Abstractions;

namespace PixelDungeon.Core.Tests.Actors;

public class ActorTests : DungeonFixture
{
    private sealed class RecordingActor : Actor
    {
        private readonly string _name;
        private readonly List<string> _log;
        private readonly float _spend;
        private int _acts;
        public Action OnAct;

        public RecordingActor(string name,
            List<string> log,
            float spend,
            int acts)
        {
            _name = name;
            _log = log;
            _spend = spend;
            _acts = acts;
        }

        protected override bool Act()
        {
            _log.Add(_name);
            OnAct?.Invoke();
            Spend(_spend);
            if (--_acts <= 0)
            {
                Diactivate();
            }

            return true;
        }

        public float TimeLeft => Cooldown();
    }

    private sealed class MovingChar : Char
    {
        public int Acts;

        protected override bool Act()
        {
            Acts++;
            Spend(1);
            return false;
        }
    }

    [Fact]
    public void Process_LowestTimeActsFirst()
    {
        var log = new List<string>();
        Actor.AddDelayed(new RecordingActor("a", log, 10, 1), 2);
        Actor.AddDelayed(new RecordingActor("b", log, 10, 1), 1);
        Actor.Add(new RecordingActor("c", log, 10, 1));

        Actor.Process();

        Assert.Equal(new[] { "c", "b", "a" }, log);
    }

    [Fact]
    public void Spend_ReordersActors()
    {
        var log = new List<string>();
        Actor.Add(new RecordingActor("a", log, 10, 3));
        Actor.AddDelayed(new RecordingActor("b", log, 3, 3), 1);

        Actor.Process();

        Assert.Equal(new[] { "a", "b", "b", "b", "a", "a" }, log);
    }

    [Fact]
    public void Process_haltsWhileTheCurrentCharIsMoving_AndResumesAfterMotionComplete()
    {
        var view = new FakeCharView { IsMovingFlag = true };
        var ch = new MovingChar { Pos = TestLevel.At(1, 1), Sprite = view };
        Actor.Add(ch);

        Actor.Process();
        Assert.Equal(0, ch.Acts);

        view.IsMovingFlag = false;
        Actor.Process();
        Assert.Equal(1, ch.Acts);

        Actor.Process(); // char is still current until Next() called
        Assert.Equal(1, ch.Acts);

        ch.OnMotionComplete();
        Actor.Process();
        Assert.Equal(2, ch.Acts);
    }

    [Fact]
    public void Process_StopsWhenTheHeroDies()
    {
        var log = new List<string>();
        var killer = new RecordingActor("a", log, 1, 1) { OnAct = () => Dungeon.Hero.HP = 0 };
        Actor.Add(killer);
        Actor.AddDelayed(new RecordingActor("b", log, 1, 1), 1);

        Actor.Process();
        Assert.Equal(new[] { "a" }, log);
    }

    [Fact]
    public void FixTime_ShiftsTheMinimumToZero_AndCountsDurationWhenTheHeroIsScheduled()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Spend(10);
        var log = new List<string>();
        var a = new RecordingActor("a", log, 1, 1);
        Actor.AddDelayed(a, 4);

        Actor.Process(); // "a" acts at 4, then the hero acts at 10 and stays current
        Actor.FixTime();

        Assert.Equal(10f, Statistics.Duration);
        Assert.Equal(new[] { "a" }, log);
    }

    [Fact]
    public void FixTime_WithoutTheHero_LeavesDurationAlone()
    {
        var log = new List<string>();
        var a = new RecordingActor("a", log, 1, 1);
        var b = new RecordingActor("b", log, 1, 1);
        Actor.AddDelayed(a, 5);
        Actor.AddDelayed(b, 8);

        Actor.FixTime();

        Assert.Equal(0f, Statistics.Duration);
        Assert.Equal(0f, a.TimeLeft);
        Assert.Equal(3f, b.TimeLeft);
    }

    [Fact]
    public void Id_AssignsIncreasingIds_AndFindByIdResolvesThem()
    {
        var log = new List<string>();
        var a = new RecordingActor("a", log, 1, 1);
        var b = new RecordingActor("b", log, 1, 1);
        Actor.Add(a);
        Actor.Add(b);

        Assert.Equal(1, a.Id());
        Assert.Equal(2, b.Id());
        Assert.Equal(1, a.Id());

        Actor.Remove(a);
        Actor.Add(a);
        Assert.Same(a, Actor.FindById(1));
    }

    [Fact]
    public void FindChar_ReturnsTheOccupant()
    {
        var ch = new MovingChar { Pos = TestLevel.At(3, 3) };

        Actor.OccupyCell(ch);
        Assert.Same(ch, Actor.FindChar(TestLevel.At(3, 3)));

        Actor.FreeCell(TestLevel.At(3, 3));
        Assert.Null(Actor.FindChar(TestLevel.At(3, 3)));
    }

    [Fact]
    public void Bundle_RoundTripsTimeAndId()
    {
        var log = new List<string>();
        var a = new RecordingActor("a", log, 1, 1);
        Actor.Add(a);
        a.Spend(3);
        a.Id();
        var bundle = new Bundle();
        a.StoreInBundle(bundle);
        Assert.Equal(3f, bundle.GetFloat("time"));
        Assert.Equal(1, bundle.GetInt("id"));

        var restored = new RecordingActor("r", log, 1, 1);
        restored.RestoreFromBundle(bundle);
        Assert.Equal(3f, restored.TimeLeft);
        Assert.Equal(1, restored.Id());
    }
}