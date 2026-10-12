using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Localization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IDatabaseMaintenanceService _maintenanceService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogs;
    private readonly INavigationStackService _navigationStack;
    private readonly IErrorHandler _errors;
    private readonly ILocalizationService _localization;
    private readonly IDataChangeNotifier _dataChanges;
    private readonly TimeProvider _clock;

    public SettingsViewModel(
        IDatabaseMaintenanceService maintenanceService,
        IThemeService themeService,
        IDialogService dialogs,
        INavigationStackService navigationStack,
        IErrorHandler errors,
        ILocalizationService localization,
        IDataChangeNotifier dataChanges,
        TimeProvider clock,
        ActiveWorkoutViewModel activeWorkout)
    {
        _maintenanceService = maintenanceService;
        _themeService = themeService;
        _dialogs = dialogs;
        _navigationStack = navigationStack;
        _errors = errors;
        _localization = localization;
        _dataChanges = dataChanges;
        _clock = clock;
        ActiveWorkout = activeWorkout;
        SelectedTheme = themeService.SavedPreference;
        ShowLanguages();
        localization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThemeText))]
    public partial ThemePreference SelectedTheme { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LanguageText))]
    public partial LanguageOption SelectedLanguage { get; set; } = null!;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsThemeSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsLanguageSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsEraseSheetOpen { get; set; }

    public ObservableCollection<LanguageOption> Languages { get; } = [];

    public string ThemeText => SelectedTheme.ToDisplayName();

    public string LanguageText => SelectedLanguage.ShortLabel;

    public string VersionText => string.Format(AppResources.VersionFormat, AppInfo.Current.VersionString);

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    private bool IsAnySheetOpen => IsThemeSheetOpen || IsLanguageSheetOpen || IsEraseSheetOpen;

    partial void OnSelectedThemeChanged(ThemePreference value)
    {
        if (value != _themeService.SavedPreference)
        {
            _themeService.ChangeTheme(value);
        }
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (!value.Represents(_localization.SavedLanguage))
        {
            _errors.ReportFailures(SwitchLanguageAsync(value));
        }
    }

    private async Task SwitchLanguageAsync(LanguageOption option)
    {
        await SheetTransition.CloseAsync(CloseLanguageSheet, _clock);

        if (!await _navigationStack.ConfirmDiscardingAllChangesAsync())
        {
            SelectedLanguage = OptionFor(_localization.SavedLanguage);
            return;
        }

        _localization.ChangeLanguage(option.Language);
        _navigationStack.PopAllTabsToRoot();
        _dataChanges.Notify(DataArea.All);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        ShowLanguages();
        OnPropertyChanged(nameof(ThemeText));
        OnPropertyChanged(nameof(VersionText));
    }

    private void ShowLanguages()
    {
        Languages.ReplaceAll(
        [
            LanguageOption.SystemDefault(_localization.SystemLanguage),
            .. _localization.AvailableLanguages.Select(LanguageOption.For)
        ]);
        SelectedLanguage = OptionFor(_localization.SavedLanguage);
    }

    private LanguageOption OptionFor(CultureInfo? language) => Languages.First(option => option.Represents(language));

    [RelayCommand]
    private void OpenThemeSheet()
    {
        IsThemeSheetOpen = true;
    }

    [RelayCommand]
    private void OpenLanguageSheet()
    {
        IsLanguageSheetOpen = true;
    }

    private void CloseLanguageSheet()
    {
        IsLanguageSheetOpen = false;
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
        IsLanguageSheetOpen = false;
        IsEraseSheetOpen = false;
    }

    [RelayCommand]
    private void CloseEraseSheet()
    {
        IsEraseSheetOpen = false;
    }

    [RelayCommand]
    private Task EraseDataAsync() => _errors.RunAsync(async () =>
    {
        await SheetTransition.CloseAsync(CloseEraseSheet, _clock);

        await _maintenanceService.ClearAllDataAsync();
        _navigationStack.PopAllTabsToRoot();
        ActiveWorkout.Reset();
        await _maintenanceService.DeleteTemporaryFilesAsync();
        await _dialogs.ShowMessageAsync(AppResources.TitleSuccess, AppResources.BodyDataErased);
    });

    [RelayCommand]
    private Task ExportDataAsync() => _errors.RunAsync(async () =>
    {
        if (await _maintenanceService.CreateExportSnapshotAsync() is not { } snapshotPath)
        {
            await _dialogs.ShowMessageAsync(AppResources.TitleOops, AppResources.BodyNoDataToExport);
            return;
        }
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = AppResources.TitleExportData,
            File = new ShareFile(snapshotPath, DatabaseNames.ExportContentType)
        });
    }, AppResources.BodyExportFailed);

    [RelayCommand]
    private Task ImportDataAsync() => _errors.RunAsync(async () =>
    {
        if (ActiveWorkout.IsWorkoutActive)
        {
            await _dialogs.ShowMessageAsync(AppResources.TitleHoldOn, AppResources.BodyImportBlockedByWorkout);
            return;
        }

        if (await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppResources.TitlePickBackup }) is { } file)
        {
            await ImportFromAsync(file);
        }
    }, AppResources.BodyImportFailed);

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

            await _maintenanceService.ImportAsync();
        }
        finally
        {
            await _maintenanceService.DeleteImportCopiesAsync(file.FullPath);
        }

        _navigationStack.PopAllTabsToRoot();
        ActiveWorkout.Reset();
        await _dialogs.ShowMessageAsync(AppResources.TitleSuccess, DescribeCounts(AppResources.DataImportedFormat, counts));
    }

    private async Task<ImportCounts?> ConfirmImportAsync(FileResult file)
    {
        ImportPreview preview;
        await using (var source = await file.OpenReadAsync())
        {
            preview = await _maintenanceService.PrepareImportAsync(source);
        }

        if (preview.Status != ImportFileStatus.Valid)
        {
            await _dialogs.ShowMessageAsync(AppResources.TitleOops, RejectionMessage(preview.Status));
            return null;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            AppResources.TitleImportData,
            DescribeCounts(AppResources.ImportConfirmationFormat, preview.Counts),
            AppResources.ButtonImport,
            AppResources.ButtonCancel);
        return confirmed ? preview.Counts : null;
    }

    private static string RejectionMessage(ImportFileStatus status) => status switch
    {
        ImportFileStatus.Damaged => AppResources.BodyImportDamaged,
        ImportFileStatus.Incompatible => AppResources.BodyImportIncompatible,
        _ => AppResources.BodyImportNotBackup
    };

    private static string DescribeCounts(string format, ImportCounts counts) => string.Format(
        CultureInfo.CurrentCulture,
        format,
        Plurals.Format(nameof(AppResources.ExerciseCount_Other), counts.Exercises),
        Plurals.Format(nameof(AppResources.WorkoutCount_Other), counts.Workouts),
        Plurals.Format(nameof(AppResources.SessionCount_Other), counts.Sessions));
}
