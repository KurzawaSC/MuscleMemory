using MuscleMemory.Constants;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using NSubstitute;

namespace MuscleMemory.Tests.Services;

public sealed class ThemeServiceTests
{
    private readonly MemoryPreferences _preferences = new();
    private readonly IStatusBarService _statusBar = Substitute.For<IStatusBarService>();
    private readonly ThemeService _themes;

    public ThemeServiceTests() => _themes = new ThemeService(_statusBar, _preferences);

    [Fact]
    public void SavedPreference_NothingStored_FollowsTheSystem() =>
        Assert.Equal(ThemePreference.System, _themes.SavedPreference);

    [Theory]
    [InlineData(nameof(ThemePreference.System), ThemePreference.System)]
    [InlineData(nameof(ThemePreference.Light), ThemePreference.Light)]
    [InlineData(nameof(ThemePreference.Dark), ThemePreference.Dark)]
    public void SavedPreference_StoredName_ReturnsIt(string stored, ThemePreference expected)
    {
        _preferences.Set(PreferenceKeys.AppTheme, stored);

        Assert.Equal(expected, _themes.SavedPreference);
    }

    [Theory]
    [InlineData("Sepia")]
    [InlineData("dark")]
    [InlineData("7")]
    [InlineData("")]
    public void SavedPreference_UnknownValue_FollowsTheSystem(string stored)
    {
        _preferences.Set(PreferenceKeys.AppTheme, stored);

        Assert.Equal(ThemePreference.System, _themes.SavedPreference);
    }

    [Theory]
    [InlineData(ThemePreference.Light)]
    [InlineData(ThemePreference.Dark)]
    [InlineData(ThemePreference.System)]
    public void ChangeTheme_Preference_IsSavedForTheNextStart(ThemePreference preference)
    {
        _themes.ChangeTheme(preference);

        Assert.Equal(preference.ToString(), _preferences.Get<string?>(PreferenceKeys.AppTheme, null));
        Assert.Equal(preference, new ThemeService(_statusBar, _preferences).SavedPreference);
    }

    [Fact]
    public void RestoreSavedTheme_NoRunningApplication_LeavesTheStatusBarAlone()
    {
        _preferences.Set(PreferenceKeys.AppTheme, nameof(ThemePreference.Dark));

        _themes.RestoreSavedTheme();

        Assert.Empty(_statusBar.ReceivedCalls());
    }
}
