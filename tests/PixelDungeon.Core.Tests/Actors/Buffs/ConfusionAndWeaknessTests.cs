using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Items.Weapon.Melee;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class ConfusionAndWeaknessTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#.....#",
        "#.....#",
        "#.....#",
        "#.....#",
        "#######"
    ];

    private sealed class Dummy : Char
    {
        public Dummy()
        {
            HP = HT = 20;
        }

        protected override bool Act() => false;
    }

    private static int C(int x, int y = 1) => TestLevel.At(x, y);

    private Core.Actors.Hero.Hero PlaceViewedHero(int cell)
    {
        var hero = PlaceHero(cell);
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static Dummy PlaceDummy(int cell)
    {
        var dummy = new Dummy { Pos = cell };
        Actor.OccupyCell(dummy);
        return dummy;
    }

    // ---- Vertigo in Char.Move ----

    [Fact]
    public void Vertigo_AdjacentStep_DrawsOneNeighbour()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(3, 3));
        Buff.Affect<Vertigo>(hero);
        Random.Seed(5);
        var k = Random.Int(8);
        Random.Seed(5);

        hero.Move(C(4, 3));

        Assert.Equal(C(3, 3) + Level.Neighbours8[k], hero.Pos);
    }

    [Fact]
    public void Vertigo_BlockedStep_AbortsWithoutMoving()
    {
        LoadLevel(
            "###",
            "#/#",
            "###"
        ); // open door, walled in on every side
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Vertigo>(hero);

        hero.Move(C(2));

        Assert.Equal(C(1), hero.Pos);
        Assert.Equal(Terrain.OpenDoor, Dungeon.Level.Map[C(1)]); // door-leave skipped too
    }

    [Fact]
    public void Vertigo_OccupiedStep_AbortsWithoutMoving()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        ); // the only open neighbors are the two sides
        var hero = PlaceViewedHero(C(2));
        PlaceDummy(C(1));
        PlaceDummy(C(3));
        Buff.Affect<Vertigo>(hero);

        for (var seed = 1; seed <= 20; seed++)
        {
            Random.Seed(seed);
            hero.Move(C(3));
            Assert.Equal(C(2), hero.Pos);
        }
    }

    [Fact]
    public void Vertigo_NotDizzy_DrawsNothing()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(3, 3));
        Random.Seed(7);
        var expected = Random.Int(1000);
        Random.Seed(7);

        dummy.Move(C(4, 3));

        Assert.Equal(C(4, 3), dummy.Pos);
        Assert.Equal(expected, Random.Int(1000));
    }

    [Fact]
    public void Vertigo_NonAdjacentStep_DrawsNothing()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1, 1));
        Buff.Affect<Vertigo>(dummy);
        Random.Seed(7);
        var expected = Random.Int(1000);
        Random.Seed(7);

        dummy.Move(C(4, 1));

        Assert.Equal(C(4, 1), dummy.Pos);
        Assert.Equal(expected, Random.Int(1000));
    }

    [Fact]
    public void Vertigo_AbortedStep_RepressesOwnCell()
    {
        LoadLevel(
            "###",
            "#T#",
            "###"
        );
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Vertigo>(hero);

        hero.Move(C(2));

        Assert.Equal(C(1), hero.Pos);
        Assert.Equal(Terrain.InactiveTrap, Dungeon.Level.Map[C(1)]);
    }

    [Fact]
    public void Vertigo_LevitatingHeroAbortedStep_DoesNotPress()
    {
        LoadLevel("###", "#T#", "###");
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Vertigo>(hero);
        Buff.Affect<Levitation>(hero);

        hero.Move(C(2));

        Assert.Equal(Terrain.ToxicTrap, Dungeon.Level.Map[C(1)]);
    }

    [Fact]
    public void Vertigo_MobAbortedStep_StillPresses()
    {
        LoadLevel("#####", "#T#.#", "#####");
        PlaceHero(C(3));
        var rat = new Rat { Pos = C(1), Sprite = new FakeCharView() };
        Actor.OccupyCell(rat);
        Buff.Affect<Vertigo>(rat);

        rat.Move(C(2));

        Assert.Equal(C(1), rat.Pos);
        Assert.Equal(Terrain.InactiveTrap, Dungeon.Level.Map[C(1)]);
    }

    [Fact]
    public void Vertigo_StumbleIntoChasm_Falls()
    {
        // Pick the neighbor the seeded draw will choose, and make it the chasm.
        Random.Seed(3);
        var k = Random.Int(8);
        var pit = C(3, 3) + Level.Neighbours8[k];
        var rows = new string[7];
        for (var y = 0; y < 7; y++)
        {
            var row = new char[7];
            for (var x = 0; x < 7; x++)
            {
                var wall = x == 0 || y == 0 || x == 6 || y == 6;
                row[x] = C(x, y) == pit
                    ? 'x'
                    : wall
                        ? '#'
                        : '.';
            }

            rows[y] = new string(row);
        }

        LoadLevel(rows);
        var hero = PlaceViewedHero(C(3, 3));
        Buff.Affect<Vertigo>(hero);
        Random.Seed(3);

        hero.Move(C(4, 3));
        // hero meant to step east
        // draw sends it into the pit, with no confirmation window

        Assert.Equal(pit, hero.Pos);
        Assert.Equal(new[] { InterlevelMode.Fall }, View.SwitchedModes);
        Assert.Empty(View.Windows);
    }

    [Fact]
    public void Vertigo_ClickPathOfTenCells_SpendsTimeEveryStepAndTerminates()
    {
        LoadLevel(
            "############",
            "#..........#",
            "############"
        );
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(C(1));
        var view = new FakeCharView();
        hero.Sprite = view;
        Buff.Prolong<Vertigo>(hero, 30f);

        hero.Handle(C(10));
        var calls = 0;
        while (hero.CurAction != null && calls < 50)
        {
            Actor.Process();
            calls++;
        }

        Assert.Null(hero.CurAction);
        Assert.True(calls < 50);
        Assert.Equal(C(10), hero.Pos);
        // Every step, aborted or not, told the view to move; a stumble that went nowhere still cost a turn.
        Assert.True(view.Moves.Count >= 10);
        Assert.Contains(view.Moves, m => m.From == m.To);
    }

    [Fact]
    public void Vertigo_Add_ShowsDizzyAndWarnsAndInterrupts()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        hero.CurAction = new HeroAction.Move(C(4));

        Buff.Affect<Vertigo>(hero);

        Assert.Contains((StatusColor.Negative, "dizzy"), ((FakeCharView)hero.Sprite).Statuses);
        Assert.True(View.Logged("Everything is spinning around you!"));
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Vertigo_Basics()
    {
        Assert.Equal(10f, Vertigo.DurationConst);
        Assert.Equal(10f, Vertigo.Duration(new Core.Actors.Hero.Hero()));
        Assert.Equal(BuffIndicator.Vertigo, new Vertigo().Icon());
        Assert.Equal("Vertigo", new Vertigo().ToString());
    }

    // ---- Weakness ----

    [Fact]
    public void Weakness_Attach_WeakensAndDetachRestores()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Assert.Equal(hero.STR, hero.Str());

        Buff.Prolong<Weakness>(hero, Weakness.Duration(hero));

        Assert.True(hero.Weakened);
        Assert.Equal(hero.STR - 2, hero.Str());

        Buff.Detach<Weakness>(hero);

        Assert.False(hero.Weakened);
        Assert.Equal(hero.STR, hero.Str());
    }

    [Fact]
    public void Weakness_Basics()
    {
        Assert.Equal(40f, Weakness.Duration(new Core.Actors.Hero.Hero()));
        Assert.Equal(BuffIndicator.Weakness, new Weakness().Icon());
        Assert.Equal("Weakened", new Weakness().ToString());
    }

    [Fact]
    public void Weakness_Add_WarnsWithoutInterrupting()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        var action = new HeroAction.Move(C(4));
        hero.CurAction = action;

        Buff.Prolong<Weakness>(hero, 40f);

        Assert.True(View.Logged("You feel weakened!"));
        Assert.Same(action, hero.CurAction);
    }

    [Fact]
    public void Weakness_ClothArmourStr10_SpeedAndDefense()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        hero.Belongings.Armor = new ClothArmor { Str = 10 }; // weakened Str 8: encumbrance 2
        Buff.Prolong<Weakness>(hero, 40f);

        Assert.Equal(0.5917160f, hero.Speed(), 5); // 1.3^-2
        Assert.Equal(2, hero.DefenseSkill(null)); // (int)(5 / 1.5^2) = (int)2.22
    }

    [Fact]
    public void Weakness_DaggerStr10_AccuracyAndSpeedFactors()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        var dagger = new Dagger();
        Assert.Equal(10, dagger.Str);
        Buff.Prolong<Weakness>(hero, 40f);

        Assert.Equal(0.533333f, dagger.AccuracyFactor(hero), 5); // 1.2 / 1.5^2
        Assert.Equal(1.44f, dagger.SpeedFactor(hero), 5); // 1 * 1.2^2
    }

    [Fact]
    public void Weakness_Bundle_RestoresWeakened()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Buff.Prolong<Weakness>(hero, 40f);
        var bundle = new Bundle();
        bundle.Put("hero", hero);

        var restored = (Core.Actors.Hero.Hero)bundle.Get("hero");

        Assert.NotNull(restored.GetBuff<Weakness>());
        Assert.True(restored.Weakened);
        Assert.Equal(restored.STR - 2, restored.Str());
    }

    [Fact]
    public void Healing_CuresWeaknessInTheJavaOrder()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Poison>(hero).Set(5);
        Buff.Prolong<Cripple>(hero, 5f);
        Buff.Prolong<Weakness>(hero, 40f);
        Buff.Affect<Bleeding>(hero).Set(3);

        PotionOfHealing.Heal(hero);

        Assert.Null(hero.GetBuff<Poison>());
        Assert.Null(hero.GetBuff<Cripple>());
        Assert.Null(hero.GetBuff<Weakness>());
        Assert.False(hero.Weakened);
        Assert.Null(hero.GetBuff<Bleeding>());
    }

    [Fact]
    public void RemoveCurse_CuresWeaknessEvenWhenNothingIsCursed()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Buff.Prolong<Weakness>(hero, 40f);
        var scroll = new ScrollOfRemoveCurse();
        scroll.Collect();

        scroll.Execute(hero);

        Assert.Null(hero.GetBuff<Weakness>());
        Assert.False(hero.Weakened);
        Assert.True(View.Logged("nothing happens"));
    }

    [Fact]
    public void Carpaccio_RollTwo_CuresWeakness()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        Dungeon.Hero = new SpyHero();
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Hunger>(hero).Satisfy(-300);
        Buff.Prolong<Weakness>(hero, 40f);
        var food = new FrozenCarpaccio();
        food.Collect();
        var seed = 1;
        while (true)
        {
            Random.Seed(seed);
            if (Random.Int(5) == 2)
            {
                break;
            }

            seed++;
        }

        Random.Seed(seed);

        food.Execute(hero, Food.AcEat);

        Assert.Null(hero.GetBuff<Weakness>());
        Assert.False(hero.Weakened);
    }

    // ---- Dungeon.FindPath ----

    [Fact]
    public void FindPath_Amok_PathsThroughAvoidCells()
    {
        LoadLevel(
            "#####",
            "#.x.#",
            "#####"
        );
        PlaceHero(C(3));
        var walker = new Dummy { Pos = C(1) };
        Actor.Add(walker);
        var visible = new bool[Level.Length];

        Assert.Equal(-1, Dungeon.FindPath(walker, C(1), C(3), Level.Passable, visible));

        Buff.Affect<Amok>(walker);

        Assert.Equal(C(2), Dungeon.FindPath(walker, C(1), C(3), Level.Passable, visible));
    }
}