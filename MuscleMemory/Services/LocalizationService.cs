using System.Globalization;
using System.Reflection;
using System.Resources;
using MuscleMemory.Constants;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;

namespace MuscleMemory.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string SatelliteLanguagesKey = "SatelliteLanguages";
    private const char LanguageSeparator = ';';
    private const string NoNeutralLanguageMessage = "The assembly declares no neutral resources language.";

    private readonly CultureInfo _systemCulture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _systemUiCulture = CultureInfo.CurrentUICulture;
    private readonly Lazy<IReadOnlyList<CultureInfo>> _languages = new(DiscoverLanguages);

    public event EventHandler? LanguageChanged;

    public IReadOnlyList<CultureInfo> AvailableLanguages => _languages.Value;

    public CultureInfo? SystemLanguage => FindLanguageOf(_systemUiCulture);

    public CultureInfo? SavedLanguage =>
        Preferences.Default.Get<string?>(PreferenceKeys.AppLanguage, null) is { } name
            ? AvailableLanguages.FirstOrDefault(language => IsSameName(language, name))
            : null;

    public void RestoreSavedLanguage() => Apply(SavedLanguage);

    public void ChangeLanguage(CultureInfo? language)
    {
        if (language is null)
        {
            Preferences.Default.Remove(PreferenceKeys.AppLanguage);
        }
        else
        {
            Preferences.Default.Set(PreferenceKeys.AppLanguage, language.Name);
        }

        Apply(language);
        LocalizedText.Instance.Refresh();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Apply(CultureInfo? chosen)
    {
        var language = chosen ?? SystemLanguage ?? AvailableLanguages[0];
        var uiCulture = Localize(_systemUiCulture, language);
        var culture = Localize(_systemCulture, language);

        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }

    private CultureInfo? FindLanguageOf(CultureInfo culture) =>
        SelfAndParents(culture)
            .Select(candidate => AvailableLanguages.FirstOrDefault(language => IsSameName(language, candidate.Name)))
            .FirstOrDefault(language => language is not null);

    private static CultureInfo Localize(CultureInfo systemCulture, CultureInfo language) =>
        SelfAndParents(systemCulture).Any(candidate => IsSameName(language, candidate.Name))
            ? systemCulture
            : CultureInfo.CreateSpecificCulture(language.Name);

    private static IEnumerable<CultureInfo> SelfAndParents(CultureInfo culture)
    {
        for (var current = culture; !current.Equals(CultureInfo.InvariantCulture); current = current.Parent)
        {
            yield return current;
        }
    }

    private static bool IsSameName(CultureInfo language, string name) =>
        string.Equals(language.Name, name, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<CultureInfo> DiscoverLanguages()
    {
        var assembly = typeof(AppResources).Assembly;
        var neutralName = assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>()?.CultureName
            ?? throw new InvalidOperationException(NoNeutralLanguageMessage);

        return
        [
            CultureInfo.GetCultureInfo(neutralName),
            .. assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(attribute => attribute.Key == SatelliteLanguagesKey)
                .SelectMany(attribute => (attribute.Value ?? string.Empty).Split(LanguageSeparator, StringSplitOptions.RemoveEmptyEntries))
                .Select(CultureInfo.GetCultureInfo)
                .Where(HasResources)
        ];
    }

    private static bool HasResources(CultureInfo culture) =>
        AppResources.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false) is not null;
}
