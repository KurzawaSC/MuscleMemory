using MuscleMemory.Constants;
using MuscleMemory.Models;

namespace MuscleMemory.Services;

public sealed class ThemeService(IStatusBarService statusBarService) : IThemeService
{
    public ThemePreference SavedPreference =>
        Enum.Parse<ThemePreference>(Preferences.Default.Get(PreferenceKeys.AppTheme, nameof(ThemePreference.System)));

    public void RestoreSavedTheme()
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        Application.Current.UserAppTheme = ToAppTheme(SavedPreference);
        statusBarService.ApplyTheme();
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

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs args) =>
        statusBarService.ApplyTheme();

    private static AppTheme ToAppTheme(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => AppTheme.Light,
        ThemePreference.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };
}
