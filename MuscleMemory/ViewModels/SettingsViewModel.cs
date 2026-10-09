using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
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

    [RelayCommand]
    private Task ImportDataAsync() => errors.RunAsync(async () =>
    {
        if (ActiveWorkout.IsWorkoutActive)
        {
            await dialogs.ShowMessageAsync(UiText.TitleHoldOn, UiText.BodyImportBlockedByWorkout);
            return;
        }

        if (await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = UiText.TitlePickBackup }) is { } file)
        {
            await ImportFromAsync(file);
        }
    }, UiText.BodyImportFailed);

    private async Task ImportFromAsync(FileResult file)
    {
        ImportCounts? counts;
        try
        {
            counts = await ConfirmImportAsync(file);
            if (counts is null)
            {
                return;
            }

            await maintenanceService.ImportAsync();
        }
        finally
        {
            await maintenanceService.DeleteImportCopiesAsync(file.FullPath);
        }

        navigationStack.PopAllTabsToRoot();
        ActiveWorkout.Reset();
        await dialogs.ShowMessageAsync(UiText.TitleSuccess, DescribeCounts(UiText.DataImportedFormat, counts));
    }

    private async Task<ImportCounts?> ConfirmImportAsync(FileResult file)
    {
        ImportPreview preview;
        await using (var source = await file.OpenReadAsync())
        {
            preview = await maintenanceService.PrepareImportAsync(source);
        }

        if (preview.Status != ImportFileStatus.Valid)
        {
            await dialogs.ShowMessageAsync(UiText.TitleOops, RejectionMessage(preview.Status));
            return null;
        }

        var confirmed = await dialogs.ConfirmAsync(
            UiText.TitleImportData,
            DescribeCounts(UiText.ImportConfirmationFormat, preview.Counts),
            UiText.ButtonImport,
            UiText.ButtonCancel);
        return confirmed ? preview.Counts : null;
    }

    private static string RejectionMessage(ImportFileStatus status) => status switch
    {
        ImportFileStatus.Damaged => UiText.BodyImportDamaged,
        ImportFileStatus.Incompatible => UiText.BodyImportIncompatible,
        _ => UiText.BodyImportNotBackup
    };

    private static string DescribeCounts(string format, ImportCounts counts) => string.Format(
        CultureInfo.CurrentCulture,
        format,
        counts.Exercises,
        CountCaption.Exercises(counts.Exercises),
        counts.Workouts,
        CountCaption.Workouts(counts.Workouts),
        counts.Sessions,
        CountCaption.Sessions(counts.Sessions));
}
