using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Services;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel(
    IDatabaseMaintenanceService maintenanceService,
    IThemeService themeService,
    IDialogService dialogs,
    INavigationStackService navigationStack,
    IErrorHandler errors,
    ActiveWorkoutViewModel activeWorkout) : ObservableObject
{
    private readonly IDatabaseMaintenanceService _maintenanceService = maintenanceService;
    private readonly IThemeService _themeService = themeService;
    private readonly IDialogService _dialogs = dialogs;
    private readonly INavigationStackService _navigationStack = navigationStack;
    private readonly IErrorHandler _errors = errors;

    [ObservableProperty]
    public partial ThemePreference SelectedTheme { get; set; } = themeService.SavedPreference;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsThemeSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsEraseSheetOpen { get; set; }

    public string VersionText { get; } = string.Format(UiText.VersionFormat, AppInfo.Current.VersionString);

    public ActiveWorkoutViewModel ActiveWorkout { get; } = activeWorkout;

    private bool IsAnySheetOpen => IsThemeSheetOpen || IsEraseSheetOpen;

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
    private Task EraseDataAsync() => _errors.RunAsync(async () =>
    {
        IsEraseSheetOpen = false;
        await Task.Delay(UiTiming.SheetClose);

        await _maintenanceService.ClearAllDataAsync();
        _navigationStack.PopAllTabsToRoot();
        ActiveWorkout.Reset();
        await _dialogs.ShowMessageAsync(UiText.TitleSuccess, UiText.BodyDataErased);
    });

    [RelayCommand]
    private Task ExportDataAsync() => _errors.RunAsync(async () =>
    {
        if (await _maintenanceService.CreateExportSnapshotAsync() is not { } snapshotPath)
        {
            await _dialogs.ShowMessageAsync(UiText.TitleOops, UiText.BodyNoDataToExport);
            return;
        }
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export Muscle Memory Data",
            File = new ShareFile(snapshotPath)
        });
    }, UiText.BodyExportFailed);
}
