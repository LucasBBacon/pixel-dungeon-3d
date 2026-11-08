using PixelDungeon.Core.Actors.Blobs;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items;
using PixelDungeon.Core.Items.Potions;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Sprites;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests.Items;

public class PotionTests : DungeonFixture
{
    private static readonly string[] Room =
    [
        "#######",
        "#.....#",
        "#######"
    ];

    private static readonly Type[] PotionClasses =
    [
        typeof(PotionOfHealing),
        typeof(PotionOfExperience),
        typeof(PotionOfToxicGas),
        typeof(PotionOfLiquidFlame),
        typeof(PotionOfStrength),
        typeof(PotionOfParalyticGas),
        typeof(PotionOfLevitation),
        typeof(PotionOfMindVision),
        typeof(PotionOfPurity),
        typeof(PotionOfInvisibility),
        typeof(PotionOfMight),
        typeof(PotionOfFrost),
    ];

    private static readonly string[] Colors =
    [
        "turquoise",
        "crimson",
        "azure",
        "jade",
        "golden",
        "magenta",
        "charcoal",
        "ivory",
        "amber",
        "bistre",
        "indigo",
        "silver",
    ];

    private static readonly int[] Images =
    [
        ItemSpriteSheet.PotionTurquoise,
        ItemSpriteSheet.PotionCrimson,
        ItemSpriteSheet.PotionAzure,
        ItemSpriteSheet.PotionJade,
        ItemSpriteSheet.PotionGolden,
        ItemSpriteSheet.PotionMagenta,
        ItemSpriteSheet.PotionCharcoal,
        ItemSpriteSheet.PotionIvory,
        ItemSpriteSheet.PotionAmber,
        ItemSpriteSheet.PotionBistre,
        ItemSpriteSheet.PotionIndigo,
        ItemSpriteSheet.PotionSilver,
    ];

