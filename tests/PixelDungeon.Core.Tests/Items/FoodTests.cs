using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Food;
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

    [Theory]
    [InlineData(typeof(Food), "ration of food", ItemSpriteSheet.Ration, 260f, 10)] // Hunger.Hungry
    [InlineData(typeof(Pasty), "pasty", ItemSpriteSheet.Pasty, 360, 20)] // Hunger.Starving
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
}