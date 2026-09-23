using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;
using Xunit.Abstractions;

namespace PixelDungeon.Core.Tests.Actors;

public class CharTests : DungeonFixture
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CharTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private sealed class Fighter : Char
    {
        public int AttackSkillValue;
        public int DefenseSkillValue;
        public int DamageValue = 1;
        public int DrValue;

        protected override bool Act() => false;

        public override int AttackSkill(Char target) => AttackSkillValue;
        public override int DefenseSkill(Char enemy) => DefenseSkillValue;
        public override int DamageRoll() => DamageValue;
        public override int Dr() => DrValue;
    }

    [Fact]
    public void Move_FlyingOntoADoor_OpensIt_AndLeavingAnOpenDoorClosesIt()
    {
        var level = LoadLevel(
            "#####",
            "#.+.#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var ch = new Fighter { Pos = TestLevel.At(1, 1), Flying = true };
        Actor.Add(ch);

        ch.Move(TestLevel.At(2, 1));
        Assert.Equal(Terrain.OpenDoor, level.Map[TestLevel.At(2, 1)]);
        Assert.Contains(TestLevel.At(2, 1), View.UpdatedCells);

        ch.Move(TestLevel.At(3, 1));
        Assert.Equal(Terrain.Door, level.Map[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void Move_WalkingOntoDoor_LeavesItClosed()
    {
        var level = LoadLevel(
            "#####",
            "#.+.#",
            "#####"
        );

        PlaceHero(TestLevel.At(1, 1));
        var ch = new Fighter { Pos = TestLevel.At(1, 1) };
        ch.Move(TestLevel.At(2, 1));

        Assert.Equal(Terrain.Door, level.Map[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void Move_LeavingAnOpenDoorWithAHeap_KeepsItOpen()
    {
        var level = LoadLevel(
            "#####",
            "#./.#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        level.Heaps[TestLevel.At(2, 1)] = new Heap { Pos = TestLevel.At(2, 1) };
        var ch = new Fighter { Pos = TestLevel.At(2, 1) };
        ch.Move(TestLevel.At(3, 1));

        Assert.Equal(Terrain.OpenDoor, level.Map[TestLevel.At(2, 1)]);
    }

    [Fact]
    public void Move_HidesNonHeroSpritesOutsideTheHeroView()
    {
        LoadLevel(
            "#######",
            "#..#..#",
            "#######"
        );
        PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        var ch = new Fighter { Pos = TestLevel.At(2, 1), Sprite = view };

        ch.Move(TestLevel.At(4, 1));
        Assert.False(view.Visible);

        ch.Move(TestLevel.At(2, 1));
        Assert.True(view.Visible);
    }

    [Fact]
    public void Distance_IsChebyshev()
    {
        var a = new Fighter { Pos = TestLevel.At(1, 1) };
        var b = new Fighter { Pos = TestLevel.At(4, 3) };

        Assert.Equal(3, a.Distance(b));
    }

    [Fact]
    public void Damage_ReducesHp_ShowsStatus_AndKillsAtZero()
    {
        var view = new FakeCharView();
        var ch = new Fighter { Pos = TestLevel.At(2, 2), HT = 10, HP = 10, Sprite = view };
        Actor.Add(ch);

        ch.Damage(3, "test");
        Assert.Equal(7, ch.HP);
        Assert.Equal((StatusColor.Warning, "3"), view.Statuses[0]);

        ch.Damage(4, "test");
        Assert.Equal((StatusColor.Negative, "4"), view.Statuses[1]);

        ch.Damage(9, "test");
        Assert.False(ch.IsAlive());
        Assert.True(view.Died);
        Assert.DoesNotContain(ch, Actor.All());
        Assert.Null(Actor.FindChar(TestLevel.At(2, 2)));

        ch.Damage(1, "test"); // already dead, ignored
        Assert.Equal(3, view.Statuses.Count);
    }

    [Fact]
    public void Damage_ZeroFromNonChar_ShowsNoStatus()
    {
        var view = new FakeCharView();
        var ch = new Fighter { HT = 10, HP = 10, Sprite = view };
        ch.Damage(0, "test");

        Assert.Empty(view.Statuses);
        Assert.Equal(10, ch.HP);
    }

    [Fact]
    public void Hit_ComparesSkillRolls()
    {
        var sure = new Fighter { AttackSkillValue = 10 };
        var helpless = new Fighter { DefenseSkillValue = 0 };
        var hopeless = new Fighter { AttackSkillValue = 0 };
        var nimble = new Fighter { DefenseSkillValue = 10 };
        for (var i = 0; i < 100; i++)
        {
            Assert.True(Char.Hit(sure, helpless, false));
            Assert.False(Char.Hit(hopeless, nimble, false));
        }
    }

    [Fact]
    public void Attack_HitDealsDamageAndLogs_MissSHowsDodge()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        Dungeon.Visible[TestLevel.At(2, 1)] = true;
        var enemyView = new FakeCharView();
        var attacker = new Fighter
        {
            Name = "rat",
            Pos = TestLevel.At(2, 1),
            AttackSkillValue = 100,
            DamageValue = 3
        };
        var enemy = new Fighter
        {
            Name = "crab",
            Pos = TestLevel.At(3, 1),
            HT = 10,
            HP = 10,
            Sprite = enemyView
        };

        Assert.True(attacker.Attack(enemy));
        Assert.Equal(7, enemy.HP);
        Assert.True(View.Logged("rat hit crab"));
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndHit);

        attacker.AttackSkillValue = 0;
        enemy.DefenseSkillValue = 100;
        Assert.False(attacker.Attack(enemy));
        Assert.Equal((StatusColor.Neutral, "dodged"), enemyView.Statuses[^1]);
        _testOutputHelper.WriteLine(enemyView.Statuses[0].Text);
        Assert.True(View.Logged("crab dodged rat's attack"));
    }

    [Fact]
    public void Attack_KillingTheHero_RecordsTheResult()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = 1;
        Dungeon.Depth = 2;
        var attacker = new Fighter
        {
            Name = "rat",
            Pos = TestLevel.At(2, 1),
            AttackSkillValue = 100,
            DamageValue = 5
        };
        attacker.Attack(hero);
        Assert.False(hero.IsAlive());
        Assert.Equal("Killed by a rat on level 2", Dungeon.ResultDescription);
        Assert.True(View.Logged("rat killed you..."));
    }

    [Fact]
    public void Bundle_RoundTripsPosHpHt()
    {
        var ch = new Fighter { Pos = 40, HP = 3, HT = 9 };
        var bundle = new Bundle();
        ch.StoreInBundle(bundle);

        var restored = new Fighter();
        restored.RestoreFromBundle(bundle);

        Assert.Equal(40, restored.Pos);
        Assert.Equal(3, restored.HP);
        Assert.Equal(9, restored.HT);
    }

    [Fact]
    public void Observe_MarksVisibleCellsVisited()
    {
        var level = LoadLevel(
            "#####",
            "#...#",
            "#####"
        );

        PlaceHero(TestLevel.At(1, 1));

        Assert.True(Dungeon.Visible[TestLevel.At(3, 1)]);
        Assert.True(level.Visited[TestLevel.At(3, 1)]);
        Assert.False(Dungeon.Visible[TestLevel.At(10, 10)]);
        Assert.Equal(1, View.ObserveCount);
    }
}