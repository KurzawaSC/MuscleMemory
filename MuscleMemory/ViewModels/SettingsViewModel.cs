using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial ThemePreference SelectedTheme { get; set; } = ThemePreference.System;

    public List<ThemePreference> ThemeOptions { get; } = Enum.GetValues<ThemePreference>().ToList();

    public SettingsViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        SelectedTheme = Enum.Parse<ThemePreference>(Preferences.Default.Get(PreferenceKeys.AppTheme, nameof(ThemePreference.System)));
    }

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        Preferences.Default.Set(PreferenceKeys.AppTheme, value.ToString());
        if (Application.Current != null)
        {
            var themeToSet = value switch
            {
                ThemePreference.Light => AppTheme.Light,
                ThemePreference.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            
            Application.Current.UserAppTheme = AppTheme.Unspecified;
            Application.Current.UserAppTheme = themeToSet;

            if (Shell.Current is AppShell shell)
            {
                shell.UpdateTabBarTheme(themeToSet);
            }
        }
    }
    [RelayCommand]
    private async Task EraseDataAsync()
    {
        bool isConfirmed = await Shell.Current.DisplayAlertAsync(
            "Warning!",
            "Are you sure you want to delete ALL your exercises and workouts? This action cannot be undone.",
            "Yes, erase it",
            "Cancel");

        if (isConfirmed)
        {
            await _dbContext.ClearAllDataAsync();
            await Shell.Current.DisplayAlertAsync("Success", "All your data has been erased.", "OK");
        }
    }
    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseNames.DatabaseFileName);

            if (!File.Exists(dbPath))
            {
                await Shell.Current.DisplayAlertAsync("Oops!", "There is no data to export yet.", "OK");
                return;
            }
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Muscle Memory Data",
                File = new ShareFile(dbPath)
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to export data: {ex.Message}", "OK");
        }
    }
}
