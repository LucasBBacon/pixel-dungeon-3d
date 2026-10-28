using PixelDungeon.Core.Actors;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Actors.Hero;
using PixelDungeon.Core.Actors.Mobs;
using PixelDungeon.Core.Items.Weapon.Missiles;
using PixelDungeon.Core.Scenes;
using PixelDungeon.Core.Tests.Levels;
using PixelDungeon.Core.Tests.View;
using PixelDungeon.Core.Ui;
using PixelDungeon.Core.View;

namespace PixelDungeon.Core.Tests;

public class EquipmentIntegrationTests : DungeonFixture
{
    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########"
    ];

    private static readonly string[] Room =
    [
        "#####",
        "#...#",
        "#####"
    ];

    [Fact]
    public void ThrowingQuickSlotDarts_ThroughTheScheduler_KillsARat()
    {
        LoadLevel(Corridor);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        var rat = new Rat { Pos = TestLevel.At(5, 1), Sprite = new FakeCharView() };
        GameScene.Add(rat);
        // Rooted, the rat never closes to melee range
        // a visible mob's attack waits for its sprite's animation, which a fake view never finishes
        // It stays in the line of fire, four cells away
        Buff.Prolong<Roots>(rat, 1000f);

        new Dart(30).Identify().Collect();
        QuickSlot.PrimaryValue = typeof(Dart);
        var slainBefore = Statistics.EnemiesSlain;

        Actor.Process();

        var throws = 0;
        while (rat.IsAlive() && throws < 30)
        {
            Assert.True(hero.Ready, $"throw {throws}: the hero is not waiting for input");

            // QuickSlotView's first press, aim then run the slotted item's default action, which opens the throw prompt
            Assert.True(QuickSlot.UseTargeting(), $"throw {throws}: nothing to aim at");
            Assert.Same(rat, QuickSlot.LastTarget);
            var darts = hero.Belongings.GetItem(typeof(Dart));
            darts.Execute(hero);

            // The second press hands the target's cell to that prompt
            // the dart resolves in the missile callback
            View.Answer(QuickSlot.LastTarget.Pos);
            throws++;
            Assert.Null(hero.RangedWeapon);

            Actor.Process(); // the rat's turns, then the hero's, until it waits again
        }

        Assert.False(rat.IsAlive(), "30 darts did not kill the rat");
        Assert.DoesNotContain(rat, Dungeon.Level.Mobs);
        Assert.Equal(slainBefore + 1, Statistics.EnemiesSlain);
        Assert.Equal(1, hero.Exp);

        var packed = hero.Belongings.GetItem(typeof(Dart))?.Quantity() ?? 0;
        var landed = Dungeon.Level.Heaps.Values
            .SelectMany(heap => heap.Items)
            .OfType<Dart>()
            .Sum(dart => dart.Quantity());
        Assert.Equal(30 - throws, packed);
        Assert.True(throws - landed >= 2, $"throws {throws}, darts on the floor {landed}");

        Assert.Same(rat, HealthIndicator.Target);
        Assert.False(QuickSlot.UseTargeting());
    }

    [Fact]
    public void Resting_ThroughTheScheduler_GoesHungryAfter300Turns_AndStarvesAfter420()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        hero.Live(); // Regeneration and Hunger, as Dungeon.Init gives every game hero
        var hunger = hero.GetBuff<Hunger>();
        hero.Spend(0.5f);

        Actor.Process();

        var rests = 0;
        var hungryAt = -1;
        while (!hunger.IsStarving() && rests < 1000)
        {
            hero.Rest(false); // TimeToRest - 1
            Actor.Process();
            rests++;
            if (hungryAt < 0 && View.Logged("You are hungry."))
            {
                hungryAt = rests;
            }
        }

        // After r rests the hero waits at r + 0.5, and every hunger turn before that has run: floor((r + 0.5) / 12) + 1
        // turns of STEP 10. HUNGRY 260 is the 26th turn, at 12 * 25 = 300, which first runs at r = 300
        // STARVING 360 is the 36th, at 12 * 35 = 420, which first runs at r = 420
        Assert.Equal(300, hungryAt);
        Assert.Equal(420, rests);
        Assert.Equal(Hunger.Starving, hunger.HungerLevel);
        Assert.True(hero.IsStarving());
        Assert.Single(View.Logs, l => l.Text == "You are hungry.");
        Assert.Single(View.Logs, l => l.Text == "You are starving!");
        Assert.Equal(20, hero.HP); // the first starving damage roll is the 37th turn, at 432
    }

    [Fact]
    public void RestingWhileStarving_ThroughTheScheduler_DiesOfHunger_WithoutRegenerating()
    {
        LoadLevel(Room);
        var hero = PlaceHero(TestLevel.At(1, 1));
        hero.Sprite = new FakeCharView();
        Dungeon.Depth = 1;
        hero.Live();
        hero.GetBuff<Hunger>().Satisfy(-Hunger.Starving);
        hero.HP = 3;
        hero.Spend(0.5f); // no ties with the hunger turns at 0, 12, 24...

        Actor.Process();

        var rests = 0;
        var lowestHp = hero.HP;
        while (hero.IsAlive() && rests < 3000)
        {
            hero.Rest(false);
            Actor.Process();
            rests++;
            // Regeneration heals only while HP < HT and the hero is not starving.
            Assert.True(hero.HP <= lowestHp, $"rest {rests}: HP rose from {lowestHp} to {hero.HP} while starving");
            lowestHp = hero.HP;
        }

        // Each starving hunger turn, every 12 turns, deals 1 with chance 0.3. 3000 rests hold 250 such turns,
        // and fewer than three hits among them has a probability far below 1e-30.
        Assert.False(hero.IsAlive());
        Assert.Equal(1, View.GameOvers);
        Assert.Equal("Starved to death on level 1", Dungeon.ResultDescription);
        Assert.Contains(("You starved to death...", LogKind.Negative), View.Logs);
        // The starving branch logs "You are starving!" only with its 1 damage, so 3 HP took exactly three.
        Assert.Equal(3, View.Logs.Count(l => l.Text == "You are starving!"));
    }
}