using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Actors.Mobs.Npcs;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class MobScrollTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#######"
    ];

    private static readonly string[] Divided =
    [
        "#########",
        "#...#...#",
        "#########"
    ];

    private sealed class SleeplessMob : Mob
    {
        private static readonly HashSet<Type> Immune = [typeof(Sleep)];

        public SleeplessMob()
        {
            Name = "insomniac";
            HP = HT = 10;
        }

        public override HashSet<Type> Immunities() => Immune;
    }

    private Hero PlaceReader(string[] rows, int x, int y = 1)
    {
        LoadLevel(rows);
        var hero = PlaceHero(TestLevel.At(x, y));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static T AddMob<T>(int cell) where T : Mob, new()
    {
        var mob = new T { Pos = cell, Sprite = new FakeCharView() };
        GameScene.Add(mob);
        return mob;
    }

    private static T Read<T>(Hero hero) where T : Scroll, new()
    {
        var scroll = new T();
        scroll.Collect();
        scroll.Execute(hero);
        return scroll;
    }

    // ---- Challenge ----

    [Fact]
    public void Challenge_BeckonsEveryMob_AndEnragesTheVisibleOnesForTheirDistance()
    {
        var hero = PlaceReader(Divided, 1);
        var near = AddMob<Rat>(TestLevel.At(3, 1));
        var far = AddMob<Rat>(TestLevel.At(6, 1)); // behind the wall
        Dungeon.Observe();

        var scroll = Read<ScrollOfChallenge>(hero);

        Assert.Equal(2f, CooldownOf(near.GetBuff<Rage>())); // Level.distance(reader, mob) = 2
        Assert.Null(far.GetBuff<Rage>());
        Assert.Same(near.WanderingState, near.State); // Beckon wakes a sleeping rat to wander toward the reader
        Assert.Same(far.WanderingState, far.State);
        Assert.Contains(("The scroll emits a challenging roar that echoes throughout the dungeon!", LogKind.Warning),
            View.Logs);
        Assert.Contains((0xFFFFFFFFu, 3), ((FakeCharView)hero.Sprite).BurstLog); // Speck.SCREAM
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndChallenge);
        Assert.True(scroll.IsKnown());
        Assert.Equal(1, ((FakeCharView)hero.Sprite).Reads);
    }

    // ---- Terror ----

    [Fact]
    public void Terror_FrightensALoneMobInView_WithTheReaderAsTheSource_AndNamesIt()
    {
        var hero = PlaceReader(Divided, 1);
        var rat = AddMob<Rat>(TestLevel.At(3, 1));
        var hidden = AddMob<Rat>(TestLevel.At(6, 1));
        Dungeon.Observe();

        var scroll = Read<ScrollOfTerror>(hero);

        var terror = rat.GetBuff<Terror>();
        Assert.Equal(Terror.Duration, CooldownOf(terror));
        Assert.Equal(hero.Id(), terror.Object);
        Assert.Same(rat.FleeingState, rat.State);
        Assert.Null(hidden.GetBuff<Terror>());
        Assert.Contains(
            ("The scroll emits a brilliant flash of red light and the " + rat.Name + " flees!", LogKind.Info),
            View.Logs);
        Assert.Contains((0xFFFF0000u, 5), ((FakeCharView)hero.Sprite).BurstLog);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void Terror_WithNoMobInView_OnlyFlashes()
    {
        var hero = PlaceReader(Room, 1);
        Dungeon.Observe();

        Read<ScrollOfTerror>(hero);

        Assert.Contains(("The scroll emits a brilliant flash of red light", LogKind.Info), View.Logs);
    }

    [Fact]
    public void Terror_WithSeveralMobsInView_SaysTheMonstersFlee()
    {
        var hero = PlaceReader(Room, 1);
        AddMob<Rat>(TestLevel.At(3, 1));
        AddMob<Rat>(TestLevel.At(5, 1));
        Dungeon.Observe();

        Read<ScrollOfTerror>(hero);

        Assert.Contains(("The scroll emits a brilliant flash of red light and the monsters flee!", LogKind.Info),
            View.Logs);
    }

    // ---- Lullaby ----

    [Fact]
    public void Lullaby_PutsALoneMobInViewToSleep_AndNamesIt()
    {
        var hero = PlaceReader(Room, 1);
        var rat = AddMob<Rat>(TestLevel.At(4, 1));
        rat.State = rat.HuntingState;
        Dungeon.Observe();

        var scroll = Read<ScrollOfLullaby>(hero);

        Assert.NotNull(rat.GetBuff<Sleep>());
        Assert.Same(rat.SleepingState, rat.State);
        Assert.Contains(("The scroll utters a soothing melody and the " + rat.Name + " falls asleep!", LogKind.Info),
            View.Logs);
        Assert.Contains((0xFFAADDFFu, 5), ((FakeCharView)hero.Sprite).BurstLog);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndLullaby);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void Lullaby_AMobImmuneToSleep_IsNotCounted()
    {
        var hero = PlaceReader(Room, 1);
        var mob = AddMob<SleeplessMob>(TestLevel.At(4, 1));
        Dungeon.Observe();

        Read<ScrollOfLullaby>(hero);

        Assert.Null(mob.GetBuff<Sleep>());
        Assert.Contains(("The scroll utters a soothing melody.", LogKind.Info), View.Logs);
    }

    [Fact]
    public void Lullaby_WithSeveralMobsInView_SaysTheMonstersFallAsleep()
    {
        var hero = PlaceReader(Room, 1);
        AddMob<Rat>(TestLevel.At(3, 1));
        AddMob<Rat>(TestLevel.At(5, 1));
        Dungeon.Observe();

        Read<ScrollOfLullaby>(hero);

        Assert.Contains(("The scroll utters a soothing melody and the monsters fall asleep!", LogKind.Info), View.Logs);
    }

    // ---- Psionic Blast ----

    [Fact]
    public void PsionicBlast_FlashesBlindsAndHurtsEveryMobInView_ThenBlindsTheReader()
    {
        var hero = PlaceReader(Divided, 1);
        var seen = AddMob<Rat>(TestLevel.At(3, 1));
        var hidden = AddMob<Rat>(TestLevel.At(6, 1));
        Dungeon.Observe();
        var seenHp = seen.HP;
        var hiddenHp = hidden.HP;
        Random.Seed(12);
        var blind = Random.Int(3, 6);
        var damage = Random.IntRange(1, seen.HT * 2 / 3);
        var readerBlind = Random.Int(3, 6);
        Random.Seed(12);

        var scroll = Read<ScrollOfPsionicBlast>(hero);

        Assert.Equal(new[] { 0xFFFFFFu }, View.Flashes);
        Assert.Equal(blind, CooldownOf(seen.GetBuff<Blindness>()));
        Assert.Equal(seenHp - damage, seen.HP);
        Assert.Null(hidden.GetBuff<Blindness>());
        Assert.Equal(hiddenHp, hidden.HP);
        Assert.Equal(readerBlind, CooldownOf(hero.GetBuff<Blindness>()));
        Assert.Contains(("You are blinded!", LogKind.Warning), View.Logs);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndBlast);
        Assert.False(Dungeon.Visible[TestLevel.At(3, 1)]); // observed while blind: only the block around the reader
        Assert.True(scroll.IsKnown());
    }

    // ---- Mirror Image ----

    [Fact]
    public void MirrorImage_PlacesThreeImagesOnFreeNeighbouringCells_AndLearnsTheScroll()
    {
        var hero = PlaceReader(
            [
                "#####",
                "#...#",
                "#...#",
                "#...#",
                "#####"
            ],
            2, 2);

        var scroll = Read<ScrollOfMirrorImage>(hero);

        var images = Dungeon.Level.Mobs.OfType<MirrorImage>().ToList();
        Assert.Equal(3, images.Count);
        Assert.All(images, image => Assert.True(Level.Adjacent(hero.Pos, image.Pos)));
        Assert.Equal(3, images.Select(image => image.Pos).Distinct().Count());
        Assert.Equal(3, View.AddedMobs.OfType<MirrorImage>().Count());
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndRead);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void MirrorImage_WithOneFreeNeighbour_PlacesOneImage()
    {
        var hero = PlaceReader(
            [
                "#####",
                "#...#",
                "#####"
            ],
            1);

        var scroll = Read<ScrollOfMirrorImage>(hero);

        var image = Assert.Single(Dungeon.Level.Mobs.OfType<MirrorImage>());
        Assert.Equal(TestLevel.At(2, 1), image.Pos);
        Assert.True(scroll.IsKnown());
    }

    [Fact]
    public void MirrorImage_WithNoFreeNeighbour_PlacesNone_AndStaysUnknown()
    {
        var hero = PlaceReader(
            [
                "#####",
                "#...#",
                "#####"
            ],
            1);
        AddMob<Rat>(TestLevel.At(2, 1));

        var scroll = Read<ScrollOfMirrorImage>(hero);

        Assert.Empty(Dungeon.Level.Mobs.OfType<MirrorImage>());
        Assert.False(scroll.IsKnown());
        Assert.Equal(1, ((FakeCharView)hero.Sprite).Reads); // the turn is spent anyway
    }
}