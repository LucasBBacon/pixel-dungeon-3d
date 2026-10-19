using System.Reflection;
using PixelDungeon.Core.Actors.Buffs;
using PixelDungeon.Core.Ui;

namespace PixelDungeon.Core.Tests.Actors.Buffs;

public class BuffIconTests : DungeonFixture
{
    private static readonly (string Name, int Value)[] JavaConstants =
    [
        ("None", -1),
        ("MindVision", 0),
        ("Levitation", 1),
        ("Fire", 2),
        ("Poison", 3),
        ("Paralysis", 4),
        ("Hunger", 5),
        ("Starvation", 6),
        ("Slow", 7),
        ("Ooze", 8),
        ("Amok", 9),
        ("Terror", 10),
        ("Roots", 11),
        ("Invisible", 12),
        ("Shadows", 13),
        ("Weakness", 14),
        ("Frost", 15),
        ("Blindness", 16),
        ("Combo", 17),
        ("Fury", 18),
        ("Healing", 19),
        ("Armor", 20),
        ("Heart", 21),
        ("Light", 22),
        ("Cripple", 23),
        ("Barkskin", 24),
        ("Immunity", 25),
        ("Bleeding", 26),
        ("Mark", 27),
        ("Deferred", 28),
        ("Vertigo", 29),
        ("Rage", 30),
        ("Sacrifice", 31),
        ("Size", 7)
    ];

    [Fact]
    public void BuffIndicator_CarriesEveryJavaConstant_WithItsValue()
    {
        var ported = typeof(BuffIndicator)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (f.Name, Value: (int)f.GetRawConstantValue()))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(JavaConstants.OrderBy(c => c.Name, StringComparer.Ordinal).ToArray(), ported);
    }

    [Theory]
    [InlineData(typeof(Buff), -1)] // BuffIndicator.NONE, Buff.icon()'s default
    [InlineData(typeof(Sleep), -1)] // Sleep.java does not override icon()
    [InlineData(typeof(Regeneration), -1)] // nor does Regeneration.java
    [InlineData(typeof(Amok), 9)] // BuffIndicator.AMOK
    [InlineData(typeof(Terror), 10)] // BuffIndicator.TERROR
    [InlineData(typeof(Cripple), 23)] // BuffIndicator.CRIPPLE
    [InlineData(typeof(Bleeding), 26)] // BuffIndicator.BLEEDING
    public void Icon_ForEachPortedBuff_matchesTheJava(Type type, int icon)
    {
        Assert.Equal(icon, ((Buff)Activator.CreateInstance(type)).Icon());
    }
}