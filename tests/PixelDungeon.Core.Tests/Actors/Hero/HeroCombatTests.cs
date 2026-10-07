using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Levels.Features;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Hero;

public class HeroCombatTests : DungeonFixture
{
    private Rat PlaceRat(int cell)
    {
        var rat = new Rat { Pos = cell, Sprite = new FakeCharView() };
        GameScene.Add(rat);
        return rat;
    }

    // TestLevel is sealed doesn't expose NMobs(), so the respawner test needs
    // its own minimal level. rows loaded the same way TestLevel.Load does
    // plus NMobs() the respawner branch can compare against
    // No hero is placed on this level, so Dungeon.Visible stays all-false and
    // RandomRespawnCell's rejection loop only has to avoid walls and occupied
    // cells
    private sealed class RespawnTestLevel : Level
    {
        public int MobCap;
        public override int NMobs() => MobCap;

        public void Load(params string[] rows)
        {
            Map = new int[Length];
            Array.Fill(Map, Terrain.Wall);
            Visited = new bool[Length];
            Mapped = new bool[Length];
            for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < rows[y].Length; x++)
                Map[x + y * Width] = TestLevel.CodeFor(rows[y][x]);

            BuildFlagMaps();
            CleanWalls();
        }

        protected override bool Build() => true;

        protected override void Decorate()
        {
        }

        protected override void CreateMobs()
        {
        }

        protected override void CreateItems()
        {
        }
    }

    [Fact]
    public void Respawner_WhenBelowNMobs_SpawnsAWanderingMobAndRegistersItWithTheScene()
    {
        var level = new RespawnTestLevel { MobCap = 1 };
        level.Load(
            "#####",
            "#...#",
            "#####"
        );
        Dungeon.Level = level;
        Dungeon.Depth = 1; // Bestiary.Mutable(1) always resolves to a ported mob (Rat/Albino)

        // invoke the respawner's protected Act() directly (reflection) instead of Actor.Process()
        // the spawned mob shares the respawner's spawn-time _time, Process()'s do-while loop would
        // immediately hand it its own turn too, and wandering AI can walk it straight to hunting
        // before this method gets to look at it
        var respawner = level.Respawner();
        var act = typeof(Actor).GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(act);
        act.Invoke(respawner, null);

        var spawned = Assert.Single(level.Mobs);
        Assert.Equal(spawned.WanderingState, spawned.State);
        Assert.Contains(spawned, View.AddedMobs);
    }

    [Fact]
    public void Handle_OnAnAdjacentRat_SwingsThroughTheView()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var rat = PlaceRat(TestLevel.At(2, 1));
        Dungeon.Observe();

        // the rat just became visible, so this first interrupts itself
        // the swing lands on the second call, once the rat is a known visible enemy
        hero.Handle(rat.Pos);
        hero.Handle(rat.Pos);

        Assert.Contains(rat.Pos, view.Attacks);
    }

    [Fact]
    public void ActAttack_OnAnAdjacentRat_HoldsTheSchedulerUntilOnAttackCompletes()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var rat = PlaceRat(TestLevel.At(2, 1));
        Dungeon.Observe();

        // one frame of the per-frame driver
        // settles the hero into "ready" and registers the rat as a known visible enemy
        Actor.Process();

        // GameScene's defaultCellListener.OnSelect: if (hero.handle(cell)) hero.next();
        var acted = hero.Handle(rat.Pos);
        if (acted)
        {
            hero.Next();
        }

        // true return here would free scheduler immediately
        // before the swing animation has even been shown
        Assert.False(acted);
        Assert.Equal(new[] { rat.Pos }, view.Attacks);

        // held, further frames must not let the hero swing again before
        // the view reports the animation finished
        Actor.Process();
        Assert.Equal(new[] { rat.Pos }, view.Attacks);

        hero.OnAttackComplete();
        Actor.Process(); // now free, must not throw or hang
    }

    [Fact]
    public void OnAttackComplete_AfterASwing_DamagesTheRatAndFreesTheScheduler()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var rat = PlaceRat(TestLevel.At(2, 1));
        Dungeon.Observe();

        // Handle only registers the rat as a known visible enemy
        hero.Handle(rat.Pos);
        hero.Handle(rat.Pos);

        // Pins that ActAttack actually swung (the stub's MakeReady()/return false
        // never touches the view, so this fails against the stub).
        Assert.Contains(rat.Pos, view.Attacks);
        var before = rat.HP;

        hero.OnAttackComplete();

        // Strictly less: a mutation that skips the swing (or the stub) leaves HP
        // unchanged, which would still satisfy "<=".
        Assert.True(rat.HP < before);
        Actor.Process(); // would spin forever if the gate were still held
    }

    [Fact]
    public void Handle_OnADistantVisibleRat_StepsCloserInsteadOfSwinging()
    {
        LoadLevel(
            "#######",
            "#<....#",
            "#######");
        var hero = PlaceHero(TestLevel.At(1, 1));
        var view = new FakeCharView();
        hero.Sprite = view;
        var rat = PlaceRat(TestLevel.At(5, 1));
        Dungeon.Observe();

        // See the comment in Handle_OnAnAdjacentRat_SwingsThroughTheView: the first
        // Handle only registers the rat as a known visible enemy.
        hero.Handle(rat.Pos);
        hero.Handle(rat.Pos);

        Assert.Empty(view.Attacks);
        Assert.Equal(TestLevel.At(2, 1), hero.Pos);
    }

    [Fact]
    public void Live_AttachesRegeneration()
    {
        LoadLevel(
            "###",
            "#<#",
            "###"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));

        hero.Live();

        Assert.NotNull(hero.GetBuff<Regeneration>());
    }

    [Fact]
    public void Add_Cripple_WarnsTheHero()
    {
        LoadLevel("###", "#<#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));

        Buff.Prolong<Cripple>(hero, Cripple.Duration);

        Assert.Contains(View.Logs, l => l is { Text: "You are crippled!", Kind: LogKind.Warning });
    }

    [Fact]
    public void Add_Bleeding_WarnsTheHero()
    {
        LoadLevel("###", "#<#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));

        Buff.Affect<Bleeding>(hero).Set(3);

        Assert.Contains(View.Logs, l => l is { Text: "You are bleeding!", Kind: LogKind.Warning });
    }

    [Fact]
    public void ChasmHeroLand_CripplesTheHero()
    {
        LoadLevel("###", "#<#", "###");
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.HP = hero.HT;

        Chasm.HeroLand();

        Assert.NotNull(hero.GetBuff<Cripple>());
    }
}