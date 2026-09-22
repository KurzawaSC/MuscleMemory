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
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    public partial ThemePreference SelectedTheme { get; set; } = ThemePreference.System;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsThemeSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsEraseSheetOpen { get; set; }

    public string VersionText { get; } = string.Format(UiText.VersionFormat, AppInfo.Current.VersionString);

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    private bool IsAnySheetOpen => IsThemeSheetOpen || IsEraseSheetOpen;

    public SettingsViewModel(IDatabaseMaintenanceService maintenanceService, IThemeService themeService, IDialogService dialogs, ActiveWorkoutViewModel activeWorkout)
    {
        _maintenanceService = maintenanceService;
        _themeService = themeService;
        _dialogs = dialogs;
        ActiveWorkout = activeWorkout;
        SelectedTheme = themeService.SavedPreference;
    }

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        _themeService.ChangeTheme(value);
    }

    [RelayCommand]
    private void OpenThemeSheet()
    {
        IsThemeSheetOpen = true;
    }

    [RelayCommand]
    private void OpenEraseSheet()
    {
        IsEraseSheetOpen = true;
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsThemeSheetOpen = false;
        IsEraseSheetOpen = false;
    }

    [RelayCommand]
    private async Task EraseDataAsync()
    {
        IsEraseSheetOpen = false;
        await Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));

        await _maintenanceService.ClearAllDataAsync();
        ActiveWorkout.Reset();
        await _dialogs.ShowMessageAsync(UiText.TitleSuccess, UiText.BodyDataErased);
    }

    [RelayCommand]
    private async Task ExportDataAsync()
    {
        try
        {
            var dbPath = _maintenanceService.DatabaseFilePath;

            if (!File.Exists(dbPath))
            {
                await _dialogs.ShowMessageAsync(UiText.TitleOops, UiText.BodyNoDataToExport);
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
            await _dialogs.ShowMessageAsync(UiText.TitleError, string.Format(UiText.ExportFailedFormat, ex.Message));
        }
    }
}
