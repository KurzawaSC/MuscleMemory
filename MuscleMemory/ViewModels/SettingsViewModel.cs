using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Services;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseMaintenanceService _maintenanceService;
    private readonly IThemeService _themeService;

    [ObservableProperty]
    public partial ThemePreference SelectedTheme { get; set; } = ThemePreference.System;

    public List<ThemePreference> ThemeOptions { get; } = [.. Enum.GetValues<ThemePreference>()];

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    public SettingsViewModel(IDatabaseMaintenanceService maintenanceService, IThemeService themeService, ActiveWorkoutViewModel activeWorkout)
    {
        _maintenanceService = maintenanceService;
        _themeService = themeService;
        ActiveWorkout = activeWorkout;
        SelectedTheme = themeService.SavedPreference;
    }

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        _themeService.ChangeTheme(value);
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
            await _maintenanceService.ClearAllDataAsync();
            ActiveWorkout.Reset();
            await Shell.Current.DisplayAlertAsync(UiText.TitleSuccess, UiText.BodyDataErased, UiText.ButtonOk);
        }
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            var dbPath = _maintenanceService.DatabaseFilePath;

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
