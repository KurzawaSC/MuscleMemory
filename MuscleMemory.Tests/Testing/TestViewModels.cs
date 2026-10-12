using MuscleMemory.Services;
using MuscleMemory.ViewModels;
using NSubstitute;

namespace MuscleMemory.Tests.Testing;

public sealed class TestViewModels : IAsyncDisposable
{
    public const string AppVersion = "1.4.2";

    private readonly Lazy<ActiveWorkoutViewModel> _activeWorkout;

    public TestViewModels(string systemCulture = Cultures.EnglishUs)
    {
        Dispatcher.CreateTimer().Returns(WorkoutTicker);
        NavigationStack.ConfirmDiscardingChangesOnTabAsync(Arg.Any<string>()).Returns(true);
        NavigationStack.ConfirmDiscardingAllChangesAsync().Returns(true);
        AppInfo.VersionString.Returns(AppVersion);
        FileReader.OpenReadAsync(Arg.Any<FileResult>()).Returns(call => File.OpenRead(call.Arg<FileResult>().FullPath));

        App = new TestApp(new DataChangeNotifier(Dispatcher));
        Errors = new TrackingErrorHandler(new ErrorHandler(Dialogs));
        Timer = new WorkoutTimerService(App.Clock, Dispatcher);
        Localization = CreateLocalization(Preferences, systemCulture);
        Theme = new ThemeService(StatusBar, Preferences);
        _activeWorkout = new(CreateActiveWorkout);
    }

    public TestApp App { get; }

    public TestClock Clock => App.Clock;

    public IDispatcher Dispatcher { get; } = Substitute.For<IDispatcher>();

    public IDispatcherTimer WorkoutTicker { get; } = Substitute.For<IDispatcherTimer>();

    public IDialogService Dialogs { get; } = Substitute.For<IDialogService>();

    public INavigationService Navigation { get; } = Substitute.For<INavigationService>();

    public INavigationStackService NavigationStack { get; } = Substitute.For<INavigationStackService>();

    public IHapticService Haptics { get; } = Substitute.For<IHapticService>();

    public IAudioCueService AudioCues { get; } = Substitute.For<IAudioCueService>();

    public IStatusBarService StatusBar { get; } = Substitute.For<IStatusBarService>();

    public IAppInfo AppInfo { get; } = Substitute.For<IAppInfo>();

    public IShare Share { get; } = Substitute.For<IShare>();

    public IFilePicker FilePicker { get; } = Substitute.For<IFilePicker>();

    public IFileResultReader FileReader { get; } = Substitute.For<IFileResultReader>();

    public MemoryPreferences Preferences { get; } = new();

    public TrackingErrorHandler Errors { get; }

    public WorkoutTimerService Timer { get; }

    public LocalizationService Localization { get; }

    public ThemeService Theme { get; }

    public ActiveWorkoutViewModel ActiveWorkout => _activeWorkout.Value;

    public void Tick() => WorkoutTicker.Tick += Raise.Event();

    public Task SettleAsync() => Errors.DrainAsync();

    public void AnswerConfirmations(bool answer) =>
        Dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(answer);

    public TaskCompletionSource<bool> HoldConfirmations()
    {
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(answer.Task);
        return answer;
    }

    public WorkoutHistoryViewModel CreateWorkoutHistory() =>
        new(App.History, App.ActiveSessions, Dialogs, Timer, Haptics, Navigation, Errors, Clock, CreateExercisePicker());

    public ExerciseHistoryViewModel CreateExerciseHistory() => new(App.History, Navigation, Errors);

    public ExercisePickerViewModel CreateExercisePicker() => new(App.Catalog, new ExerciseFilterViewModel());

    public ExerciseFormViewModel CreateExerciseForm() => new(App.Catalog);

    public ExerciseListViewModel CreateExerciseList() =>
        new(App.Exercises, App.Workouts, App.Catalog, ActiveWorkout, CreateExerciseForm(), new ExerciseFilterViewModel(),
            Haptics, Dialogs, App.DataChanges, Navigation, Errors, Localization, Clock);

    public WorkoutListViewModel CreateWorkoutList() =>
        new(App.Workouts, ActiveWorkout, Haptics, Dialogs, App.DataChanges, Navigation, Errors, Clock);

    public AddEditWorkoutViewModel CreateWorkoutEditor() =>
        new(App.Workouts, CreateExercisePicker(), new ExerciseConfigurationViewModel(), CreateExerciseForm(),
            Dialogs, NavigationStack, Navigation, Errors, Clock);

    public SettingsViewModel CreateSettings() =>
        new(App.Maintenance, Theme, Dialogs, NavigationStack, Errors, Localization, App.DataChanges, Clock,
            AppInfo, Share, FilePicker, FileReader, ActiveWorkout);

    public ValueTask DisposeAsync() => App.DisposeAsync();

    private static LocalizationService CreateLocalization(IPreferences preferences, string systemCulture)
    {
        LocalizationService? localization = null;
        ExecutionContext.Run(ExecutionContext.Capture()!, _ =>
        {
            using var scope = new CultureScope(systemCulture);
            localization = new LocalizationService(preferences);
        }, null);
        return localization!;
    }

    private ActiveWorkoutViewModel CreateActiveWorkout() =>
        new(App.ActiveSessions, Timer, Clock, AudioCues, Dialogs, App.Summary, NavigationStack, Navigation, Haptics, Errors, Localization);
}
