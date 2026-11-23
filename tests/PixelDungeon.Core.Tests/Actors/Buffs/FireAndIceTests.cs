using System.Reflection;
using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Actors.Mobs.Npcs;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Items.Scrolls;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.Utils;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class FireAndIceTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#.,~..#",
        "#######",
    ];

    private sealed class Dummy : Char
    {
        public readonly HashSet<Type> Immune = [];
        
        public Dummy(int ht)
        {
            HP = HT = ht;
        }

        protected override bool Act() => false;

        public override HashSet<Type> Immunities() => Immune;
    }

    private static int C(int x, int y = 1) => TestLevel.At(x, y);

    private static void Tick(Actor actor) =>
        typeof(Actor).GetMethod("Act", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(actor, null);

    private static Dummy PlaceDummy(int cell, int hp, int ht)
    {
        var dummy = new Dummy(ht) { Pos = cell, HP = hp };
        Actor.OccupyCell(dummy);
        return dummy;
    }

    private Core.Actors.Hero.Hero PlaceBareHero(int cell)
    {
        var hero = PlaceHero(cell);
        hero.Sprite = new FakeCharView();
        hero.HP = hero.HT = 20;
        hero.Belongings.Backpack.Items.Clear();
        return hero;
    }

    private static float Threshold(int hp, int ht) => (2 + (float)hp / ht) / 3;
    
    [Fact]
    public void Burning_Act_DealsOnToFourDamage()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);
        Random.Seed(5);
        var expected = Random.Int(1, 5);
        Random.Seed(5);

        Tick(burning);
        
        Assert.InRange(expected, 1, 4);
        Assert.Equal(20 - expected, dummy.HP);
    }

    [Fact]
    public void Burning_DetachRoll_DetachesExactlyWhenTheFloatExceedsTwoPlusHpOverHtOverThree()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 10, 20);
        var detachedCount = 0;
        var keptCount = 0;

        for (var s = 1; s <= 200; s++)
        {
            Random.Seed(s);
            var d = Random.Int(1, 5);
            var f = Random.Float();
            var expected = f > (2 + (10 - d) / 20f) / 3;

            dummy.HP = 10;
            var burning = Buff.Affect<Burning>(dummy);
            burning.Reignite(dummy);
            Random.Seed(s);
            Tick(burning);

            var detached = dummy.GetBuff<Burning>() == null;
            Assert.Equal(expected, detached);
            if (detached)
            {
                detachedCount++;
            }
            else
            {
                keptCount++;
            }
            Buff.Detach<Burning>(dummy);
        }
        
        Assert.True(detachedCount > 0);
        Assert.True(keptCount > 0);
    }

    [Fact]
    public void Burning_TargetStandsInWater_Detaches()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(3, 2), 20, 20);
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);
        
        Tick(burning);
        
        Assert.Null(dummy.GetBuff<Burning>());
    }

    [Fact]
    public void Burning_FlyingTargetOverWater_KeepsBurning()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(3, 2), 20, 20);
        dummy.Flying = true;
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);

        var seed = 1;
        while (true)
        {
            // seed whose detach float stays under threshold for any HP of 16 to 19 / 20 (at least 0.9333)
            Random.Seed(seed);
            Random.Int(1, 5);
            if (Random.Float() <= 0.5f)
            {
                break;
            }

            seed++;
        }
        Random.Seed(seed);
        
        Tick(burning);
        
        Assert.NotNull(dummy.GetBuff<Burning>());
    }

    [Fact]
    public void Burning_HeroWithItems_DrawsThePackItemBeforeTheDetachRoll()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        new Food().Collect();
        var differing = 0;

        for (var s = 1; s <= 100; s++)
        {
            Random.Seed(s);
            var d = Random.Int(1, 5);
            Random.Int(1);
            var f = Random.Float();
            var expected = f > Threshold(20 - d, 20);
            
            Random.Seed(s);
            Random.Int(1, 5);
            if ((Random.Float() > Threshold(20 - d, 20)) != expected)
            {
                differing++;
            }

            hero.HP = 20;
            var burning = Buff.Affect<Burning>(hero);
            burning.Reignite(hero);
            Random.Seed(s);
            Tick(burning);
            
            Assert.Equal(expected, hero.GetBuff<Burning>() == null);
            Buff.Detach<Burning>(hero);
        }
        
        Assert.True(differing > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Burning_HeroWithAScroll_BurnsItUpAndLogsItsName(bool known)
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        var scroll = new ScrollOfIdentify();
        if (known) scroll.SetKnown();
        scroll.Collect();
        var name = scroll.ToString();
        var burning =  Buff.Affect<Burning>(hero);
        burning.Reignite(hero); 
        
        Tick(burning);
        
        Assert.Empty(hero.Belongings.Backpack.Items);
        Assert.True(View.Logged($"{name} burns up!"));
        Assert.Contains((hero.Pos, 0xFF22EE66u, 6), View.Splashes);
        Assert.Contains((Assets.SndBurning, 1f), View.Sounds);
    }

    [Fact]
    public void Burning_HeroWithMysteryMeat_ChargrillsIt()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        new MysteryMeat().Collect();
        var burning = Buff.Affect<Burning>(hero);
        burning.Reignite(hero);
        
        Tick(burning);
        
        Assert.IsType<ChargrilledMeat>(Assert.Single(hero.Belongings.Backpack.Items));
        Assert.True(View.Logged("mystery meat burns up!"));
        Assert.Contains((hero.Pos, 0xFF22EE66u, 6), View.Splashes);
    }

    [Fact]
    public void Burning_MeatWithFullPack_DropsTheStackAtTheHero()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        hero.Belongings.Backpack.Size = 1;
        var meat = new MysteryMeat();
        meat.Quantity(2);
        meat.Collect();
        var burning = Buff.Affect<Burning>(hero);
        burning.Reignite(hero);
        
        Tick(burning);

        var heap = Dungeon.Level.Heaps[hero.Pos];
        Assert.IsType<ChargrilledMeat>(heap.Peek());
        Assert.Equal(1, Assert.IsType<MysteryMeat>(Assert.Single(hero.Belongings.Backpack.Items)).Quantity());
    }

    [Fact]
    public void Burning_KillsItsTarget_StillSeedsFireAndDetachesCleanly()
    {
        LoadLevel(Room);
        var dummy =  PlaceDummy(C(2, 2), 1, 20); // grass
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);
        
        Tick(burning);
        
        Assert.False(dummy.IsAlive());
        Assert.Null(dummy.GetBuff<Burning>());
        Assert.Equal(4, Dungeon.Level.Blobs[typeof(Fire)].Cur[C(2, 2)]);
    }

    [Fact]
    public void Burning_OnFlammableCell_SeedsFireFour()
    {
        LoadLevel(Room);
        var  dummy = PlaceDummy(C(2, 2), 20, 20);
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);
        
        Tick(burning);

        var fire = Assert.IsType<Fire>(Dungeon.Level.Blobs[typeof(Fire)]);
        Assert.Equal(4, fire.Cur[C(2, 2)]);
        Assert.Contains(fire, View.AddedBlobs);
    }

    [Fact]
    public void Burning_KillsTheHero_RecordsTheBurningCause()
    {
        LoadLevel(Room);
        var hero =  PlaceBareHero(C(1));
        Dungeon.Depth = 3;
        hero.HP = 1;
        var burning = Buff.Affect<Burning>(hero);
        burning.Reignite(hero);
        
        Tick(burning);
        
        Assert.False(hero.IsAlive());
        Assert.Equal(TextUtils.Format(ResultDescriptions.Burning, Dungeon.Depth), Dungeon.ResultDescription);
        Assert.True(View.Logged("You burned to death..."));
    }

    [Fact]
    public void Burning_IconAndText()
    {
        var burning = new Burning();
        
        Assert.Equal(BuffIndicator.Fire, burning.Icon());
        Assert.Equal("Burning", burning.ToString());
    }

    [Fact]
    public void Burning_Bundle_RoundTrips_Left()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(2, 2), 20, 20);
        var burning = Buff.Affect<Burning>(dummy);
        burning.Reignite(dummy);
        var bundle = new Bundle();
        burning.StoreInBundle(bundle);

        var restored = new Burning();
        restored.RestoreFromBundle(bundle);
        var again = new Bundle();
        restored.StoreInBundle(again);
        
        Assert.Equal(8f, bundle.GetFloat("left"));
        Assert.Equal(8f, again.GetFloat("left"));
    }

    [Fact]
    public void Frost_Attach_ParalysesAndPutsOutBurning()
    {
        LoadLevel(Room);
        var  dummy = PlaceDummy(C(1), 20, 20);
        Buff.Affect<Burning>(dummy).Reignite(dummy);

        Buff.Prolong<Frost>(dummy, 5f);
        
        Assert.True(dummy.Paralysed);
        Assert.Null(dummy.GetBuff<Burning>());
        Assert.NotNull(dummy.GetBuff<Frost>());
    }

    [Fact]
    public void Frost_HeroWithMysteryMeat_FreezesItWithoutALog()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        new MysteryMeat().Collect();

        Buff.Prolong<Frost>(hero, 5f);
        
        Assert.IsType<FrozenCarpaccio>(Assert.Single(hero.Belongings.Backpack.Items));
        Assert.Empty(View.Logs);
    }

    [Fact]
    public void Frost_MeathWithFullPack_DropsTheCarpaccioAtTheHero()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        hero.Belongings.Backpack.Size = 1;
        var meat = new MysteryMeat();
        meat.Quantity(2);
        meat.Collect();
        
        Buff.Prolong<Frost>(hero, 5f);

        var heap = Dungeon.Level.Heaps[hero.Pos];
        Assert.IsType<FrozenCarpaccio>(heap.Peek());
        Assert.Equal(1, Assert.IsType<MysteryMeat>(Assert.Single(hero.Belongings.Backpack.Items)).Quantity());
    }

    [Fact]
    public void Frost_AnyDamage_Thaws()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        dummy.Immune.Add(typeof(string)); // hit is resited
        Buff.Prolong<Frost>(dummy, 5f);
        
        dummy.Damage(7, "a stone");
        
        Assert.Equal(20, dummy.HP);
        Assert.Null(dummy.GetBuff<Frost>());
        Assert.False(dummy.Paralysed);
    }

    [Fact]
    public void Frost_Detach_KeepsParalysedWhileParalysisRemains()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        Buff.Prolong<Paralysis>(dummy, 10f);
        Buff.Prolong<Frost>(dummy, 5f);
        
        Buff.Detach<Frost>(dummy);
        
        Assert.True(dummy.Paralysed);
        Buff.Detach<Paralysis>(dummy);
        Assert.False(dummy.Paralysed);
    }

    [Fact]
    public void Freezing_Affect_ProlongsFrostFiveToSevenAndAHalf()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        Random.Seed(9);
        var expected = 5f * Random.Float(1.0f, 1.5f);
        Random.Seed(9);

        Freezing.Affect(C(1), null);
        
        Assert.InRange(expected, 5f, 7.5f);
        Assert.Equal(expected, CooldownOf(dummy.GetBuff<Frost>()));
    }

    [Fact]
    public void Freezing_Affect_ClearsFireAndReportsVisibility()
    {
        LoadLevel(Room);
        var fire = Blob.Seed<Fire>(C(2), 3);
        Dungeon.Visible[C(2)] = true;
        Dungeon.Visible[C(4)] = false;

        var seen = Freezing.Affect(C(2), fire);
        var unseen = Freezing.Affect(C(4), fire);
        
        Assert.True(seen);
        Assert.False(unseen);
        Assert.Equal(0, fire.Cur[C(2)]);
        Assert.Equal(0, fire.Volume);
        Assert.Equal(new[] {(C(2), 0xFFFFFFFFu, 6)}, View.Splashes);
    }

    [Fact]
    public void CharAdd_BurningAndFrost_AddTheirSpriteStates()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        var view = new FakeCharView();
        dummy.Sprite = view;
        
        Buff.Affect<Burning>(dummy);
        Buff.Detach<Burning>(dummy);
        Buff.Prolong<Frost>(dummy, 5f);
        Buff.Detach<Frost>(dummy);
        
        Assert.Equal(
            new[]
            {
                (CharState.Burning, true),
                (CharState.Burning, false),
                (CharState.Frozen, true),
                (CharState.Frozen, false),
            }, view.States.ToArray());
    }

    [Fact]
    public void CharUpdateSpriteState_ReAddsBurningAndFrost()
    {
        LoadLevel(Room);
        var dummy = PlaceDummy(C(1), 20, 20);
        Buff.Affect<Burning>(dummy);
        Buff.Prolong<Frost>(dummy, 5f);
        Buff.Affect<Burning>(dummy);
        var view = new FakeCharView();
        dummy.Sprite = view;
        
        dummy.UpdateSpriteState();

        Assert.Contains((CharState.Burning, true), view.States);
        Assert.Contains((CharState.Frozen, true), view.States);
    }

    [Fact]
    public void HeroAdd_Burning_WarnsAndInterrupts()
    {
        LoadLevel(Room);
        var hero = PlaceBareHero(C(1));
        hero.CurAction = new HeroAction.Move(C(4));
        
        Buff.Affect<Burning>(hero);
        
        Assert.True(View.Logged("You catch fire!"));
        Assert.Null(hero.CurAction);
    }

    [Fact]
    public void Swarm_SplitWhileBurning_ClonesTheFire()
    {
        LoadLevel(
            "#####",
            "#<..#",
            "#...#",
            "#####"
        );
        PlaceHero(C(1));
        var swarm = new Swarm { Pos = C(2, 2), Sprite = new FakeCharView() };
        GameScene.Add(swarm);
        swarm.HP = 81;
        Buff.Affect<Burning>(swarm).Reignite(swarm);

        swarm.DefenseProc(Dungeon.Hero, 10);

        var clone = Dungeon.Level.Mobs.OfType<Swarm>().Single(m => m != swarm);
        Assert.NotNull(clone.GetBuff<Burning>());
    }

    [Fact]
    public void MirrorImage_IsImmuneToBurning()
    {
        Assert.Contains(typeof(Burning), new MirrorImage().Immunities());
    }
}