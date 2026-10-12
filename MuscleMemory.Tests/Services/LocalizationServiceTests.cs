using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Services;

[Collection(nameof(DefaultCultureCollection))]
public sealed class LocalizationServiceTests
{
    private const string UnavailableLanguage = "de";
    private const string UpperCasePolish = "PL";

    private readonly MemoryPreferences _preferences = new();

    [Fact]
    public void AvailableLanguages_Resources_ListsTheNeutralLanguageFirstThenEachTranslation() =>
        Assert.Equal([Cultures.English, Cultures.PolishLanguage], Create(Cultures.EnglishUs).AvailableLanguages.Select(language => language.Name));

    [Theory]
    [InlineData(Cultures.EnglishUk, Cultures.English)]
    [InlineData(Cultures.EnglishUs, Cultures.English)]
    [InlineData(Cultures.Polish, Cultures.PolishLanguage)]
    public void SystemLanguage_SystemCultureWithResources_IsThatLanguage(string systemCulture, string expected) =>
        Assert.Equal(expected, Create(systemCulture).SystemLanguage?.Name);

    [Fact]
    public void SystemLanguage_SystemCultureWithoutResources_IsNull() =>
        Assert.Null(Create(Cultures.German).SystemLanguage);

    [Fact]
    public void SavedLanguage_NothingStored_IsNull() =>
        Assert.Null(Create(Cultures.EnglishUs).SavedLanguage);

    [Theory]
    [InlineData(Cultures.PolishLanguage)]
    [InlineData(UpperCasePolish)]
    public void SavedLanguage_StoredAvailableLanguage_IsThatLanguage(string stored)
    {
        _preferences.Set(PreferenceKeys.AppLanguage, stored);

        Assert.Equal(Cultures.PolishLanguage, Create(Cultures.EnglishUs).SavedLanguage?.Name);
    }

    [Fact]
    public void SavedLanguage_StoredLanguageWithoutResources_IsNull()
    {
        _preferences.Set(PreferenceKeys.AppLanguage, UnavailableLanguage);

        Assert.Null(Create(Cultures.EnglishUs).SavedLanguage);
    }

    [Theory]
    [InlineData(Cultures.EnglishUk, Cultures.EnglishUk)]
    [InlineData(Cultures.Polish, Cultures.Polish)]
    [InlineData(Cultures.German, Cultures.EnglishUs)]
    public void RestoreSavedLanguage_NothingStored_DefaultsFromTheSystemCulture(string systemCulture, string expected)
    {
        using var defaults = new DefaultCultureScope();

        Create(systemCulture).RestoreSavedLanguage();

        AssertDefaultCultures(expected);
    }

    [Theory]
    [InlineData(Cultures.EnglishUk, Cultures.PolishLanguage, Cultures.Polish)]
    [InlineData(Cultures.Polish, Cultures.English, Cultures.EnglishUs)]
    [InlineData(Cultures.EnglishUk, Cultures.English, Cultures.EnglishUk)]
    public void RestoreSavedLanguage_StoredLanguage_AppliesItKeepingAMatchingSystemCulture(string systemCulture, string stored, string expected)
    {
        using var defaults = new DefaultCultureScope();
        _preferences.Set(PreferenceKeys.AppLanguage, stored);

        Create(systemCulture).RestoreSavedLanguage();

        AssertDefaultCultures(expected);
    }

    [Fact]
    public void ChangeLanguage_Language_StoresItAppliesItAndAnnounces()
    {
        using var defaults = new DefaultCultureScope();
        var localization = Create(Cultures.EnglishUk);
        var announcements = 0;
        localization.LanguageChanged += (_, _) => announcements++;

        localization.ChangeLanguage(CultureInfo.GetCultureInfo(Cultures.PolishLanguage));

        Assert.Equal(Cultures.PolishLanguage, _preferences.Get<string?>(PreferenceKeys.AppLanguage, null));
        Assert.Equal(Cultures.PolishLanguage, localization.SavedLanguage?.Name);
        Assert.Equal(1, announcements);
        AssertDefaultCultures(Cultures.Polish);
    }

    [Fact]
    public void ChangeLanguage_SystemDefault_ForgetsTheStoredLanguage()
    {
        using var defaults = new DefaultCultureScope();
        _preferences.Set(PreferenceKeys.AppLanguage, Cultures.PolishLanguage);
        var localization = Create(Cultures.EnglishUk);

        localization.ChangeLanguage(null);

        Assert.False(_preferences.ContainsKey(PreferenceKeys.AppLanguage));
        Assert.Null(localization.SavedLanguage);
        AssertDefaultCultures(Cultures.EnglishUk);
    }

    private LocalizationService Create(string systemCulture)
    {
        using var scope = new CultureScope(systemCulture);
        return new LocalizationService(_preferences);
    }

    private static void AssertDefaultCultures(string expected) =>
        Assert.Equal((expected, expected), (CultureInfo.DefaultThreadCurrentCulture?.Name, CultureInfo.DefaultThreadCurrentUICulture?.Name));
}
