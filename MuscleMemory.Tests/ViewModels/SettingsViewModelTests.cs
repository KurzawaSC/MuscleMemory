using System.Globalization;
using MuscleMemory.Constants;
using MuscleMemory.Localization;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MuscleMemory.Tests.ViewModels;

[Collection(nameof(DefaultCultureCollection))]
public sealed class SettingsViewModelTests : IAsyncLifetime
{
    private const string PickerFolderName = "picker-copy";
    private const string BackupFileName = "backup.db3";
    private const string ImportConfirmation =
        "This replaces all current data with 3 exercises, 2 workouts and 1 session from the file. It can't be undone.";

    private readonly TestViewModels _ui = new();
    private SettingsViewModel _settings = null!;

    public ValueTask InitializeAsync()
    {
        _settings = _ui.CreateSettings();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _ui.DisposeAsync();

    [Fact]
    public void New_SavedThemeAndNoLanguage_ShowsThemAndTheVersion()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _ui.Preferences.Set(PreferenceKeys.AppTheme, nameof(ThemePreference.Dark));

        var settings = _ui.CreateSettings();

        Assert.Equal((ThemePreference.Dark, "Dark"), (settings.SelectedTheme, settings.ThemeText));
        Assert.Equal(["System default (English)", "English", "Polski"], settings.Languages.Select(option => option.Label));
        Assert.Equal((null, "System default"), (settings.SelectedLanguage.Language, settings.LanguageText));
        Assert.Equal($"Version {TestViewModels.AppVersion}", settings.VersionText);
    }

    [Fact]
    public void SelectedTheme_Changed_StoresTheChoice()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        var changed = PropertyChanges.Record(_settings);

        _settings.SelectedTheme = ThemePreference.Light;

