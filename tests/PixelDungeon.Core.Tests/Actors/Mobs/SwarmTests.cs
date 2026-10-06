using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Actors.Mobs;

public class SwarmTests : DungeonFixture
{
    private Swarm PlaceSwarm(int cell)
    {
        var swarm = new Swarm { Pos = cell, Sprite = new FakeCharView() };
        GameScene.Add(swarm);
        return swarm;
    }

    // HP = 81, damage = 10, 3 plausible formulas disagree:
    // (HP - damage) / 2 = 35, HP / 2 = 40, (HP - damage + 1) / 2 = 36
    // 35 is correct, so mutat using either alternative fails assertion
    [Fact]
    public void DefenseProc_WithRoomAndHealth_SplitsAndHalvesTheRemainder()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var swarm = PlaceSwarm(TestLevel.At(2, 2));
        swarm.HP = 81;

        swarm.DefenseProc(Dungeon.Hero, 10);

        Assert.Equal(2, Dungeon.Level.Mobs.Count(m => m is Swarm));
        var clone = Dungeon.Level.Mobs.OfType<Swarm>().Single(m => m != swarm);

        Assert.Equal(35, clone.HP); // (81 - 10) / 2
        Assert.Equal(46, swarm.HP); // 81 - 35
    }

    // HP = 11, damage = 10, exactly 1 below HP >= damage + 2 gate
    // (11 >= 12 is false), so mutant that relaxes gate to > or to
    // damage + 1 would split were when it should not
    [Fact]
    public void DefenseProc_WhenTooHurt_DoesNotSplit()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var swarm = PlaceSwarm(TestLevel.At(2, 2));
        swarm.HP = 11;

        swarm.DefenseProc(Dungeon.Hero, 10);

        Assert.Equal(1, Dungeon.Level.Mobs.Count(m => m is Swarm));
    }

    // 1x1 room has no floor cell outside the swarm's own
    // none of its 4 orthogonal neighbors are passable
    // genuine no free cell case
    [Fact]
    public void DefenseProc_WhenBoxedIn_DoesNotSplit()
    {
        LoadLevel(
            "###",
            "#<#",
            "###"
        );
        var swarm = PlaceSwarm(TestLevel.At(1, 1));
        swarm.HP = 80;

        swarm.DefenseProc(null, 10);

        Assert.Equal(1, Dungeon.Level.Mobs.Count(m => m is Swarm));
    }
    
    // two-cell vertical dead end
    // swarm's only non-wall orthogonal neighbor cell south, hero stands there
    // pins the "&& Actor.FindChar(n) == null" exclusion deterministically
    // no RNG involved, unlike the door test above (which has a genuinely
    // free cell to land on and so cannot, by itself, prove the occupied
    // neighbor was excluded rather than just not drawn).
    [Fact]
    public void DefenseProc_WhenOnlyFreeNeighbourIsOccupiedByHero_DoesNotSplit()
    {
        LoadLevel(
            "###",
            "#.#",
            "#.#",
            "###");
        PlaceHero(TestLevel.At(1, 2));
        var swarm = PlaceSwarm(TestLevel.At(1, 1));
        swarm.HP = 80;

        swarm.DefenseProc(Dungeon.Hero, 10);

        Assert.Equal(1, Dungeon.Level.Mobs.Count(m => m is Swarm));
        Assert.Equal(80, swarm.HP);
    }

    [Fact]
    public void Split_IncrementsTheGeneration()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var swarm = PlaceSwarm(TestLevel.At(2, 2));
        swarm.HP = 80;

        swarm.DefenseProc(Dungeon.Hero, 10);
        var clone = Dungeon.Level.Mobs.OfType<Swarm>().Single(m => m != swarm);

        var bundle = new Bundle();
        clone.StoreInBundle(bundle);
        Assert.Equal(1, bundle.GetInt("generation"));
    }

    // hero occupies (1, 1), rows 0  and 2 solid wall
    // (3, 1) door is only passable unoccupied neighbor of swarm
    // clone has nowhere else to land
    [Fact]
    public void DefenseProc_WhenACloneLandsOnADoor_OpensIt()
    {
        LoadLevel(
            "#####",
            "#<.+#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var swarm = PlaceSwarm(TestLevel.At(2, 1));
        swarm.HP = 80;

        // only free orthogonal neighbor is door at 3, 1
        swarm.DefenseProc(Dungeon.Hero, 10);

        Assert.Equal(Terrain.OpenDoor, Dungeon.Level.Map[TestLevel.At(3, 1)]);
    }

    [Fact]
    public void Swarm_IsFlyingAndEvades()
    {
        var swarm = new Swarm();
        Assert.True(swarm.Flying);
        Assert.Equal("evaded", swarm.DefenseVerb());
        Assert.Equal(80, swarm.HT);
        Assert.Equal(12, swarm.AttackSkill(null));
    }

    [Fact]
    public void Swarm_RoundTripsItsGeneration()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####");
        PlaceHero(TestLevel.At(1, 1));
        var swarm = PlaceSwarm(TestLevel.At(2, 2));
        swarm.HP = 81;

        swarm.DefenseProc(Dungeon.Hero, 10);
        var clone = Dungeon.Level.Mobs.OfType<Swarm>().Single(m => m != swarm);

        var bundle = new Bundle();
        clone.StoreInBundle(bundle);
        var restored = new Swarm();
        restored.RestoreFromBundle(bundle);

        var check = new Bundle();
        restored.StoreInBundle(check);
        Assert.Equal(1, check.GetInt("generation"));
    }
}