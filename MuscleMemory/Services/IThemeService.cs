using MuscleMemory.Models;

namespace MuscleMemory.Services;

public interface IThemeService
{
    ThemePreference SavedPreference { get; }
    void RestoreSavedTheme();
    void ChangeTheme(ThemePreference preference);
}