        Assert.Equal(ThemePreference.Light, _ui.Theme.SavedPreference);
        Assert.Equal("Light", _settings.ThemeText);
        Assert.Contains(nameof(SettingsViewModel.ThemeText), changed);
    }

    [Fact(Timeout = 10_000)]
    public async Task SelectedLanguage_Polish_SwitchesEveryTextAndReturnsTabsToRoot()
    {
        using var defaults = new DefaultCultureScope();
        _ui.Localization.RestoreSavedLanguage();
        var settings = _ui.CreateSettings();
        var refreshed = PropertyChanges.Record(LocalizedText.Instance);
        var notified = new List<DataArea>();
        _ui.App.DataChanges.Changed += (_, areas) => notified.Add(areas);
        _ui.Clock.HoldPauses();

        settings.OpenLanguageSheetCommand.Execute(null);
        settings.SelectedLanguage = settings.Languages.Single(option => option.Language?.Name == Cultures.PolishLanguage);
        await _ui.Clock.PauseStarted.WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(settings.IsLanguageSheetOpen);
        Assert.Equal(CultureInfo.GetCultureInfo(Cultures.EnglishUs), CultureInfo.DefaultThreadCurrentCulture);
        _ui.Clock.ReleasePauses();
        await _ui.SettleAsync().WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Cultures.PolishLanguage, _ui.Localization.SavedLanguage?.Name);
        Assert.Equal(("Polski", "Systemowy", $"Wersja {TestViewModels.AppVersion}"), (settings.LanguageText, settings.ThemeText, settings.VersionText));
        Assert.Equal("Język systemu (English)", settings.Languages[0].Label);
        Assert.Equal("Ustawienia", LocalizedText.Instance[nameof(AppResources.HeaderSettings)]);
        Assert.Contains(null, refreshed);
        _ui.NavigationStack.Received(1).PopAllTabsToRoot();
        Assert.Equal([DataArea.All], notified);
    }

    [Fact]
    public async Task SelectedLanguage_UnsavedChangesKept_StaysOnTheSavedLanguage()
    {
        using var defaults = new DefaultCultureScope();
        _ui.Localization.RestoreSavedLanguage();
        var settings = _ui.CreateSettings();
        _ui.NavigationStack.ConfirmDiscardingAllChangesAsync().Returns(false);

        settings.SelectedLanguage = settings.Languages.Single(option => option.Language?.Name == Cultures.PolishLanguage);
        await _ui.SettleAsync();

        Assert.Null(settings.SelectedLanguage.Language);
        Assert.Null(_ui.Localization.SavedLanguage);
        Assert.Equal(CultureInfo.GetCultureInfo(Cultures.EnglishUs), CultureInfo.DefaultThreadCurrentCulture);
        _ui.NavigationStack.DidNotReceive().PopAllTabsToRoot();
    }

    [Fact]
    public async Task SelectedLanguage_BackToSystemDefault_ForgetsTheChoice()
    {
        using var defaults = new DefaultCultureScope();
        _ui.Preferences.Set(PreferenceKeys.AppLanguage, Cultures.PolishLanguage);
        _ui.Localization.RestoreSavedLanguage();
        var settings = _ui.CreateSettings();

        settings.SelectedLanguage = settings.Languages[0];
        await _ui.SettleAsync();

        Assert.False(_ui.Preferences.ContainsKey(PreferenceKeys.AppLanguage));
        Assert.Equal(("System default", "Version 1.4.2"), (settings.LanguageText, settings.VersionText));
    }

    [Fact]
    public void CloseSheets_OnlyWhileASheetIsOpen_CanExecute()
    {
        Assert.False(_settings.CloseSheetsCommand.CanExecute(null));

        _settings.OpenThemeSheetCommand.Execute(null);
        Assert.True(_settings.CloseSheetsCommand.CanExecute(null));
        _settings.CloseSheetsCommand.Execute(null);

        Assert.False(_settings.IsThemeSheetOpen);
        Assert.False(_settings.CloseSheetsCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportData_NoDatabaseYet_SaysThereIsNothingToExport()
    {
        await _settings.ExportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleOops, AppResources.BodyNoDataToExport);
        await _ui.Share.DidNotReceiveWithAnyArgs().RequestAsync(default(ShareFileRequest)!);
    }

    [Fact]
    public async Task ExportData_Data_SharesASnapshotOfTheDatabase()
    {
        await SeedAsync(_ui.App);

        await _settings.ExportDataCommand.ExecuteAsync(null);

        var snapshot = Path.Combine(_ui.App.Database.CacheFolder, DatabaseNames.ExportFileName);
        await _ui.Share.Received(1).RequestAsync(Arg.Is<ShareFileRequest>(request =>
            request.Title == AppResources.TitleExportData && request.File!.FullPath == snapshot && request.File.ContentType == DatabaseNames.ExportContentType));
        Assert.Equal(DatabaseRows.ReadAll(_ui.App.Database.FilePath), DatabaseRows.ReadAll(snapshot));
    }

    [Fact]
    public async Task ExportData_ShareFails_ShowsTheFixedExportMessage()
    {
        await SeedAsync(_ui.App);
        _ui.Share.RequestAsync(Arg.Any<ShareFileRequest>()).ThrowsAsync(new InvalidOperationException());

        await _settings.ExportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleError, AppResources.BodyExportFailed);
    }

    [Fact]
    public async Task ImportData_WorkoutActive_RefusesWithoutTouchingData()
    {
        await StartWorkoutAsync();

        await _settings.ImportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleHoldOn, AppResources.BodyImportBlockedByWorkout);
        await _ui.FilePicker.DidNotReceiveWithAnyArgs().PickAsync(default);
    }

    [Fact]
    public async Task ImportData_PickerCancelled_DoesNothing()
    {
        await SeedAsync(_ui.App);
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);

        await _settings.ImportDataCommand.ExecuteAsync(null);

        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
        await _ui.Dialogs.DidNotReceiveWithAnyArgs().ShowMessageAsync(default!, default!);
    }

    [Fact]
    public async Task ImportData_Confirmed_ReplacesTheDataAndResetsTheApp()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        await SeedOwnDataAsync();
        var picked = await PickBackupAsync();
        _ui.AnswerConfirmations(true);

        await _settings.ImportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ConfirmAsync(AppResources.TitleImportData, ImportConfirmation, AppResources.ButtonImport, AppResources.ButtonCancel);
        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleSuccess, "Imported 3 exercises, 2 workouts and 1 session.");
        Assert.Equal(["Bench Press", "Overhead Press", "Back Squat"], (await _ui.App.Exercises.GetAllAsync()).Select(exercise => exercise.Name));
        _ui.NavigationStack.Received(1).PopAllTabsToRoot();
        Assert.False(File.Exists(picked));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_ui.App.Database.CacheFolder));
    }

    [Fact]
    public async Task ImportData_Cancelled_KeepsTheDataAndDeletesTheCopies()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);
        await PickBackupAsync();
        _ui.AnswerConfirmations(false);

        await _settings.ImportDataCommand.ExecuteAsync(null);

        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
        _ui.NavigationStack.DidNotReceive().PopAllTabsToRoot();
        Assert.Empty(Directory.EnumerateFileSystemEntries(_ui.App.Database.CacheFolder));
    }

    [Fact]
    public async Task ImportData_NotABackup_SaysSoAndKeepsTheData()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);
        var picked = PickerCopyPath();
        ImportFiles.PlainText(picked);
        _ui.FilePicker.PickAsync(Arg.Any<PickOptions>()).Returns(new FileResult(picked));

        await _settings.ImportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleOops, AppResources.BodyImportNotBackup);
        await _ui.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
    }

    [Fact]
    public async Task ImportData_CopyFails_ShowsTheFixedImportMessageAndKeepsTheData()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);
        await PickBackupAsync();
        _ui.AnswerConfirmations(true);
        await _ui.App.Database.FailOnAsync(nameof(Exercise), TableOperation.Insert);

        await _settings.ImportDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleError, AppResources.BodyImportFailed);
        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
        _ui.NavigationStack.DidNotReceive().PopAllTabsToRoot();
        Assert.Empty(Directory.EnumerateFileSystemEntries(_ui.App.Database.CacheFolder));
    }

    [Fact]
    public async Task EraseData_Confirmed_EmptiesEveryTableAndResetsTheApp()
    {
        await SeedOwnDataAsync();
        await StartWorkoutAsync();
        _settings.OpenEraseSheetCommand.Execute(null);

        await _settings.EraseDataCommand.ExecuteAsync(null);

        Assert.False(_settings.IsEraseSheetOpen);
        Assert.All(DatabaseRows.ReadAll(_ui.App.Database.FilePath), table => Assert.Empty(table.Value));
        Assert.False(_ui.ActiveWorkout.IsWorkoutActive);
        _ui.WorkoutTicker.Received().Stop();
        _ui.NavigationStack.Received(1).PopAllTabsToRoot();
        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleSuccess, AppResources.BodyDataErased);
    }

    [Fact]
    public async Task CloseEraseSheet_Cancelled_KeepsTheData()
    {
        await SeedOwnDataAsync();
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);
        _settings.OpenEraseSheetCommand.Execute(null);

        _settings.CloseEraseSheetCommand.Execute(null);

        Assert.False(_settings.IsEraseSheetOpen);
        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
    }

    [Fact]
    public async Task EraseData_DeleteFails_ShowsTheFixedErrorAndKeepsEverything()
    {
        await SeedOwnDataAsync();
        await StartWorkoutAsync();
        var before = DatabaseRows.ReadAll(_ui.App.Database.FilePath);
        await _ui.App.Database.FailOnAsync(nameof(WorkoutSession), TableOperation.Delete);

        await _settings.EraseDataCommand.ExecuteAsync(null);

        await _ui.Dialogs.Received(1).ShowMessageAsync(AppResources.TitleError, AppResources.BodyOperationFailed);
        Assert.Equal(before, DatabaseRows.ReadAll(_ui.App.Database.FilePath));
        Assert.True(_ui.ActiveWorkout.IsWorkoutActive);
        _ui.NavigationStack.DidNotReceive().PopAllTabsToRoot();
    }

    private async Task SeedOwnDataAsync()
    {
        var curl = await _ui.App.Data.ExerciseAsync("Barbell Curl", MuscleGroup.Biceps);
        var arms = await _ui.App.Data.WorkoutAsync("Arm Day", curl);
        await _ui.App.Data.CompletedSessionAsync(arms, TestApp.StartUtc, new LoggedSet(0, 30, 12));
    }

    private async Task StartWorkoutAsync()
    {
        var push = (await _ui.App.Workouts.GetAllAsync()).FirstOrDefault()
            ?? await _ui.App.Data.WorkoutAsync("Push Day", await _ui.App.Data.ExerciseAsync("Bench Press"));
        _ui.ActiveWorkout.ApplyQueryAttributes(new Dictionary<string, object> { [QueryKeys.Workout] = push });
        await _ui.SettleAsync();
    }

    private async Task<string> PickBackupAsync()
    {
        await using var source = new TestApp();
        await SeedAsync(source);
        var snapshot = await source.Maintenance.CreateExportSnapshotAsync();
        Assert.NotNull(snapshot);
        var picked = PickerCopyPath();
        File.Copy(snapshot, picked);
        _ui.FilePicker.PickAsync(Arg.Any<PickOptions>()).Returns(new FileResult(picked));
        return picked;
    }

    private string PickerCopyPath()
    {
        var folder = Path.Combine(_ui.App.Database.CacheFolder, PickerFolderName);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, BackupFileName);
    }

    private static async Task SeedAsync(TestApp app)
    {
        var bench = await app.Data.ExerciseAsync("Bench Press", MuscleGroup.Chest);
        var press = await app.Data.ExerciseAsync("Overhead Press", MuscleGroup.Shoulders);
        var squat = await app.Data.ExerciseAsync("Back Squat", MuscleGroup.Legs);
        var push = await app.Data.WorkoutAsync("Push Day", bench, press);
        await app.Data.WorkoutAsync("Leg Day", squat);
        await app.Data.CompletedSessionAsync(push, TestApp.StartUtc, new LoggedSet(0, 80, 5));
    }
}
