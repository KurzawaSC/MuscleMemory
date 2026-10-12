using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Localization;

public sealed class PluralsTests
{
    private const string SetCountKey = nameof(AppResources.SetCount_Other);
    private const string RepCountKey = nameof(AppResources.RepCount_Other);

    [Theory]
    [InlineData(1, "1 set")]
    [InlineData(0, "0 sets")]
    [InlineData(2, "2 sets")]
    [InlineData(21, "21 sets")]
    public void Format_English_PicksOneOrOtherKey(int count, string expected)
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal(expected, Plurals.Format(SetCountKey, count));
    }

    [Theory]
    [InlineData(1, "1 seria")]
    [InlineData(2, "2 serie")]
    [InlineData(4, "4 serie")]
    [InlineData(22, "22 serie")]
    [InlineData(0, "0 serii")]
    [InlineData(5, "5 serii")]
    [InlineData(12, "12 serii")]
    [InlineData(21, "21 serii")]
    public void Format_Polish_PicksKeyForEachCategory(int count, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(expected, Plurals.Format(SetCountKey, count));
    }

    [Theory]
    [InlineData(1, "1 powtórzenie")]
    [InlineData(3, "3 powtórzenia")]
    [InlineData(8, "8 powtórzeń")]
    public void Format_PolishReps_PicksKeyForEachCategory(int count, string expected)
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal(expected, Plurals.Format(RepCountKey, count));
    }

    [Fact]
    public void Format_CategoryMissingFromResources_FallsBackToOtherKey()
    {
        using var scope = new CultureScope(Cultures.Czech);

        Assert.Equal("3 sets", Plurals.Format(SetCountKey, 3));
    }

    [Fact]
    public void Format_CategoryPresentInFallbackResources_UsesThatCategory()
    {
        using var scope = new CultureScope(Cultures.Czech);

        Assert.Equal("1 set", Plurals.Format(SetCountKey, 1));
    }

    [Fact]
    public void Format_EnglishThousands_GroupsDigitsInCurrentCulture()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        Assert.Equal("1,000 sets", Plurals.Format(SetCountKey, 1000));
    }

    [Fact]
    public void Format_PolishThousands_GroupsDigitsInCurrentCulture()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal($"1{Cultures.PolishGroupSeparator}000 serii", Plurals.Format(SetCountKey, 1000));
    }

    [Fact]
    public void Format_CultureDiffersFromUICulture_FormatsNumberInCultureAndWordsInUICulture()
    {
        using var scope = new CultureScope(Cultures.German, Cultures.Polish);

        Assert.Equal("1.002 serie", Plurals.Format(SetCountKey, 1002));
    }

    [Theory]
    [InlineData(nameof(AppResources.SetCount_One))]
    [InlineData("SetCount")]
    [InlineData("SetCount_other")]
    public void Format_KeyNotEndingInOther_Throws(string key) =>
        Assert.Throws<ArgumentException>(() => Plurals.Format(key, 1));
}
