using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Food;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class FoodTests : DungeonFixture
{
    private SpyHero PlaceEater()
    {
        LoadLevel(
            "###",
            "#.#",
            "###"
        );
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        Buff.Affect<Hunger>(hero).Satisfy(-300); // 0 - (-300) = 300
        return hero;
    }

    private static int SeedRolling(int roll)
    {
        for (var seed = 1; seed <= 1000; seed++)
        {
            Random.Seed(seed);
            if (Random.Int(5) == roll)
            {
                return seed;
            }
        }

        throw new InvalidOperationException($"no seed up to 1000 draws Random.Int(5) == {roll}");
    }

    [Theory]
    [InlineData(typeof(Food), "ration of food", ItemSpriteSheet.Ration, 260f, 10)] // Hunger.Hungry
    [InlineData(typeof(Pasty), "pasty", ItemSpriteSheet.Pasty, 360f, 20)] // Hunger.Starving
    [InlineData(typeof(MysteryMeat), "mystery meat", ItemSpriteSheet.Meat, 100f, 5)] // Hunger.Starving
    public void Stats_MatchTheJava(Type type, string name, int image, float energy, int unitPrice)
    {
        var food = (Food)Activator.CreateInstance(type);

        Assert.NotNull(food);
        Assert.Equal(name, food.Name());
        Assert.Equal(image, food.Image());
        Assert.Equal(energy, food.Energy);
        Assert.True(food.Stackable);
        Assert.False(food.IsUpgradable());
        Assert.True(food.IsIdentified());
        Assert.Equal(unitPrice, food.Price());

        food.Quantity(3);
        Assert.Equal(unitPrice * 3, food.Price());
    }

    [Theory]
    [InlineData(typeof(Food), "Nothing fancy here: dried meat, some biscuits - things like that.",
        "That food tasted delicious!")]
    [InlineData(typeof(Pasty), "This is authentic Cornish pasty with traditional filling of beef and potato.",
        "That food tasted delicious!")]
    [InlineData(typeof(MysteryMeat), "Eat at your own risk!", "That food tasted... strange.")]
    public void InfoAndMessage_AreTheJavas(Type type, string info, string message)
    {
        var food = (Food)Activator.CreateInstance(type);

        Assert.NotNull(food);
        Assert.Equal(info, food.Info());
        Assert.Equal(message, food.Message);
    }

    [Fact]
    public void Actions_AddEat_AndTHereIsNoDefaultAction()
    {
        var ration = new Food();

        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow, Food.AcEat }, ration.Actions(Dungeon.Hero));
        Assert.Null(ration.DefaultAction);
    }

    [Fact]
    public void Eat_DetachesOne_SatisfiesByItsEnergy_AndLogsItsMessage()
    {
        var hero = PlaceEater();
        var rations = new Food();
        rations.Quantity(2);
        rations.Collect();

        rations.Execute(hero, Food.AcEat);

        Assert.Equal(40f, hero.GetBuff<Hunger>().HungerLevel); // 300 - Hunger.HUNGRY 260
        Assert.Contains(("That food tasted delicious!", LogKind.Info), View.Logs);
        Assert.Equal(1, rations.Quantity());
        Assert.Contains(rations, hero.Belongings.Backpack.Items);
    }

    [Theory]
    [InlineData(typeof(Food), 40f)] // 300 - 260
    [InlineData(typeof(Pasty), 0f)] // 300 - 360 - 60, clamped to 0
    [InlineData(typeof(MysteryMeat), 200f)]
    public void Eat_EachFood_SatisfiesByItsEnergy(Type type, float expectedLevel)
    {
        var hero = PlaceEater();
        var food = (Food)Activator.CreateInstance(type);

        Assert.NotNull(food);
        food.Collect();
        food.Execute(hero, Food.AcEat);

        Assert.Equal(expectedLevel, hero.GetBuff<Hunger>().HungerLevel);
    }

    [Fact]
    public void Eat_SpendsThreeTurns_AndWaitsForTheOperateAnimation()
    {
        var hero = PlaceEater();
        var view = (FakeCharView)hero.Sprite;
        var ration = new Food();
        ration.Collect();
        Actor.Process(); // the hero acts, becomes ready, and stays current
        Assert.True(hero.Ready);

        ration.Execute(hero, Food.AcEat);

        Assert.Equal(3f, hero.TimeLeft); // Food.TimeToEat
        Assert.False(hero.Ready);
        Assert.Equal(new[] { hero.Pos }, view.Operations);
        Assert.Contains((Assets.SndEat, 1f), View.Sounds);
        Assert.Equal(1, view.Bursts); // the food burst in place of SpellSprite.FOOD

        Actor.Process(); // the hero is still current: nothing acts while the animation plays
        Assert.False(hero.Ready);

        hero.OnOperateComplete(); // what the view calls when Operate finishes
        Actor.Process();
        Assert.True(hero.Ready);
    }

    [Fact]
    public void Eat_AsHurtWarrior_HealsFive_CappedAtHt()
    {
        var hero = PlaceEater();
        hero.HeroClass = HeroClass.Warrior;
        var view = (FakeCharView)hero.Sprite;
        var rations = new Food();
        rations.Quantity(3);
        rations.Collect();

        hero.HP = 12;
        rations.Execute(hero, Food.AcEat);
        Assert.Equal(17, hero.HP); // min(12 + 5, 20)
        Assert.Equal(2, view.Bursts); // the healing speck, then the food burst

        hero.HP = 18;
        rations.Execute(hero, Food.AcEat);
        Assert.Equal(20, hero.HP); // min(18 + 5, 20)
        Assert.Equal(4, view.Bursts);

        rations.Execute(hero, Food.AcEat);
        Assert.Equal(20, hero.HP); // HP < HT fails: no heal and no healing speck
        Assert.Equal(5, view.Bursts); // the food burst alone
    }

    [Theory]
    [InlineData(HeroClass.Mage)] // its wand recharge is deferred
    [InlineData(HeroClass.Rogue)]
    [InlineData(HeroClass.Huntress)]
    public void Eat_AsAnotherClass_HealsNothing(HeroClass heroClass)
    {
        var hero = PlaceEater();
        hero.HeroClass = heroClass;
        hero.HP = 12;
        var ration = new Food();
        ration.Collect();

        ration.Execute(hero, Food.AcEat);

        Assert.Equal(12, hero.HP);
        Assert.Equal(1, ((FakeCharView)hero.Sprite).Bursts); // the food burst alone
    }

    [Fact]
    public void Eat_CountsTheFoodEatenStatistic()
    {
        var hero = PlaceEater();
        var pasty = new Pasty();
        pasty.Quantity(2);
        pasty.Collect();

        pasty.Execute(hero, Food.AcEat);
        Assert.Equal(1, Statistics.FoodEaten);

        pasty.Execute(hero, Food.AcEat);
        Assert.Equal(2, Statistics.FoodEaten);
    }

    [Theory]
    [InlineData(0)] // TODO burning, the roll still draws
    [InlineData(4)] // the Java's switch has no case 4
    public void EatMysteryMeat_RollOfZeroOrFour_DrawsTheRollAndInflictsNothing(int roll)
    {
        var hero = PlaceEater();
        var meat = new MysteryMeat();
        meat.Collect();
        var seed = SeedRolling(roll);

        Random.Seed(seed);
        meat.Execute(hero, Food.AcEat);
        var next = Random.Int(1000);

        Assert.IsType<Hunger>(Assert.Single(hero.Buffs()));
        Assert.DoesNotContain(View.Logs, l => l.Kind == LogKind.Warning);
        Random.Seed(seed);
        Random.Int(5);
        Assert.Equal(Random.Int(1000), next); // eating drew exactly one number
    }

    [Fact]
    public void EatMysteryMeat_RollOfOne_RootsForParalysisDuration()
    {
        var hero = PlaceEater();
        var meat = new MysteryMeat();
        meat.Collect();

        Random.Seed(SeedRolling(1));
        meat.Execute(hero, Food.AcEat);

        Assert.True(hero.Rooted);
        Assert.Equal(10f, CooldownOf(hero.GetBuff<Roots>())); // Paralysis.Duration(hero)
        var legs = View.Logs.IndexOf(("You can't feel your legs!", LogKind.Warning));
        var move = View.Logs.IndexOf(("You can't move!", LogKind.Warning));
        Assert.InRange(legs, 0, move - 1); // the meat's warning, then Hero.add's
    }

    [Fact]
    public void EatMysteryMeat_RollOfTwo_PoisonsForAFifthOfHt()
    {
        var hero = PlaceEater();
        hero.HT = 23;
        var meat = new MysteryMeat();
        meat.Collect();

        Random.Seed(SeedRolling(2));
        meat.Execute(hero, Food.AcEat);

        Assert.Equal(4.6f, hero.GetBuff<Poison>().Left); // Poison.DurationFactor 1 * HT 23 / 5, divided in float
        Assert.Contains(("You are not feeling well.", LogKind.Warning), View.Logs);
        Assert.Contains(("You are poisoned!", LogKind.Warning), View.Logs);
    }

    [Fact]
    public void EatMysteryMeat_RollOfThree_SlowsOnlyAfterTheMealsOwnTurns()
    {
        var hero = PlaceEater();
        var meat = new MysteryMeat();
        meat.Collect();

        Random.Seed(SeedRolling(3));
        meat.Execute(hero, Food.AcEat);

        Assert.Equal(10f, CooldownOf(hero.GetBuff<Slow>())); // Slow.Duration(hero)
        Assert.Equal(3f, hero.TimeLeft); // TIME_TO_EAT was spent before the Slow took hold
        Assert.Contains(("You are stuffed.", LogKind.Warning), View.Logs);
    }

    [Fact]
    public void CrabDeath_DropsItsMysteryMeatOnTheLootRoll()
    {
        LoadLevel("#####", "#...#", "#####");
        PlaceHero(TestLevel.At(1, 1));
        var drops = 0;

        for (var seed = 1; seed <= 100; seed++)
        {
            Dungeon.Level.Heaps.Clear();
            var crab = new Crab { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
            GameScene.Add(crab);

            Random.Seed(seed);
            var rolled = Random.Float() < 0.167f; // Crab.java's lootChance; Mob.dropLoot's one draw
            Random.Seed(seed);
            crab.Die(Dungeon.Hero);

            // The hero stops earning a crab's exp past level 9, so it never passes Mob.die's
            // Lvl <= maxLvl + 2 = 11 gate, and every crab reaches dropLoot.
            var heap = Dungeon.Level.Heaps.GetValueOrDefault(TestLevel.At(3, 1));
            Assert.Equal(rolled, heap != null);
            if (rolled)
            {
                Assert.IsType<MysteryMeat>(heap.Peek());
                drops++;
            }
        }

        Assert.InRange(drops, 1, 99); // both outcomes occur: no drop in 100 has a chance of 0.833^100, about 1e-8
    }
}