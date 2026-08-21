using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class ThemeService : IThemeService
{
    public ThemePreference SavedPreference =>
        Enum.Parse<ThemePreference>(Preferences.Default.Get(PreferenceKeys.AppTheme, nameof(ThemePreference.System)));

    public void RestoreSavedTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = ToAppTheme(SavedPreference);
    }

    public void ChangeTheme(ThemePreference preference)
    {
        Preferences.Default.Set(PreferenceKeys.AppTheme, preference.ToString());

        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = AppTheme.Unspecified;
        Application.Current.UserAppTheme = ToAppTheme(preference);
    }

    private static AppTheme ToAppTheme(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => AppTheme.Light,
        ThemePreference.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };
}
