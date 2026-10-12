using System.Globalization;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.ViewModels;

public sealed class LanguageOptionTests
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo(Cultures.PolishLanguage);
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo(Cultures.English);

    [Fact]
    public void For_Language_UsesCapitalizedNativeName()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var option = LanguageOption.For(Polish);

        Assert.Same(Polish, option.Language);
        Assert.Equal("Polski", option.Label);
        Assert.Equal("Polski", option.ShortLabel);
    }

    [Fact]
    public void For_LanguageInAnotherUILanguage_KeepsNativeName()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("English", LanguageOption.For(English).Label);
    }

    [Fact]
    public void SystemDefault_UnknownSystemLanguage_ShowsPlainLabel()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var option = LanguageOption.SystemDefault(null);

        Assert.Null(option.Language);
        Assert.Equal("System default", option.Label);
        Assert.Equal("System default", option.ShortLabel);
    }

    [Fact]
    public void SystemDefault_KnownSystemLanguage_NamesItInLabelOnly()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);

        var option = LanguageOption.SystemDefault(Polish);

        Assert.Null(option.Language);
        Assert.Equal("System default (Polski)", option.Label);
        Assert.Equal("System default", option.ShortLabel);
    }

    [Fact]
    public void SystemDefault_Polish_TranslatesLabel()
    {
        using var scope = new CultureScope(Cultures.Polish);

        Assert.Equal("Język systemu (English)", LanguageOption.SystemDefault(English).Label);
    }

    [Fact]
    public void Represents_SameLanguage_ReturnsTrue() =>
        Assert.True(LanguageOption.For(Polish).Represents(new CultureInfo(Cultures.PolishLanguage)));

    [Fact]
    public void Represents_OtherLanguage_ReturnsFalse() =>
        Assert.False(LanguageOption.For(Polish).Represents(English));

    [Fact]
    public void Represents_SystemDefaultAndNull_ReturnsTrue() =>
        Assert.True(LanguageOption.SystemDefault(Polish).Represents(null));

    [Fact]
    public void Represents_LanguageAndNull_ReturnsFalse() =>
        Assert.False(LanguageOption.For(Polish).Represents(null));
}
