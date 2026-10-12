using System.Globalization;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    private readonly IDatabaseMaintenanceService _maintenance = Substitute.For<IDatabaseMaintenanceService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly IErrorHandler _errors = Substitute.For<IErrorHandler>();
    private readonly ILocalizationService _localization = Substitute.For<ILocalizationService>();
    private readonly ActiveWorkoutViewModel _activeWorkout;
    private readonly SettingsViewModel _settings;

    public SettingsViewModelTests()
    {
        _errors.RunAsync(Arg.Any<Func<Task>>(), Arg.Any<string?>(), Arg.Any<string>())
            .Returns(call => call.Arg<Func<Task>>()());
        _localization.AvailableLanguages.Returns([CultureInfo.GetCultureInfo(Cultures.English)]);

        _activeWorkout = new ActiveWorkoutViewModel(
            Substitute.For<IActiveWorkoutSessionService>(),
            Substitute.For<IWorkoutTimerService>(),
            TimeProvider.System,
            Substitute.For<IAudioCueService>(),
            _dialogs,
            Substitute.For<IWorkoutSummaryService>(),
            Substitute.For<INavigationStackService>(),
            Substitute.For<INavigationService>(),
            Substitute.For<IHapticService>(),
            _errors,
            _localization);

        _settings = new SettingsViewModel(
            _maintenance,
            Substitute.For<IThemeService>(),
            _dialogs,
            Substitute.For<INavigationStackService>(),
            _errors,
            _localization,
            Substitute.For<IDataChangeNotifier>(),
            TimeProvider.System,
            _activeWorkout);
    }

    [Fact]
    public async Task ImportData_WorkoutActive_RefusesWithoutTouchingData()
    {
        using var scope = new CultureScope(Cultures.EnglishUs);
        _activeWorkout.IsWorkoutActive = true;

        await _settings.ImportDataCommand.ExecuteAsync(null);

        await _dialogs.Received(1).ShowMessageAsync(AppResources.TitleHoldOn, AppResources.BodyImportBlockedByWorkout);
        Assert.Empty(_maintenance.ReceivedCalls());
    }
}
