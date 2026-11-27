using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Painters;
using PixelDungeon.Core.Levels.Traps;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Levels.Traps;

public class TrapTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#.....#",
        "#.....#",
        "#######"
    ];

    private static int C(int x, int y = 2) => TestLevel.At(x, y);

    private static int Gas<T>(int cell) where T : Blob =>
        Dungeon.Level.Blobs.TryGetValue(typeof(T), out var blob) ? blob.Cur[cell] : 0;

    private static int BleedingLevel(Bleeding b) =>
        (int)typeof(Bleeding)
            .GetField("Level", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(b);

    private static void Tick(Actor actor) =>
        typeof(Actor)
            .GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(actor, null);

    private sealed class Dummy : Mob
    {
        public Dummy(int hp)
        {
            Name = "dummy";
            HP = HT = hp;
        }
    }

    private Dummy Place(int cell, int hp = 10)
    {
        var d = new Dummy(hp) { Pos = cell, Sprite = new FakeCharView() };
        Actor.AddDelayed(d, 100f);
        Actor.OccupyCell(d);
        Dungeon.Level.Mobs.Add(d);
        return d;
    }

    [Theory]
    [InlineData(1, 320)]
    [InlineData(3, 360)]
    public void ToxicTrap_SeedsGasByDepth(int depth, int amount)
    {
        LoadLevel(Room);
        Dungeon.Depth = depth;
        ToxicTrap.Trigger(C(3), null);
        Assert.Equal(amount, Gas<ToxicGas>(C(3)));
    }

    [Fact]
    public void ToxicTrap_Twice_Accumulates()
    {
        LoadLevel(Room);
        Dungeon.Depth = 3;
        ToxicTrap.Trigger(C(3), null);
        ToxicTrap.Trigger(C(3), null);
        Assert.Equal(720, Gas<ToxicGas>(C(3)));
    }

    [Fact]
    public void ParalyticTrap_Depth3_Seeds95()
    {
        LoadLevel(Room);
        Dungeon.Depth = 3;
        ParalyticTrap.Trigger(C(3), null);
        Assert.Equal(95, Gas<ParalyticGas>(C(3)));
    }

    [Fact]
    public void FireTrap_SeedsFireTwoAndBurstsFlames()
    {
        LoadLevel(Room);
        FireTrap.Trigger(C(3), null);
        Assert.Equal(2, Gas<Fire>(C(3)));
        Assert.Contains((C(3), 0xFFEE7722u, 5), View.Splashes);
    }

    [Theory]
    [InlineData(1, 4f)]
    [InlineData(3, 5f)]
    [InlineData(4, 6f)]
    public void PoisonTrap_PoisonsByDepth(int depth, float left)
    {
        LoadLevel(Room);
        Dungeon.Depth = depth;
        var d = Place(C(3));
        PoisonTrap.Trigger(C(3), d);
        Assert.Equal(left, d.GetBuff<Poison>().Left);
        Assert.Contains((C(3), 0xFF8844FFu, 3), View.Splashes);
    }

    [Fact]
    public void PoisonTrap_ReplacesALongerPoison()
    {
        LoadLevel(Room);
        Dungeon.Depth = 1;
        var d = Place(C(3));
        Buff.Affect<Poison>(d).Set(50f);
        PoisonTrap.Trigger(C(3), d);
        Assert.Equal(4f, d.GetBuff<Poison>().Left);
    }

    [Fact]
    public void PoisonTrap_NoCharacter_OnlySplashes()
    {
        LoadLevel(Room);
        PoisonTrap.Trigger(C(3), null);
        Assert.Single(View.Splashes);
    }

    [Fact]
    public void AlarmTrap_BeckonsAllButTheTrigger_LogsOnlyInView_AlwaysSounds()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var trigger = Place(C(3));
        var other = Place(C(5));
        other.State = other.SleepingState;
        trigger.State = trigger.SleepingState;

        Array.Fill(Dungeon.Visible, false);
        AlarmTrap.Trigger(C(3), trigger);

        Assert.Same(other.WanderingState, other.State);
        Assert.Same(trigger.SleepingState, trigger.State);
        Assert.False(View.Logged("piercing sound"));
        Assert.Empty(View.Splashes);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndAlert);

        View.Sounds.Clear();
        Dungeon.Visible[C(3)] = true;
        AlarmTrap.Trigger(C(3), trigger);
        Assert.True(View.Logged("The trap emits a piercing sound that echoes throughout the dungeon!"));
        Assert.Contains((C(3), 0xFFFFFFFFu, 3), View.Splashes);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndAlert);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(2)]
    [InlineData(1)]
    public void LightningTrap_DamageIsAThirdToTwoThirdsOfHp_AtLeastOne(int hp)
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var d = Place(C(3), hp);
        Random.Seed(7);
        var expected = Math.Max(1, Random.Int(hp / 3, 2 * hp / 3));
        Random.Seed(7);
        LightningTrap.Trigger(C(3), d);
        Assert.Equal(hp - expected, d.HP);
    }

    [Fact]
    public void LightningTrap_Hero_ShakesAndDrawsTwoArcs()
    {
        LoadLevel(Room);
        var hero = PlaceHero(C(3));
        hero.HP = hero.HT = 100;
        LightningTrap.Trigger(C(3), hero);
        Assert.Contains((2f, 0.3f), View.Shakes);
        Assert.Equal(2, View.Lightnings.Count);
        Assert.Equal(new[] { C(3) - Level.Width, C(3) + Level.Width }, View.Lightnings[0].Cells);
        Assert.Equal(new[] { C(3) - 1, C(3) + 1 }, View.Lightnings[1].Cells);
        Assert.Equal(2, View.Lightnings[0].Length);
        Assert.Equal(2, View.Sounds.Count(s => s.Id == Assets.SndLightning));
    }

    [Fact]
    public void LightningTrap_KillsTheHero_RecordsTheTrapCause()
    {
        LoadLevel(Room);
        Dungeon.Depth = 2;
        var hero = PlaceHero(C(3));
        hero.HP = 1; // Random.Int(0, 0) = 0, so Max(1, 0) = 1 kills
        LightningTrap.Trigger(C(3), hero);
        Assert.False(hero.IsAlive());
        Assert.Equal("Killed by discharge of lightning trap on level 2", Dungeon.ResultDescription);
        Assert.True(View.Logged("You were killed by a discharge of a lightning trap..."));
    }

    [Fact]
    public void LightningTrap_SparkRoll_ComesAfterTheDamageRoll()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var d = Place(C(3), 30);
        Random.Seed(11);
        Random.Int(10, 20);
        var sparks = Random.IntRange(3, 4);
        Random.Seed(11);
        LightningTrap.Trigger(C(3), d);
        Assert.Contains((C(3), 0xFFFFFFFFu, sparks), View.Splashes);
    }

    [Fact]
    public void LightningTrap_NoCharacter_OnlySparks()
    {
        LoadLevel(Room);
        LightningTrap.Trigger(C(3), null);
        Assert.Empty(View.Lightnings);
        var splash = Assert.Single(View.Splashes);
        Assert.Equal((C(3), 0xFFFFFFFFu), (splash.Cell, splash.Color));
        Assert.InRange(splash.N, 3, 4);
    }

    [Fact]
    public void GrippingTrap_RatAtDepth2_Bleeds5AndCrippled10()
    {
        LoadLevel(Room);
        Dungeon.Depth = 2;
        var rat = new Rat { Pos = C(3), Sprite = new FakeCharView() };
        Actor.AddDelayed(rat, 100f);
        Assert.Equal(0, rat.Dr() / 2);
        GrippingTrap.Trigger(C(3), rat);
        Assert.Equal(5, BleedingLevel(rat.GetBuff<Bleeding>()));
        Assert.Equal(Cripple.Duration, CooldownOf(rat.GetBuff<Cripple>()));
        Assert.Contains((EffectKind.Wound, C(3)), View.Effects);
    }

    [Fact]
    public void GrippingTrap_NoCharacter_OnlyAWound()
    {
        LoadLevel(Room);
        GrippingTrap.Trigger(C(3), null);
        Assert.Equal(new[] { (EffectKind.Wound, C(3)) }, View.Effects.ToArray());
    }

    [Fact]
    public void SummoningTrap_BossLevel_DoesNothing()
    {
        LoadLevel(Room);
        Dungeon.Depth = 5;
        Assert.True(Dungeon.BossLevel());
        SummoningTrap.Trigger(C(3), null);
        Assert.Empty(View.AddedMobs);
        Assert.Empty(Dungeon.Level.Mobs);
    }

    [Fact]
    public void SummoningTrap_SpawnsOneToThreeWanderingMobsWithDelayTwo()
    {
        var counts = new HashSet<int>();
        for (var seed = 1; seed <= 40; seed++)
        {
            Dungeon.Reset();
            Random.Seed(seed);
            View.AddedMobs.Clear();
            LoadLevel(Room);
            Dungeon.Depth = 2;
            Dungeon.Hero = new Hero();
            PlaceHero(C(1, 1));
            SummoningTrap.Trigger(C(3), Dungeon.Hero);
            counts.Add(Dungeon.Level.Mobs.Count);
            foreach (var mob in Dungeon.Level.Mobs)
            {
                Assert.Same(mob.WanderingState, mob.State);
                Assert.Equal(2f, CooldownOf(mob));
            }

            Assert.Equal(Dungeon.Level.Mobs.Count, View.AddedMobs.Count);
        }

        Assert.Equal(new[] { 1, 2, 3 }, counts.OrderBy(c => c).ToArray());
    }

    [Fact]
    public void SummoningTrap_NoFreeNeighbour_SpawnsNothing()
    {
        LoadLevel(
            "#####",
            "#####",
            "##.##",
            "#####",
            "#####");
        Dungeon.Depth = 2;
        PlaceHero(TestLevel.At(2, 2));
        SummoningTrap.Trigger(TestLevel.At(2, 2), Dungeon.Hero);
        Assert.Empty(Dungeon.Level.Mobs);
        Assert.Empty(View.AddedMobs);
        Actor.Process();
        for (var i = 0; i < Level.Length; i++)
        {
            Assert.True(Actor.FindChar(i) == null || Actor.FindChar(i) == Dungeon.Hero);
        }
    }

    [Fact]
    public void SummoningTrap_OneFreeNeighbour_SpawnsAtMostOne()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            Dungeon.Reset();
            Random.Seed(seed);
            LoadLevel(
                "#####",
                "#####",
                "##..#",
                "#####",
                "#####");
            Dungeon.Depth = 2;
            Dungeon.Hero = new Hero();
            PlaceHero(TestLevel.At(2, 2));
            var free = TestLevel.At(3, 2);
            SummoningTrap.Trigger(TestLevel.At(2, 2), Dungeon.Hero);
            Assert.InRange(Dungeon.Level.Mobs.Count, 0, 1);
            Actor.Process();
            var there = Actor.FindChar(free);
            Assert.True(there == null || Dungeon.Level.Mobs.Contains((Mob)there),
                $"seed {seed}: the dummy still holds the cell");
        }
    }

    [Fact]
    public void SummoningTrap_UnportedDepth_SkipsThePoint()
    {
        LoadLevel(Room);
        Dungeon.Depth = 12; // not a boss level, and the Bestiary has no mobs for it yet
        Assert.False(Dungeon.BossLevel());
        Assert.Null(Bestiary.Mob(12));
        PlaceHero(C(1, 1));
        SummoningTrap.Trigger(C(3), Dungeon.Hero);
        Assert.Empty(Dungeon.Level.Mobs);
        Assert.Empty(View.AddedMobs);
    }

    [Fact]
    public void Press_SecretTrap_LogsTheClickBeforeTheTrigger()
    {
        var level = LoadLevel(
            "#####",
            "#.a.#",
            "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        Dungeon.Visible[TestLevel.At(2, 1)] = true;
        level.Press(TestLevel.At(2, 1), hero);
        var click = View.Logs.FindIndex(l => l.Text.Contains("hidden pressure plate"));
        var alarm = View.Logs.FindIndex(l => l.Text.Contains("piercing sound"));
        Assert.True(click >= 0 && alarm > click);
    }

    [Fact]
    public void BurnedRoomVariant_FireTrapsOnEmbers_TriggerWithoutSpreading()
    {
        Dungeon.Depth = 2;
        TestLevel level = null;
        for (var seed = 1; seed < 500 && level == null; seed++)
        {
            Random.Seed(seed);
            var candidate = TestLevel.FromRows("#");
            Dungeon.Level = candidate;
            var room = (Room)new Room().Set(2, 2, 12, 10);
            StandardPainter.Paint(candidate, room);
            if (candidate.Map.Contains(Terrain.Embers) && candidate.Map.Contains(Terrain.FireTrap))
            {
                level = candidate;
            }
        }

        Assert.NotNull(level);
        level.Reflag();
        PlaceHero(C(1, 1));

        var trap = Array.IndexOf(level.Map, Terrain.FireTrap);
        var embers = Enumerable
            .Range(0, Level.Length)
            .Where(i => level.Map[i] == Terrain.Embers)
            .ToList();

        level.Press(trap, null);
        var fire = (Fire)Dungeon.Level.Blobs[typeof(Fire)];
        Assert.Equal(Terrain.InactiveTrap, level.Map[trap]);
        Assert.True(fire.Cur[trap] > 0);
        for (var i = 0; i < 6; i++)
        {
            Tick(fire);
        }

        Assert.All(embers, cell => Assert.Equal(Terrain.Embers, level.Map[cell]));
    }

    [Fact]
    public void Press_TrapTriggerSeesTheTrapTerrain()
    {
        var level = LoadLevel(
            "#####",
            "#.Z.#",
            "#####");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT = 100;
        var cell = TestLevel.At(2, 1);
        level.Press(cell, hero);
        Assert.Equal(Terrain.InactiveTrap, level.Map[cell]);
        Assert.Equal(2, View.Lightnings.Count);
        Assert.Equal(cell - Level.Width, View.Lightnings[0].Cells[0]);
    }

    [Fact]
    public void MobPress_HiddenTrap_DoesNothing()
    {
        var level = LoadLevel(
            "#####",
            "#.^.#",
            "#####");
        PlaceHero(TestLevel.At(1, 1));
        var rat = new Rat { Pos = TestLevel.At(2, 1), Sprite = new FakeCharView() };
        Actor.Add(rat);
        level.MobPress(rat);
        Assert.Equal(Terrain.SecretToxicTrap, level.Map[TestLevel.At(2, 1)]);
        Assert.Empty(level.Blobs);
    }
}