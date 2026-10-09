using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel(
    IDatabaseMaintenanceService maintenanceService,
    IThemeService themeService,
    IDialogService dialogs,
    INavigationStackService navigationStack,
    IErrorHandler errors,
    ActiveWorkoutViewModel activeWorkout) : ObservableObject
{
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
        themeService.ChangeTheme(value);
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
    private void CloseEraseSheet()
    {
        IsEraseSheetOpen = false;
    }

    [RelayCommand]
    private Task EraseDataAsync() => errors.RunAsync(async () =>
    {
        await SheetTransition.CloseAsync(CloseEraseSheet);

        await maintenanceService.ClearAllDataAsync();
        navigationStack.PopAllTabsToRoot();
        ActiveWorkout.Reset();
        await maintenanceService.DeleteTemporaryFilesAsync();
        await dialogs.ShowMessageAsync(UiText.TitleSuccess, UiText.BodyDataErased);
    });

    [RelayCommand]
    private Task ExportDataAsync() => errors.RunAsync(async () =>
    {
        if (await maintenanceService.CreateExportSnapshotAsync() is not { } snapshotPath)
        {
            await dialogs.ShowMessageAsync(UiText.TitleOops, UiText.BodyNoDataToExport);
            return;
        }
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = UiText.TitleExportData,
            File = new ShareFile(snapshotPath, DatabaseNames.ExportContentType)
        });
    }, UiText.BodyExportFailed);
}
