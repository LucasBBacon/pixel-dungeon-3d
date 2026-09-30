using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Levels;
using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Levels;

public class LevelPipelineTests : DungeonFixture
{
    [Fact]
    public void Create_BuildsFlags_AndDepthOneHasNoFeeling()
    {
        Dungeon.Depth = 1;
        var level = TestLevel.Create(
            "#####",
            "#.<.#",
            "#####"
        );
        Assert.True(Level.Passable[TestLevel.At(1, 1)]);
        Assert.Equal(LevelFeeling.None, level.Feeling);
        Assert.Empty(level.Mobs);
        Assert.Empty(level.Heaps);
        Assert.Equal(TestLevel.At(2, 1), level.Entrance);
    }

    [Fact]
    public void Create_AtDepthThree_RollsEverythingAcrossSeeds()
    {
        Dungeon.Depth = 3;
        var seen = new HashSet<LevelFeeling>();
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            seen.Add(TestLevel.Create("#").Feeling);
        }

        Assert.Contains(LevelFeeling.None, seen);
        Assert.Contains(LevelFeeling.Chasm, seen);
        Assert.Contains(LevelFeeling.Water, seen);
        Assert.Contains(LevelFeeling.Grass, seen);
    }

    [Fact]
    public void Create_OnAChasmLevel_FillsWithChasmBeforeBuilding()
    {
        Dungeon.Depth = 3;
        for (var seed = 1; seed <= 100; seed++)
        {
            Random.Seed(seed);
            var level = TestLevel.Create("#");
            if (level.Feeling != LevelFeeling.Chasm) continue;
            Assert.True(Level.Pit[TestLevel.At(10, 10)]);
            return;
        }

        Assert.Fail("no chasm level in 100 seeds");
    }

    [Fact]
    public void Create_CountsTheStrengthPotionQuotaEventThoughItemsAreDeferred()
    {
        Dungeon.Depth = 4;
        TestLevel.Create("#");
        Assert.Equal(1, Dungeon.PotionOfStrength);
    }

    [Fact]
    public void Quotas_FollowTables()
    {
        Dungeon.Depth = 4;
        Dungeon.PotionOfStrength = 0;
        Assert.True(Dungeon.PosNeeded());
        Dungeon.PotionOfStrength = 2;
        Assert.False(Dungeon.PosNeeded());

        Dungeon.Depth = 5;
        Dungeon.ScrollsOfUpgrade = 0;
        Assert.True(Dungeon.SouNeeded());

        Dungeon.Depth = 12;
        Dungeon.ScrollsOfEnchantment = 0;
        Assert.True(Dungeon.SoeNeeded());
        Dungeon.Depth = 0;
        Assert.False(Dungeon.SoeNeeded());

        Dungeon.Depth = 30;
        Assert.False(Dungeon.PosNeeded());
    }

    [Fact]
    public void AddItemToSpawn_IgnoresNull_AndPrizeDrawsFromTheList()
    {
        var level = TestLevel.FromRows("#");
        level.AddItemToSpawn(null);
        Assert.Equal(0, level.SpawnCount);
        Assert.Null(level.ItemToSpawnAsPrize());

        var item = new Item();
        level.AddItemToSpawn(item);
        Item prize = null;
        for (var i = 0; i < 50 && prize == null; i++)
        {
            prize = level.ItemToSpawnAsPrize();
        }

        Assert.Same(item, prize);
        Assert.Equal(0, level.SpawnCount);
    }

    [Fact]
    public void Respawner_RunsWithoutMobsToSpawn()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Spend(60);
        var respawner = Dungeon.Level.Respawner();
        Assert.NotNull(respawner);
        Actor.Add(respawner);
        Actor.Process(); // respawner at 0 and 50, then the hero at 60
        Assert.Empty(Dungeon.Level.Mobs);
    }

    [Fact]
    public void Drop_CreatesAHeap_AndTellsTheView()
    {
        var level = LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        var cell = TestLevel.At(2, 1);
        var item = new Item();
        var heap = level.Drop(item, cell);
        Assert.Same(heap, level.Heaps[cell]);
        Assert.Same(item, heap.Peek());
        Assert.Contains(heap, View.AddedHeaps);

        var again = level.Drop(new Item(), cell);
        Assert.Same(heap, again);
        Assert.Equal(2, heap.Size());
    }

    [Fact]
    public void Drop_OntoAChasm_FallsToTheNextDepth()
    {
        Dungeon.Depth = 3;
        var level = LoadLevel(
            "###",
            "#x#",
            "#x#",
            "###"
        );
        var item = new Item();
        var heap = level.Drop(item, TestLevel.At(1, 2));
        Assert.Empty(level.Heaps);
        Assert.Contains(heap, View.DiscardedHeaps);
        Assert.Contains(item, Dungeon.DroppedItems[4]);
    }

    [Fact]
    public void Drop_OntoAnAlchemyPot_LandsOnNeighbouringSpecialFloor()
    {
        var level = LoadLevel(
            "####",
            "#AP#",
            "####"
        );
        level.Drop(new Item(), TestLevel.At(1, 1));
        Assert.True(level.Heaps.ContainsKey(TestLevel.At(2, 1)));
        Assert.False(level.Heaps.ContainsKey(TestLevel.At(1, 1)));
    }

    [Fact]
    public void Drop_OntoALockedChest_MovesToPassableNeighbour()
    {
        var level = LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        var cell = TestLevel.At(2, 1);
        level.Heaps[cell] = new Heap { Pos = cell, Type = HeapType.LockedChest };
        var heap = level.Drop(new Item(), cell);
        Assert.NotEqual(cell, heap.Pos);
        Assert.True(Level.Passable[heap.Pos]);
    }

    [Fact]
    public void Bundle_RoundTripsMapVisitedMappedStairsAndHeaps()
    {
        Dungeon.Depth = 1;
        var level = TestLevel.Create(
            "######",
            "#<..>#",
            "######"
        );
        level.Visited[TestLevel.At(1, 1)] = true;
        level.Mapped[TestLevel.At(4, 1)] = true;
        level.Heaps[TestLevel.At(2, 1)] = new Heap
        {
            Pos = TestLevel.At(2, 1),
            Type = HeapType.Chest
        };

        var bundle = new Bundle();
        bundle.Put("level", level);
        var restored = (TestLevel)bundle.Get("level");

        Assert.Equal(level.Map, restored.Map);
        Assert.Equal(level.Visited, restored.Visited);
        Assert.Equal(level.Mapped, restored.Mapped);
        Assert.Equal(level.Entrance, restored.Entrance);
        Assert.Equal(level.Exit, restored.Exit);
        Assert.Equal(HeapType.Chest, restored.Heaps[TestLevel.At(2, 1)].Type);
        Assert.True(Level.Passable[TestLevel.At(2, 1)]); // flags rebuild on restore
    }

    [Fact]
    public void RegularLevel_CreateItems_DropsNothingWhileGeneratorIsASkeleton_AndBundleKeepsRooms()
    {
        Dungeon.Depth = 1;
        var level = new BareRegularLevel();
        level.Create();
        Assert.Empty(level.Heaps);

        var bundle = new Bundle();
        bundle.Put("level", level);
        var restored = (BareRegularLevel)bundle.Get("level");
        Assert.Equal(level.RoomSet.Count, restored.RoomSet.Count);
        Assert.Equal(level.RoomSet.Count(r => r.Type == RoomType.Standard),
            restored.RoomSet.Count(r => r.Type == RoomType.Standard));
        Assert.Equal(level.Map, restored.Map);
    }
}