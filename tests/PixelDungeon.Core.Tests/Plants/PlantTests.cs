using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Armor;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Plants;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Plants;

public class PlantTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#########",
        "#.......#",
        "#.......#",
        "#.......#",
        "#########",
    ];

    private static int C(int x, int y = 1) => TestLevel.At(x, y);

    public static IEnumerable<object[]> AllEightPlants() =>
    [
        [
            typeof(Firebloom)
        ],
        [
            typeof(Icecap)
        ],
        [
            typeof(Sorrowmoss)
        ],
        [
            typeof(Dreamweed)
        ],
        [
            typeof(Sungrass)
        ],
        [
            typeof(Earthroot)
        ],
        [
            typeof(Fadeleaf)
        ],
        [
            typeof(Rotberry)
        ]
    ];

    private sealed class Dummy : Char
    {
        public Dummy(int ht = 20)
        {
            HP = HT = ht;
        }

        protected override bool Act() => false;
    }

    private sealed class SeenMob : Mob
    {
        protected override bool Act() => false;

        public int TargetCell => Target;
    }

    private sealed class SpyArmor : ClothArmor
    {
        public int Seen = -1;

        public override int Proc(Char attacker, Char defender, int damage)
        {
            Seen = damage;
            return damage;
        }
    }

    private static void Tick(Actor actor) =>
        typeof(Actor)
            .GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(actor, null);

    private Hero PlaceViewedHero(int cell)
    {
        var hero = PlaceHero(cell);
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static Dummy PlaceDummy(int cell, int ht = 20)
    {
        var dummy = new Dummy(ht) { Pos = cell, Sprite = new FakeCharView() };
        Actor.OccupyCell(dummy);
        return dummy;
    }

    private static T Grow<T>(int cell) where T : Plant, new()
    {
        var plant = new T { Pos = cell };
        Dungeon.Level.Plants[cell] = plant;
        GameScene.Add(plant);
        return plant;
    }

    [Fact]
    public void Wither_UprootsKillsTheViewAndSplashesLeavesWhenVisible()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var plant = Grow<Firebloom>(C(3));
        var view = Assert.Single(View.PlantViews);

        plant.Wither();

        Assert.False(Dungeon.Level.Plants.ContainsKey(C(3)));
        Assert.Equal(1, view.Kills);
        Assert.Contains((C(3), 0xFF448822u, 6), View.Splashes);
    }

    [Fact]
    public void Wither_OutOfSight_DoesNotSplash()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        Plant plant = Grow<Firebloom>(C(3));
        Dungeon.Visible[C(3)] = false;

        plant.Wither();

        Assert.Empty(View.Splashes);
    }

    [Theory]
    [MemberData(nameof(AllEightPlants))]
    public void Activate_WithNoCharacter_WithersWithoutThrowing(Type type)
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        var plant = (Plant)Activator.CreateInstance(type);
        Assert.NotNull(plant);
        plant.Pos = C(3);
        GameScene.Add(plant);

        plant.Activate(null);

        Assert.False(Dungeon.Level.Plants.ContainsKey(C(3)));
        Assert.Equal(1, View.PlantViews[0].Kills);
    }

    [Fact]
    public void PlantSeed_OverAnExistingPlant_WithersItWithoutActivating()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        Grow<Firebloom>(C(3));

        var planted = Dungeon.Level.PlantSeed(new Icecap.Seed(), C(3));

        Assert.IsType<Icecap>(planted);
        Assert.Same(planted, Dungeon.Level.Plants[C(3)]);
        Assert.Equal(1, View.PlantViews[0].Kills);
        Assert.False(Dungeon.Level.Blobs.ContainsKey(typeof(Fire))); // Firebloom never activated
        Assert.Equal(2, View.PlantViews.Count); // new plant was handed to view
        Assert.Same(planted, View.AddedPlants[1]);
    }

    [Fact]
    public void Drop_OntoAPlant_ActivatesItWithNoCharacter()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        Grow<Firebloom>(C(4));

        Dungeon.Level.Drop(new Dart(), C(4));

        var fire = (Fire)Dungeon.Level.Blobs[typeof(Fire)];
        Assert.Equal(2, fire.Cur[C(4)]);
        Assert.False(Dungeon.Level.Plants.ContainsKey(C(4)));
    }

    [Fact]
    public void Firebloom_SeedsFireTwo()
    {
        LoadLevel(Room);
        var hero = PlaceHero(C(1));
        Grow<Firebloom>(C(1)).Activate(hero);

        Assert.Equal(2, ((Fire)Dungeon.Level.Blobs[typeof(Fire)]).Cur[C(1)]);
        Assert.Contains((C(1), 0xFFEE7722u, 5), View.Splashes);
    }

    [Fact]
    public void Icecap_FreezesTheThreeByThreeThroughOpenCells()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var near = PlaceDummy(C(4, 2));
        var diagonal = PlaceDummy(C(3, 1));
        var far = PlaceDummy(C(6, 2));
        var icecap = Grow<Icecap>(C(4, 1));

        icecap.Activate(null);

        Assert.NotNull(near.GetBuff<Frost>());
        Assert.NotNull(diagonal.GetBuff<Frost>());
        Assert.Null(far.GetBuff<Frost>());
    }

    [Theory]
    [InlineData(1, 4f)]
    [InlineData(3, 5f)]
    [InlineData(5, 6f)]
    [InlineData(10, 9f)]
    public void Sorrowmoss_PoisonsForFourPlusHalfDepth(int depth, float left)
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        Dungeon.Depth = depth;
        var dummy = PlaceDummy(C(3));
        var factor = Poison.DurationFactor(dummy);

        Grow<Sorrowmoss>(C(3)).Activate(dummy);

        var poison = dummy.GetBuff<Poison>();
        Assert.NotNull(poison);
        var field = typeof(Poison).GetField("Left",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(factor * left, (float)field.GetValue(poison));
        Assert.Contains((C(3), 0xFF8844FFu, 3), View.Splashes);
    }

    [Fact]
    public void Dreamweed_WithACharacter_SeedsConfusion400_WithoutOne_Nothing()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        Grow<Dreamweed>(C(3)).Activate(dummy);

        Assert.Equal(400, ((ConfusionGas)Dungeon.Level.Blobs[typeof(ConfusionGas)]).Cur[C(3)]);

        Grow<Dreamweed>(C(5)).Activate(null);
        Assert.Equal(0, ((ConfusionGas)Dungeon.Level.Blobs[typeof(ConfusionGas)]).Cur[C(5)]);
    }

    [Fact]
    public void SungrassHealth_HealsTenthOfHtEveryFiveTurns_AndStopsWhenMovedOrFull()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        dummy.HP = 10;
        Grow<Sungrass>(C(3)).Activate(dummy);
        var health = dummy.GetBuff<Sungrass.Health>();
        Assert.NotNull(health);
        Assert.Contains((C(3), 0xFFFFFF66u, 3), View.Splashes);

        Tick(health);

        Assert.Equal(12, dummy.HP);
        Assert.Equal(5f, CooldownOf(health));
        Assert.Contains((0xFF33FF33u, 1), ((FakeCharView)dummy.Sprite).BurstLog);

        dummy.HP = 19;
        Tick(health);
        Assert.Equal(20, dummy.HP);
        Assert.NotNull(dummy.GetBuff<Sungrass.Health>());

        Tick(health); // full: detaches
        Assert.Null(dummy.GetBuff<Sungrass.Health>());

        // Moving off the cell detaches it as well.
        dummy.HP = 10;
        Grow<Sungrass>(C(3)).Activate(dummy);
        health = dummy.GetBuff<Sungrass.Health>();
        dummy.Pos = C(4);
        Tick(health);
        Assert.Equal(10, dummy.HP);
        Assert.Null(dummy.GetBuff<Sungrass.Health>());
    }

    [Fact]
    public void SungrassHealth_SecondSungrass_KeepsTheFirstPosition()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        dummy.HP = 10;
        Grow<Sungrass>(C(3)).Activate(dummy);
        var first = dummy.GetBuff<Sungrass.Health>();
        dummy.Pos = C(4);

        Grow<Sungrass>(C(4)).Activate(dummy); // Buff.Affect finds the buff and reuses it

        Assert.Same(first, dummy.GetBuff<Sungrass.Health>());
        Tick(first);
        Assert.Null(dummy.GetBuff<Sungrass.Health>()); // still bound to C(3)
        Assert.Equal(10, dummy.HP);
    }

    [Fact]
    public void EarthrootArmor_Absorbs()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        var armor = Buff.Affect<Earthroot.Armor>(dummy);
        armor.LevelValue = 20;

        Assert.Equal(0, armor.Absorb(7));
        Assert.Equal(13, armor.LevelValue);
        Assert.Equal(0, armor.Absorb(13));
        Assert.Null(dummy.GetBuff<Earthroot.Armor>());

        var other = Buff.Affect<Earthroot.Armor>(dummy);
        other.LevelValue = 13;
        Assert.Equal(2, other.Absorb(15));
        Assert.Null(dummy.GetBuff<Earthroot.Armor>());
    }

    [Fact]
    public void EarthrootArmor_LevelRaisesOnly_ButActivationAssignsHt()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        var armor = Buff.Affect<Earthroot.Armor>(dummy);
        armor.LevelValue = 30;
        armor.Level(5);
        Assert.Equal(30, armor.LevelValue);
        armor.Level(40);
        Assert.Equal(40, armor.LevelValue);

        armor.LevelValue = 50;
        Grow<Earthroot>(C(3)).Activate(dummy); // assigns HT (20) directly, even downwards

        Assert.Equal(20, dummy.GetBuff<Earthroot.Armor>().LevelValue);
    }

    [Fact]
    public void EarthrootArmor_ShakesWhenVisible()
    {
        LoadLevel(Room);
        PlaceHero(C(1));
        Grow<Earthroot>(C(3)).Activate(null);

        Assert.Contains((1f, 0.4f), View.Shakes);
        Assert.Contains((C(3), 0xFF5D5D55u, 8), View.Splashes);
    }

    [Fact]
    public void EarthrootArmor_StopsWhenTheTargetMoves()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));
        var armor = Buff.Affect<Earthroot.Armor>(dummy);
        Tick(armor);
        Assert.NotNull(dummy.GetBuff<Earthroot.Armor>());
        Assert.Equal(1f, CooldownOf(armor));

        dummy.Pos = C(4);
        Tick(armor);

        Assert.Null(dummy.GetBuff<Earthroot.Armor>());
    }

    [Fact]
    public void HeroDefenseProc_EarthrootAbsorbsBeforeTheArmour()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        var spy = new SpyArmor();
        hero.Belongings.Armor = spy;
        var armor = Buff.Affect<Earthroot.Armor>(hero);
        armor.LevelValue = 5;

        var result = hero.DefenseProc(new Dummy(), 8);

        Assert.Equal(3, spy.Seen);
        Assert.Equal(3, result);
        Assert.Null(hero.GetBuff<Earthroot.Armor>());
    }

    [Fact]
    public void Fadeleaf_TeleportsTheHeroAndClearsItsAction()
    {
        var level = LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        hero.CurAction = new HeroAction.Move(C(5));
        level.RespawnCells = () => C(6, 3);
        Grow<Fadeleaf>(C(1)).Activate(hero);

        Assert.Equal(C(6, 3), hero.Pos);
        Assert.Null(hero.CurAction);
        Assert.Contains((C(1), 0xFFFFFFFFu, 3), View.Splashes);
    }

    [Fact]
    public void Fadeleaf_Mob_UsesUpToElevenRespawnDraws()
    {
        var level = LoadLevel(Room);
        PlaceHero(C(7, 3));
        var mob = new SeenMob { Pos = C(3), Sprite = new FakeCharView() };
        Actor.OccupyCell(mob);
        var draws = 0;
        level.RespawnCells = () =>
        {
            draws++;
            return -1;
        };

        Grow<Fadeleaf>(C(3)).Activate(mob);

        Assert.Equal(11, draws);
        Assert.Equal(C(3), mob.Pos);
        Assert.Empty(((FakeCharView)mob.Sprite).Placed);
    }

    [Fact]
    public void Fadeleaf_Mob_MovesToTheRespawnCell()
    {
        var level = LoadLevel(Room);
        PlaceHero(C(7, 3));
        var mob = new SeenMob { Pos = C(3), Sprite = new FakeCharView() };
        Actor.OccupyCell(mob);
        level.RespawnCells = () => C(6, 2);

        Grow<Fadeleaf>(C(3)).Activate(mob);

        Assert.Equal(C(6, 2), mob.Pos);
        Assert.Equal(C(6, 2), Assert.Single(((FakeCharView)mob.Sprite).Placed));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Fadeleaf_Mob_VisibilityFollowsThePlantCell(bool seen)
    {
        var level = LoadLevel(Room);
        PlaceHero(C(7, 3));
        level.RespawnCells = () => C(6, 2);
        Dungeon.Visible[C(3)] = seen;
        Dungeon.Visible[C(6, 2)] = !seen;
        var mob = new SeenMob { Pos = C(3), Sprite = new FakeCharView { Visible = !seen } };
        Actor.OccupyCell(mob);

        Grow<Fadeleaf>(C(3)).Activate(mob);

        Assert.Equal(seen, mob.Sprite.Visible); // the plant's cell, not the destination's
    }

    [Fact]
    public void Rotberry_SeedsToxicGasDropsASeedAndRootsForThree()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var dummy = PlaceDummy(C(3));

        Grow<Rotberry>(C(3)).Activate(dummy);

        Assert.Equal(100, ((ToxicGas)Dungeon.Level.Blobs[typeof(ToxicGas)]).Cur[C(3)]);
        var heap = Dungeon.Level.Heaps[C(3)];
        Assert.IsType<Rotberry.Seed>(heap.Peek());
        Assert.Equal(1, ((FakeHeapView)heap.Sprite).Drops);
        var roots = dummy.GetBuff<Roots>();
        Assert.NotNull(roots);
        Assert.Equal(3f, CooldownOf(roots));
        Assert.False(Dungeon.Level.Plants.ContainsKey(C(3)));
    }

    private SeenMob PlaceMob(int cell)
    {
        var mob = new SeenMob { Pos = cell, Sprite = new FakeCharView() };
        Dungeon.Level.Mobs.Add(mob);
        return mob;
    }

    [Fact]
    public void RotberrySeedCollect_Roars()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        var mob = PlaceMob(C(5));

        new Rotberry.Seed().Collect();

        Assert.Equal(hero.Pos, mob.TargetCell);
        Assert.True(View.Logged("The seed emits a roar that echoes throughout the dungeon!"));
        Assert.Contains((C(1), 0xFFFFFFFFu, 3), View.Splashes);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndChallenge);
    }

    [Fact]
    public void RotberrySeedCollect_WithAPouch_RoarsTwice()
    {
        LoadLevel(Room);
        PlaceViewedHero(C(1));
        new PixelDungeon.Core.Items.Bags.SeedPouch().Collect();
        View.Sounds.Clear();

        new Rotberry.Seed().Collect();

        Assert.Equal(2, View.Sounds.Count(s => s.Id == Assets.SndChallenge));
        Assert.Equal(2, View.Logs.Count(l => l.Text.Contains("roar")));
    }

    [Fact]
    public void RotberrySeedCollect_NoLevel_IsSilent()
    {
        PlaceViewedHero(C(1));
        Dungeon.Level = null;

        var collected = new Rotberry.Seed().Collect(Dungeon.Hero.Belongings.Backpack);

        Assert.True(collected);
        Assert.Empty(View.Logs);
        Assert.DoesNotContain(View.Sounds, s => s.Id == Assets.SndChallenge);
    }

    [Fact]
    public void LevitatingHero_DoesNotTriggerAPlant()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Buff.Affect<Levitation>(hero);
        Grow<Firebloom>(C(2));

        hero.Move(C(2));

        Assert.Equal(C(2), hero.Pos);
        Assert.True(Dungeon.Level.Plants.ContainsKey(C(2)));
        Assert.False(Dungeon.Level.Blobs.ContainsKey(typeof(Fire)));
    }

    [Fact]
    public void WalkingHero_TriggersAPlant()
    {
        LoadLevel(Room);
        var hero = PlaceViewedHero(C(1));
        Grow<Firebloom>(C(2));

        hero.Move(C(2));

        Assert.False(Dungeon.Level.Plants.ContainsKey(C(2)));
        Assert.Equal(2, ((Fire)Dungeon.Level.Blobs[typeof(Fire)]).Cur[C(2)]);
    }

    [Fact]
    public void FlyingMob_DoesNotTriggerAPlant()
    {
        LoadLevel(Room);
        PlaceHero(C(7, 3));
        var bat = new SeenMob { Pos = C(1), Sprite = new FakeCharView(), Flying = true };
        Actor.OccupyCell(bat);
        Grow<Firebloom>(C(2));

        bat.Move(C(2));

        Assert.True(Dungeon.Level.Plants.ContainsKey(C(2)));

        var walker = new SeenMob { Pos = C(4), Sprite = new FakeCharView() };
        Actor.OccupyCell(walker);
        Grow<Firebloom>(C(5));
        walker.Move(C(5));
        Assert.False(Dungeon.Level.Plants.ContainsKey(C(5)));
    }

    [Fact]
    public void Plants_Bundle_RoundTrip()
    {
        Dungeon.Depth = 1;
        var level = TestLevel.Create(Room);
        Dungeon.Level = level;
        level.Plants[C(2)] = new Sungrass { Pos = C(2) };
        level.Plants[C(5, 2)] = new Rotberry { Pos = C(5, 2) };

        var bundle = new Bundle();
        bundle.Put("level", level);
        var restored = (TestLevel)bundle.Get("level");

        Assert.IsType<Sungrass>(restored.Plants[C(2)]);
        Assert.IsType<Rotberry>(restored.Plants[C(5, 2)]);
        Assert.Equal(C(2), restored.Plants[C(2)].Pos);

        var buffs = new Bundle();
        buffs.Put("health", new Sungrass.Health());
        buffs.Put("armor", new Earthroot.Armor { LevelValue = 17 });
        var outer = new Bundle();
        outer.Put("b", buffs);
        var copy = outer.GetBundle("b");
        Assert.IsType<Sungrass.Health>(copy.Get("health"));
        Assert.Equal(17, ((Earthroot.Armor)copy.Get("armor")).LevelValue);
    }

    [Fact]
    public void Descriptions_AreVerbatim()
    {
        Assert.Equal("When something touches a Firebloom, it bursts into flames.", new Firebloom().Desc());
        Assert.Equal("Upon touching a Dreamweed it secretes a glittering cloud of confusing gas.",
            new Dreamweed().Desc());
        Assert.Equal("Berries of this shrub taste like sweet, sweet death.", new Rotberry().Desc());
        Assert.Equal("When a creature touches an Earthroot, its roots create a kind of natural armor around it.",
            new Earthroot().Desc());
        Assert.Null(new Plant().Desc());
    }

    [Fact]
    public void Names_AndImages_MatchTheJava()
    {
        var plants = new Plant[]
        {
            new Firebloom(), new Icecap(), new Sorrowmoss(), new Dreamweed(),
            new Sungrass(), new Earthroot(), new Fadeleaf(), new Rotberry(),
        };
        string[] names =
        [
            "Firebloom",
            "Icecap",
            "Sorrowmoss",
            "Dreamweed",
            "Sungrass",
            "Earthroot",
            "Fadeleaf",
            "Rotberry"
        ];
        for (var i = 0; i < plants.Length; i++)
        {
            Assert.Equal(i, plants[i].Image);
            Assert.Equal(names[i], plants[i].PlantName);
        }
    }
}