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

    public List<ThemePreference> ThemeOptions { get; } = [.. Enum.GetValues<ThemePreference>()];

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    public SettingsViewModel(DatabaseContext dbContext, ActiveWorkoutViewModel activeWorkout)
    {
        _dbContext = dbContext;
        ActiveWorkout = activeWorkout;
        SelectedTheme = Enum.Parse<ThemePreference>(Preferences.Default.Get(PreferenceKeys.AppTheme, nameof(ThemePreference.System)));
    }

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        Preferences.Default.Set(PreferenceKeys.AppTheme, value.ToString());

        if (Application.Current is null)
        {
            return;
        }

        Application.Current.UserAppTheme = AppTheme.Unspecified;
        Application.Current.UserAppTheme = value switch
        {
            ThemePreference.Light => AppTheme.Light,
            ThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
    [RelayCommand]
    private async Task EraseDataAsync()
    {
        bool isConfirmed = await Shell.Current.DisplayAlertAsync(
            UiText.TitleWarning,
            UiText.BodyEraseAllDataConfirmation,
            UiText.ButtonYesEraseIt,
            UiText.ButtonCancel);

        if (isConfirmed)
        {
            await _dbContext.ClearAllDataAsync();
            await Shell.Current.DisplayAlertAsync(UiText.TitleSuccess, UiText.BodyDataErased, UiText.ButtonOk);
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
                await Shell.Current.DisplayAlertAsync(UiText.TitleOops, UiText.BodyNoDataToExport, UiText.ButtonOk);
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
            await Shell.Current.DisplayAlertAsync(UiText.TitleError, string.Format(UiText.ExportFailedFormat, ex.Message), UiText.ButtonOk);
        }
    }
}
