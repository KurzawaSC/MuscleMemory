using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial string SelectedTheme { get; set; } = "System";

    public List<string> ThemeOptions { get; } = new() { "System", "Light", "Dark" };

    public SettingsViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        SelectedTheme = Preferences.Default.Get("AppTheme", "System");
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Preferences.Default.Set("AppTheme", value);
        if (Application.Current != null)
        {
            var themeToSet = value switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
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
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MuscleMemory.db3");

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
