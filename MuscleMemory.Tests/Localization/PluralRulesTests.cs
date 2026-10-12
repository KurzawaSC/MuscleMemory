using System.Globalization;
using MuscleMemory.Localization;

namespace MuscleMemory.Tests.Localization;

public sealed class PluralRulesTests
{
    [Theory]
    [InlineData(PluralCategory.One, 1)]
    [InlineData(PluralCategory.Other, 0, 2, 3, 5, 11, 16, 21, 100, 101, 1000, 1000000)]
    public void Select_English_ReturnsOneOrOther(PluralCategory expected, params int[] counts) =>
        AssertCategory("en", expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1)]
    [InlineData(PluralCategory.Few, 2, 3, 4, 22, 23, 24, 32, 33, 34, 42, 52, 62, 102, 1002)]
    [InlineData(PluralCategory.Many, 0, 5, 6, 11, 12, 13, 14, 15, 19, 21, 25, 100, 111, 112, 1000, 10000)]
    public void Select_Polish_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("pl", expected, counts);

    [Theory]
    [InlineData("cs")]
    [InlineData("sk")]
    public void Select_CzechAndSlovakOne_ReturnsOne(string language) =>
        AssertCategory(language, PluralCategory.One, 1);

    [Theory]
    [InlineData("cs")]
    [InlineData("sk")]
    public void Select_CzechAndSlovakTwoToFour_ReturnsFew(string language) =>
        AssertCategory(language, PluralCategory.Few, 2, 3, 4);

    [Theory]
    [InlineData("cs")]
    [InlineData("sk")]
    public void Select_CzechAndSlovakOthers_ReturnsOther(string language) =>
        AssertCategory(language, PluralCategory.Other, 0, 5, 11, 19, 21, 22, 100, 1000);

    [Theory]
    [InlineData(PluralCategory.One, 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001)]
    [InlineData(PluralCategory.Few, 2, 3, 9, 22, 29, 102, 1002)]
    [InlineData(PluralCategory.Other, 0, 10, 11, 12, 15, 19, 20, 30, 40, 100, 111, 1000, 10000)]
    public void Select_Lithuanian_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("lt", expected, counts);

    [Theory]
    [InlineData(PluralCategory.Zero, 0, 10, 11, 15, 19, 20, 30, 40, 100, 111, 1000, 10000)]
    [InlineData(PluralCategory.One, 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001)]
    [InlineData(PluralCategory.Other, 2, 3, 9, 22, 29, 102, 1002)]
    public void Select_Latvian_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("lv", expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1)]
    [InlineData(PluralCategory.Few, 0, 2, 3, 16, 19, 101, 102, 119, 1001)]
    [InlineData(PluralCategory.Other, 20, 21, 35, 100, 120, 1000)]
    public void Select_Romanian_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("ro", expected, counts);

    [Theory]
    [InlineData("ru", PluralCategory.One, new[] { 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001 })]
    [InlineData("ru", PluralCategory.Few, new[] { 2, 3, 4, 22, 23, 24, 32, 102, 1002 })]
    [InlineData("ru", PluralCategory.Many, new[] { 0, 5, 11, 12, 13, 14, 19, 20, 25, 100, 111, 112, 1000, 10000 })]
    [InlineData("uk", PluralCategory.One, new[] { 1, 21, 31, 41, 51, 61, 71, 81, 101, 1001 })]
    [InlineData("uk", PluralCategory.Few, new[] { 2, 3, 4, 22, 23, 24, 32, 102, 1002 })]
    [InlineData("uk", PluralCategory.Many, new[] { 0, 5, 11, 12, 13, 14, 19, 20, 25, 100, 111, 112, 1000, 10000 })]
    public void Select_RussianAndUkrainian_ReturnsCldrCategory(string language, PluralCategory expected, int[] counts) =>
        AssertCategory(language, expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1, 21, 31, 101, 1001)]
    [InlineData(PluralCategory.Few, 2, 3, 4, 22, 24, 102, 1002)]
    [InlineData(PluralCategory.Other, 0, 5, 11, 12, 14, 19, 20, 100, 111, 1000)]
    public void Select_Croatian_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("hr", expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1, 101, 201, 1001)]
    [InlineData(PluralCategory.Two, 2, 102, 202, 1002)]
    [InlineData(PluralCategory.Few, 3, 4, 103, 104, 1003)]
    [InlineData(PluralCategory.Other, 0, 5, 11, 21, 100, 1000)]
    public void Select_Slovenian_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("sl", expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1)]
    [InlineData(PluralCategory.Two, 2)]
    [InlineData(PluralCategory.Few, 3, 4, 5, 6)]
    [InlineData(PluralCategory.Many, 7, 8, 9, 10)]
    [InlineData(PluralCategory.Other, 0, 11, 25, 100, 1000)]
    public void Select_Irish_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("ga", expected, counts);

    [Theory]
    [InlineData(PluralCategory.One, 1)]
    [InlineData(PluralCategory.Two, 2)]
    [InlineData(PluralCategory.Few, 0, 3, 7, 10, 103, 109, 1003)]
    [InlineData(PluralCategory.Many, 11, 15, 19, 111, 117, 1011)]
    [InlineData(PluralCategory.Other, 20, 35, 100, 101, 102, 1000)]
    public void Select_Maltese_ReturnsCldrCategory(PluralCategory expected, params int[] counts) =>
        AssertCategory("mt", expected, counts);

    [Theory]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt-PT")]
    public void Select_OneOrMillionsLanguages_ReturnsCldrCategory(string language)
    {
        AssertCategory(language, PluralCategory.One, 1);
        AssertCategory(language, PluralCategory.Many, 1000000, 2000000);
        AssertCategory(language, PluralCategory.Other, 0, 2, 100, 1000, 1000001);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("pt")]
    [InlineData("pt-BR")]
    public void Select_ZeroOrOneOrMillionsLanguages_ReturnsCldrCategory(string language)
    {
        AssertCategory(language, PluralCategory.One, 0, 1);
        AssertCategory(language, PluralCategory.Many, 1000000, 2000000);
        AssertCategory(language, PluralCategory.Other, 2, 17, 100, 1000, 1000001);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("ja")]
    [InlineData("el")]
    public void Select_LanguageWithoutOwnRule_FallsBackToOneOrOther(string language)
    {
        AssertCategory(language, PluralCategory.One, 1);
        AssertCategory(language, PluralCategory.Other, 0, 2, 5, 21, 1000000);
    }

    [Fact]
    public void Select_InvariantCulture_FallsBackToOneOrOther()
    {
        Assert.Equal(PluralCategory.One, PluralRules.Select(1, CultureInfo.InvariantCulture));
        Assert.Equal(PluralCategory.Other, PluralRules.Select(2, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("pl-PL", 3, PluralCategory.Few)]
    [InlineData("lv-LV", 21, PluralCategory.One)]
    [InlineData("ga-IE", 2, PluralCategory.Two)]
    public void Select_SpecificCultureWithoutOwnRule_UsesLanguageRule(string culture, int count, PluralCategory expected) =>
        AssertCategory(culture, expected, count);

    [Fact]
    public void Select_PortugalAndBrazil_UseDifferentRulesForZero()
    {
        AssertCategory("pt-PT", PluralCategory.Other, 0);
        AssertCategory("pt-BR", PluralCategory.One, 0);
    }

    [Theory]
    [InlineData("en", -1, PluralCategory.One)]
    [InlineData("pl", -3, PluralCategory.Few)]
    [InlineData("pl", -5, PluralCategory.Many)]
    public void Select_NegativeCount_UsesAbsoluteValue(string language, int count, PluralCategory expected) =>
        AssertCategory(language, expected, count);

    private static void AssertCategory(string culture, PluralCategory expected, params int[] counts)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        Assert.All(counts, count => Assert.Equal(expected, PluralRules.Select(count, cultureInfo)));
    }
}
