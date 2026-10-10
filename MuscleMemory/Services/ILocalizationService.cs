using System.Globalization;

namespace MuscleMemory.Services;

public interface ILocalizationService
{
    event EventHandler? LanguageChanged;

    IReadOnlyList<CultureInfo> AvailableLanguages { get; }

    CultureInfo? SystemLanguage { get; }

    CultureInfo? SavedLanguage { get; }

    void RestoreSavedLanguage();

    void ChangeLanguage(CultureInfo? language);
}
