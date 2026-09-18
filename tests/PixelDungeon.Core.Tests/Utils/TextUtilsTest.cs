using PixelDungeon.Core.Utils;

namespace PixelDungeon.Core.Tests.Utils;

public class TextUtilsTest
{
    [Fact]
    public void Indefinite_PicksArticleByFirstLetter()
    {
        Assert.Equal("a rat", TextUtils.Indefinite("rat"));
        Assert.Equal("an eye", TextUtils.Indefinite("eye"));
        Assert.Equal("an Albino rat", TextUtils.Indefinite("Albino rat"));
        Assert.Equal("a", TextUtils.Indefinite(""));
    }

    [Fact]
    public void Format_UsesBracePlaceholders()
    {
        Assert.Equal("Killed by a rat on level 3", TextUtils.Format(ResultDescriptions.Mob, "a rat", 3));
    }

    [Fact]
    public void Capitalize_UppercasesFirstLetter()
    {
        Assert.Equal("Sewers", TextUtils.Capitalize("sewers"));
    }
}