using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Bags;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Levels;

namespace PixelDungeon.Core.Tests;

public class DungeonTests : DungeonFixture
{
    [Fact]
    public void Init_CreatesAHero_AndResetsRunState()
    {
        Dungeon.Depth = 7;
        Dungeon.Gold = 9;
        Dungeon.Init();
        Assert.NotNull(Dungeon.Hero);
        Assert.Equal(20, Dungeon.Hero.HP);
        Assert.Equal(0, Dungeon.Depth);
        Assert.Equal(0, Dungeon.Gold);
        Assert.True(Dungeon.DewVial);
        // Init() calls hero.Live(), which now attaches Regeneration
        // that buff schedules itself as an actor even though the hero isn't placed
        // on a level yet
        Assert.Equal(2, Actor.All().Count);
        Assert.NotNull(Dungeon.Hero.GetBuff<Hunger>());
    }

    [Fact]
    public void NewLevel_IncrementsDepth_TracksDeepestFloor_AndBuildsASewer()
    {
        Dungeon.Init();
        var first = Dungeon.NewLevel();
        Assert.IsType<SewerLevel>(first);
        Assert.Equal(1, Dungeon.Depth);
        Assert.Equal(1, Statistics.DeepestFloor);
        Assert.True(Statistics.QualifiedForNoKilling);
        Assert.Equal(Terrain.Entrance, first.Map[first.Entrance]);

        Dungeon.NewLevel();
        Assert.Equal(2, Dungeon.Depth);
        Assert.Equal(2, Statistics.DeepestFloor);
    }

    [Fact]
    public void SwitchLevel_PlacesTheHero_SchedulesActors_AndObserves()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Entrance);

        Assert.Same(level, Dungeon.Level);
        Assert.Equal(level.Entrance, Dungeon.Hero.Pos);
        Assert.True(Dungeon.Visible[level.Entrance]);
        Assert.True(level.Visited[level.Entrance]);
        Assert.Contains(Dungeon.Hero, Actor.All());
        // hero, hero's regen buff, hero's hunger buff, respawner, and every mob
        Assert.Equal(4 + level.Mobs.Count, Actor.All().Count);
        Assert.Equal(8, Dungeon.Hero.ViewDistance);
        Assert.Equal(1, View.ObserveCount);

        Dungeon.SwitchLevel(level, -1);
        Assert.Equal(level.Exit, Dungeon.Hero.Pos);
    }

    [Fact]
    public void SaveLevel_ThenLoadLevel_RoundTrips()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Entrance);
        Dungeon.SaveLevel();

        var loaded = Dungeon.LoadLevel();
        Assert.Null(Dungeon.Level);
        Assert.NotSame(level, loaded);
        Assert.Equal(level.Map, loaded.Map);
        Assert.Equal(level.Visited, loaded.Visited);
        Assert.Equal(level.Entrance, loaded.Entrance);
        Assert.Empty(Actor.All());
    }

    [Fact]
    public void LoadLevel_ForAnUnvisitedDepth_Throws()
    {
        Dungeon.Init();
        Dungeon.Depth = 9;
        Assert.Throws<KeyNotFoundException>(Dungeon.LoadLevel);
    }

    [Fact]
    public void Reset_ClearsTheLevelStore()
    {
        Dungeon.Init();
        Dungeon.NewLevel();
        Dungeon.SaveLevel();
        Dungeon.Reset();
        Dungeon.Depth = 1;

        Assert.Throws<KeyNotFoundException>(Dungeon.LoadLevel);
    }

    [Fact]
    public void ResetLevel_KeepsTheMapAndReturnsToTheEntrance()
    {
        Dungeon.Init();
        var level = Dungeon.NewLevel();
        Dungeon.SwitchLevel(level, level.Exit);
        var map = (int[])level.Map.Clone();
        Dungeon.ResetLevel();

        Assert.Equal(map, Dungeon.Level.Map);
        Assert.Equal(level.Entrance, Dungeon.Hero.Pos);
    }

    [Fact]
    public void Init_WearsClothArmourAndWieldsADagger_IdentifiedAndWithoutSpendingTime()
    {
        Dungeon.Init();
        var hero = Dungeon.Hero;

        var armor = Assert.IsType<ClothArmor>(hero.Belongings.Armor);
        var dagger = Assert.IsType<Dagger>(hero.Belongings.Weapon);
        Assert.True(armor.IsIdentified());
        Assert.True(dagger.IsIdentified());
        Assert.Equal(1, hero.Tier());
        Assert.Equal(0f, CooldownOf(hero));
    }

    [Fact]
    public void Init_PacksEightDartAndAFood_BesideTheKeyRing_InCategoryOrder()
    {
        Dungeon.Init();
        var pack = Dungeon.Hero.Belongings.Backpack.Items;

        Assert.Equal([
            typeof(Dart),
            typeof(Food),
            typeof(Keyring)
        ], pack.Select(item => item.GetType()));
        Assert.Equal(8, pack[0].Quantity());
        Assert.True(pack[0].CursedKnown);
        Assert.True(pack[1].IsIdentified());
    }

    [Fact]
    public void Init_KitsTheRogue_ToAttack12_Defense6_AndArmor2()
    {
        Dungeon.Init();
        var hero = Dungeon.Hero;

        Assert.Equal(12, hero.AttackSkill(new Rat()));
        Assert.Equal(6, hero.DefenseSkill(null));
        Assert.Equal(2, hero.Dr());
    }
    
    [Fact]
    public void Init_KitsTheRogue_ToHitForOneToEight()
    {
        Dungeon.Init();
        var hero = Dungeon.Hero;
        var rolls = new List<int>();
        for (var i = 0; i < 200; i++)
        {
            rolls.Add(hero.DamageRoll());
        }

        Assert.All(rolls, roll => Assert.InRange(roll, 1, 8));
        Assert.Contains(rolls, roll => roll > 1); // each roll is 1 with chance 1/32, so 200 rolls of 1 never happen
    }
}