    private Hero PlaceHeroInRoom()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        return hero;
    }

    private static T Carried<T>() where T : Potion, new()
    {
        var potion = new T();
        potion.Collect();
        return potion;
    }

    // ---- the table ----

    [Fact]
    public void InitColors_DrawsOneColourPerPotionInTheJavasOrder_WithTheImageAtTheSameIndex()
    {
        Random.Seed(9);
        Potion.InitColors();
        var nextDraw = Random.Int(1000);

        Random.Seed(9);
        var colorsLeft = new List<string>(Colors);
        var imagesLeft = new List<int>(Images);
        foreach (var type in PotionClasses)
        {
            var potion = (Potion)Activator.CreateInstance(type);
            var index = Random.Int(colorsLeft.Count);
            Assert.Equal(colorsLeft[index] + " potion", potion.Name());
            Assert.Equal(imagesLeft[index], potion.Image());
            colorsLeft.RemoveAt(index);
            imagesLeft.RemoveAt(index);
        }

        Assert.Equal(Random.Int(1000), nextDraw); // exactly twelve draws
    }

    [Fact]
    public void DungeonResetAndINit_EachBuildAFreshPotionTable()
    {
        new PotionOfHealing().SetKnown();
        Dungeon.Reset();
        Assert.False(new PotionOfHealing().IsKnown());

        new PotionOfHealing().SetKnown();
        Dungeon.Init();
        Assert.False(new PotionOfHealing().IsKnown());
    }

    [Fact]
    public void AllKnown_OnlyOnceEveryPotionIsKnown_AndKnownAndUnknownFollowTheTable()
    {
        foreach (var type in PotionClasses.Take(10))
        {
            ((Potion)Activator.CreateInstance(type)).SetKnown();
        }

        Assert.False(Potion.AllKnown());
        Assert.Equal(PotionClasses.Skip(10), Potion.GetUnknown());

        new PotionOfMight().SetKnown();

        Assert.True(Potion.AllKnown());
        Assert.Equal(PotionClasses, Potion.GetKnown());
    }

    // ---- the base ----

    [Fact]
    public void NameAndInfo_HideAnUnknownPotion_AndShowAKnownOne()
    {
        var potion = new PotionOfHealing();
        Assert.EndsWith(" potion", potion.Name());
        var color = potion.Name()[..(potion.Name().Length - " potion".Length)];
        Assert.Contains(color, Colors);
        Assert.Equal(
            "This flask contains a swirling " + color + " liquid. Who knows what it will do when drunk or thrown?",
            potion.Info());
        Assert.False(potion.IsIdentified());

        potion.SetKnown();

        Assert.Equal("Potion of Healing", potion.Name());
        Assert.Equal("An elixir that will instantly return you to full health and cure poison.", potion.Info());
        Assert.True(potion.IsIdentified());
        Assert.True(new PotionOfHealing().IsKnown());
        Assert.False(new PotionOfStrength().IsKnown());
    }

    [Fact]
    public void Identify_KnowsTheClass_WithoutTheItemIdentify()
    {
        var potion = new PotionOfExperience();

        Assert.Same(potion, potion.Identify());

        Assert.True(potion.IsKnown());
        Assert.False(potion.LevelKnown);
    }

    [Fact]
    public void Defaults_StackDrinkByDefault_AndCannotBeUpgraded()
    {
        var potion = new PotionOfLevitation();

        Assert.True(potion.Stackable);
        Assert.Equal(Potion.AcDrink, potion.DefaultAction);
        Assert.Equal(new[] { Item.AcDrop, Item.AcThrow, Potion.AcDrink }, potion.Actions(Dungeon.Hero));
        Assert.False(potion.IsUpgradable());
    }

    [Theory]
    [InlineData(typeof(PotionOfHealing), 30)]
    [InlineData(typeof(PotionOfExperience), 80)]
    [InlineData(typeof(PotionOfToxicGas), 40)]
    [InlineData(typeof(PotionOfLiquidFlame), 40)]
    [InlineData(typeof(PotionOfStrength), 100)]
    [InlineData(typeof(PotionOfParalyticGas), 40)]
    [InlineData(typeof(PotionOfLevitation), 35)]
    [InlineData(typeof(PotionOfMindVision), 35)]
    [InlineData(typeof(PotionOfPurity), 50)]
    [InlineData(typeof(PotionOfInvisibility), 40)]
    [InlineData(typeof(PotionOfMight), 200)]
    [InlineData(typeof(PotionOfFrost), 50)]
    public void Price_IsTwentyEachWhileUnknown_AndTheClassPriceOnceKnown(Type type, int knownPrice)
    {
        var potion = (Potion)Activator.CreateInstance(type);
        potion.Quantity(2);
        Assert.Equal(40, potion.Price());

        potion.SetKnown();

        Assert.Equal(knownPrice * 2, potion.Price());
    }

    [Theory]
    [InlineData(ItemSpriteSheet.PotionTurquoise, 0xFF40E0D0u)]
    [InlineData(ItemSpriteSheet.PotionCrimson, 0xFFDC143Cu)]
    [InlineData(ItemSpriteSheet.PotionAzure, 0xFF007FFFu)]
    [InlineData(ItemSpriteSheet.PotionJade, 0xFF00A86Bu)]
    [InlineData(ItemSpriteSheet.PotionGolden, 0xFFFFD700u)]
    [InlineData(ItemSpriteSheet.PotionMagenta, 0xFFFF00FFu)]
    [InlineData(ItemSpriteSheet.PotionCharcoal, 0xFF36454Fu)]
    [InlineData(ItemSpriteSheet.PotionIvory, 0xFFFFFFF0u)]
    [InlineData(ItemSpriteSheet.PotionAmber, 0xFFFFBF00u)]
    [InlineData(ItemSpriteSheet.PotionBistre, 0xFF3D2B1Fu)]
    [InlineData(ItemSpriteSheet.PotionIndigo, 0xFF4B0082u)]
    [InlineData(ItemSpriteSheet.PotionSilver, 0xFFC0C0C0u)]
    public void SplashColor_GivesEachPotionImageItsColour(int image, uint color)
    {
        Assert.Equal(color, Potion.SplashColor(image));
    }

    // ---- drinking ----

    [Fact]
    public void Drink_DetachesOneSpendsATurnAppliesItAndOperates()
    {
        LoadLevel(Room);
        Dungeon.Hero = new SpyHero();
        var hero = (SpyHero)PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        hero.HP = 5;
        var potions = new PotionOfHealing();
        potions.Quantity(2);
        potions.Collect();

        potions.Execute(hero);

        Assert.Equal(20, hero.HP);
        Assert.Equal(1, potions.Quantity());
        Assert.Equal(1f, hero.TimeLeft);
        Assert.False(hero.Ready);
        Assert.Equal(new[] { hero.Pos }, ((FakeCharView)hero.Sprite).Operations);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndDrink);
    }

    [Fact]
    public void DrinkAKnownHarmfulPotion_AsksFirst_AndOnlyYesDrinksIt()
    {
        var hero = PlaceHeroInRoom();
        var gas = Carried<PotionOfToxicGas>();
        gas.SetKnown();

        gas.Execute(hero, Potion.AcDrink);

        var request = Assert.Single(View.Windows);
        Assert.Equal("Harmful potion!", request.Title);
        Assert.Equal("Are you sure you want to drink it? In most cases you should throw such potions at your enemies.",
            request.Body);
        Assert.Equal(new[] { "Yes, I know what I'm doing", "No, I changed my mind" }, request.Options);
        Assert.False(Dungeon.Level.Blobs.ContainsKey(typeof(ToxicGas)));

        request.Select(1);
        Assert.False(Dungeon.Level.Blobs.ContainsKey(typeof(ToxicGas)));
        Assert.NotNull(hero.Belongings.GetItem(typeof(PotionOfToxicGas)));

        request.Select(0);
        Assert.Equal(1000, Dungeon.Level.Blobs[typeof(ToxicGas)].Cur[hero.Pos]);
        Assert.Null(hero.Belongings.GetItem(typeof(PotionOfToxicGas)));
    }

    [Theory]
    [InlineData(typeof(PotionOfLiquidFlame), true)]
    [InlineData(typeof(PotionOfToxicGas), true)]
    [InlineData(typeof(PotionOfParalyticGas), true)]
    [InlineData(typeof(PotionOfPurity), false)]
    [InlineData(typeof(PotionOfHealing), false)]
    public void DrinkAKnownPotion_AsksFirstOnlyForTheHarmfulOnes(Type type, bool asks)
    {
        var hero = PlaceHeroInRoom();
        var potion = (Potion)Activator.CreateInstance(type);
        Assert.NotNull(potion);
        potion.Collect();
        potion.SetKnown();

        potion.Execute(hero, Potion.AcDrink);

        Assert.Equal(asks ? 1 : 0, View.Windows.Count);
        Assert.Equal(asks, hero.Belongings.GetItem(type) != null); // a question leaves the potion in the pack
    }

    [Fact]
    public void DrinkAnUnknownHarmfulPotion_DrinksItAtOnce()
    {
        var hero = PlaceHeroInRoom();
        var gas = Carried<PotionOfParalyticGas>();

        gas.Execute(hero, Potion.AcDrink);

        Assert.Empty(View.Windows);
        Assert.Equal(1000, Dungeon.Level.Blobs[typeof(ParalyticGas)].Cur[hero.Pos]);
        Assert.True(gas.IsKnown()); // it shattered in sight, on the hero
    }

    // ---- throwing ----

    [Fact]
    public void ThrowAKnownBeneficialPotion_AsksFirst_AndYesOpensTheThrowPrompt()
    {
        var hero = PlaceHeroInRoom();
        var healing = Carried<PotionOfHealing>();
        healing.SetKnown();

        healing.Execute(hero, Item.AcThrow);

        var request = Assert.Single(View.Windows);
        Assert.Equal("Beneficial potion", request.Title);
        Assert.Equal("Are you sure you want to throw it? In most cases it makes sense to drink it.", request.Body);
        Assert.Equal(new[] { "Yes, I know what I'm doing", "No, I changed my mind" }, request.Options);
        Assert.Empty(View.Listeners);

        request.Select(0);

        Assert.Equal("Choose direction of throw", Assert.Single(View.Listeners).Prompt());
    }

    [Theory]
    [InlineData(typeof(PotionOfExperience), true)]
    [InlineData(typeof(PotionOfHealing), true)]
    [InlineData(typeof(PotionOfLevitation), true)]
    [InlineData(typeof(PotionOfMindVision), true)]
    [InlineData(typeof(PotionOfStrength), true)]
    [InlineData(typeof(PotionOfInvisibility), true)]
    [InlineData(typeof(PotionOfMight), true)]
    [InlineData(typeof(PotionOfToxicGas), false)]
    [InlineData(typeof(PotionOfParalyticGas), false)]
    [InlineData(typeof(PotionOfLiquidFlame), false)]
    [InlineData(typeof(PotionOfPurity), false)]
    [InlineData(typeof(PotionOfFrost), false)]
    public void ThrowAKnownPotion_AsksFirstOnlyFOrBeneficialOnes(Type type, bool asks)
    {
        var hero = PlaceHeroInRoom();
        var potion = (Potion)Activator.CreateInstance(type);
        Assert.NotNull(potion);
        potion.Collect();
        potion.SetKnown();

        potion.Execute(hero, Item.AcThrow);

        Assert.Equal(asks ? 1 : 0, View.Windows.Count);
        Assert.Equal(asks ? 0 : 1, View.Listeners.Count);
    }

    [Fact]
    public void ThrowAPotionIntoAWell_LandsAsAHeap()
    {
        LoadLevel(
            "#######",
            "#...W.#",
            "#######"
        );
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var potion = Carried<PotionOfHealing>();

        potion.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.IsType<PotionOfHealing>(Dungeon.Level.Heaps[TestLevel.At(4, 1)].Peek());
        Assert.Empty(View.Splashes);
    }

    [Fact]
    public void ThrowAPotionIntoAChasm_FallsToTheDepthBelow()
    {
        LoadLevel(
            "#######",
            "#...x.#",
            "#######"
        );
        Dungeon.Depth = 2;
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var potion = Carried<PotionOfHealing>();

        potion.Execute(hero, Item.AcThrow);
        View.Answer(TestLevel.At(4, 1));

        Assert.IsType<PotionOfHealing>(Assert.Single(Dungeon.DroppedItems[3]));
        Assert.Empty(View.Splashes);
    }

    [Fact]
    public void Shatter_OutOfSight_ShowsAndSaysNothing()
    {
        LoadLevel(
            "#######",
            "#.#...#",
            "#######");
        PlaceHero(TestLevel.At(1, 1));

        new PotionOfHealing().Shatter(TestLevel.At(4, 1));

        Assert.Empty(View.Logs);
        Assert.Empty(View.Splashes);
        Assert.Empty(View.Sounds);
    }

    // ---- each potion ----

    [Fact]
    public void Healing_Drunk_RestoresFullHpCuresPoisonCrippleAndBleeding_AndSaysSo()
    {
        var hero = PlaceHeroInRoom();
        hero.HP = 3;
        Buff.Affect<Poison>(hero).Set(5);
        Buff.Prolong<Cripple>(hero, 5f);
        Buff.Affect<Bleeding>(hero).Set(4);
        var potion = Carried<PotionOfHealing>();

        potion.Execute(hero);

        Assert.Equal(hero.HT, hero.HP);
        Assert.Null(hero.GetBuff<Poison>());
        Assert.Null(hero.GetBuff<Cripple>());
        Assert.Null(hero.GetBuff<Bleeding>());
        Assert.True(potion.IsKnown());
        Assert.Contains(("Your wounds heal completely.", LogKind.Positive), View.Logs);
        Assert.Contains((0xFF33FF33u, 4), ((FakeCharView)hero.Sprite).BurstLog);
    }

    [Fact]
    public void Experience_Drunk_FillsTheExperienceBarToTheNextLevel()
    {
        var hero = PlaceHeroInRoom();
        hero.Exp = 3;
        var potion = Carried<PotionOfExperience>();

        potion.Execute(hero);

        // earnExp(maxExp() - exp) = 10 - 3 = 7: exp reaches 10 -> level-1 threshold, hero to level 2 with 0.
        Assert.Equal(2, hero.Lvl);
        Assert.Equal(0, hero.Exp);
        Assert.True(potion.IsKnown());
    }

    [Fact]
    public void Strength_Drunk_AddsOneStrength_WithItsStatusAndMessage()
    {
        var hero = PlaceHeroInRoom();
        var potion = Carried<PotionOfStrength>();

        potion.Execute(hero);

        Assert.Equal(11, hero.STR);
        Assert.Contains((StatusColor.Positive, "+1 str"), ((FakeCharView)hero.Sprite).Statuses);
        Assert.Contains(("Newfound strength surges through your body.", LogKind.Positive), View.Logs);
        Assert.True(potion.IsKnown());
    }

    [Fact]
    public void Might_Drunk_AddsOneStrengthAndFiveHealth_AndIsLearntApartFromStrength()
    {
        var hero = PlaceHeroInRoom();
        hero.HP = 10;
        var potion = Carried<PotionOfMight>();

        potion.Execute(hero);

        Assert.Equal(11, hero.STR);
        Assert.Equal(25, hero.HT);
        Assert.Equal(15, hero.HP);
        Assert.Contains((StatusColor.Positive, "+1 str, +5 ht"), ((FakeCharView)hero.Sprite).Statuses);
        Assert.Contains(("Newfound strength surges through your body.", LogKind.Positive), View.Logs);
        Assert.Equal("Potion of Might", potion.Name());
        Assert.False(new PotionOfStrength().IsKnown());
    }

    [Fact]
    public void Levitation_Drunk_LiftsTheHero()
    {
        var hero = PlaceHeroInRoom();
        var potion = Carried<PotionOfLevitation>();

        potion.Execute(hero);

        Assert.True(hero.Flying);
        Assert.Equal(Levitation.Duration, CooldownOf(hero.GetBuff<Levitation>()));
        Assert.Contains(("You float into the air!", LogKind.Info), View.Logs);
        Assert.True(potion.IsKnown());
    }

    [Fact]
    public void MindVision_Drunk_SensesMinds_OrSaysTheHeroIsAlone()
    {
        var hero = PlaceHeroInRoom();
        var potions = new PotionOfMindVision();
        potions.Quantity(2);
        potions.Collect();

        potions.Execute(hero);

        Assert.NotNull(hero.GetBuff<MindVision>());
        Assert.Contains(("You can somehow tell that you are alone on this level at the moment.", LogKind.Info),
            View.Logs);
        Assert.True(potions.IsKnown());

        GameScene.Add(new Rat { Pos = TestLevel.At(5, 1), Sprite = new FakeCharView() });
        potions.Execute(hero);

        Assert.Contains(("You can somehow feel the presence of other creatures' minds!", LogKind.Info), View.Logs);
    }

    [Fact]
    public void Invisibility_Drunk_TurnsTheHeroInvisible()
    {
        var hero = PlaceHeroInRoom();
        var potion = Carried<PotionOfInvisibility>();

        potion.Execute(hero);

        Assert.Equal(1, hero.Invisible);
        Assert.Equal(Invisibility.Duration, CooldownOf(hero.GetBuff<Invisibility>()));
        Assert.Contains(("You see your hands turn invisible!", LogKind.Info), View.Logs);
        Assert.Contains(View.Sounds, s => s.Id == Assets.SndMeld);
        Assert.True(potion.IsKnown());
    }

    // ---- where potions come from and land ----

    [Fact]
    public void SwarmDeath_DropsAPotionOfHealingOnItsLootRoll()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        PlaceHero(TestLevel.At(1, 1));
        var drops = 0;

        for (var seed = 1; seed <= 100; seed++)
        {
            Dungeon.Level.Heaps.Clear();
            var swarm = new Swarm { Pos = TestLevel.At(3, 1), Sprite = new FakeCharView() };
            GameScene.Add(swarm);

            Random.Seed(seed);
            var rolled = Random.Int(5) == 0; // Swarm.dropLoot at generation 0: Random.Int(5 * (0 + 1)) == 0
            Random.Seed(seed);
            swarm.Die(Dungeon.Hero);

            var heap = Dungeon.Level.Heaps.GetValueOrDefault(TestLevel.At(3, 1));
            Assert.Equal(rolled, heap != null);

            if (!rolled) continue;

            Assert.IsType<PotionOfHealing>(heap.Peek());
            Assert.Equal(1, ((FakeHeapView)heap.Sprite).Drops); // .sprite.drop()
            drops++;
        }

        Assert.InRange(drops, 1, 99); // no drop in 100 has a chance of 0.8^100, about 2e-10
    }

    [Fact]
    public void LandDroppedItems_APotionShattersInsteadOfLanding()
    {
        LoadLevel(
            "#####",
            "#...#",
            "#####"
        );
        Dungeon.Depth = 4;
        Dungeon.DroppedItems[4] = [new PotionOfHealing(), new TestItem()];

        GameScene.LandDroppedItems();

        var landed = Dungeon.Level.Heaps.Values.SelectMany(h => h.Items).ToList();
        Assert.IsType<TestItem>(Assert.Single(landed));
        Assert.False(Dungeon.DroppedItems.ContainsKey(4));
    }
}