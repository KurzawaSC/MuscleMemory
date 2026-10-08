using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Diagnostics;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ActiveWorkoutViewModel : ObservableObject, IQueryAttributable, ISetActionsHost
{
    private const char RouteSeparator = '/';

    private readonly IActiveWorkoutSessionService _sessions;
    private readonly IWorkoutTimerService _timer;
    private readonly IAudioCueService _audioCues;
    private readonly IDialogService _dialogs;
    private readonly IWorkoutSummaryService _summaryService;
    private readonly INavigationStackService _navigationStack;
    private readonly INavigationService _navigation;
    private readonly IHapticService _haptics;
    private readonly IErrorHandler _errors;
    private int _sessionId;
    private int _workoutId;
    private int _currentExerciseIndex;
    private int _totalSetsForExercise;
    private int _restDurationSeconds;
    private DateTime _workoutStartTimeUtc;
    private DateTime _breakEndTimeUtc;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    [NotifyPropertyChangedFor(nameof(CanAddItems))]
    public partial bool IsWorkoutActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    public partial bool IsOnActiveWorkoutPage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAddItems))]
    public partial bool IsStateRestored { get; private set; }

    public bool IsBannerVisible => IsWorkoutActive && !IsOnActiveWorkoutPage;

    public bool CanAddItems => IsStateRestored && !IsWorkoutActive;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    private bool CanSaveSet => SetInput.IsValid;

    private string ZeroTimeText => _timer.FormatDuration(TimeSpan.Zero);

    [ObservableProperty]
    public partial string WorkoutTitle { get; set; } = UiText.LoadingText;

    [ObservableProperty]
    public partial string TimerText { get; set; } = string.Empty;

    public ObservableCollection<SessionExercise> Exercises { get; } = [];
    public ObservableCollection<WorkoutSet> CurrentSets { get; } = [];
    public ObservableCollection<LevelSegment> SetSegments { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetText))]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial SessionExercise CurrentExercise { get; set; } = new();

    public string TargetText => CurrentExercise.PlannedReps > 0
        ? string.Format(CultureInfo.CurrentCulture, UiText.TargetRepsFormat, CurrentExercise.PlannedReps, CurrentExercise.TargetRPE)
        : string.Format(CultureInfo.CurrentCulture, UiText.TargetRpeFormat, CurrentExercise.TargetRPE);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial string ExerciseProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial string SetProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasSavedSets { get; set; }
    [ObservableProperty]
    public partial bool IsExercisesEmpty { get; set; }

    [ObservableProperty]
    public partial bool HasPreviousExercise { get; set; }

    [ObservableProperty]
    public partial bool HasNextExercise { get; set; }

    [ObservableProperty]
    public partial bool IsPlanComplete { get; set; }

    public string ProgressCaption => IsResting
        ? string.Join(UiText.ListSeparator, SetProgressText, TargetText)
        : string.Join(UiText.ListSeparator, ExerciseProgressText, SetProgressText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial bool IsResting { get; set; }

    [ObservableProperty]
    public partial string LastSessionResultsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLastSession { get; set; }

    [ObservableProperty]
    public partial string RestTimerText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RestTotalText { get; set; } = string.Empty;

    public string RestExtensionText { get; } = string.Format(CultureInfo.CurrentCulture, UiText.RestExtensionFormat, DomainDefaults.RestExtensionInSeconds);

    [ObservableProperty]
    public partial double RestProgress { get; set; }

    [ObservableProperty]
    public partial bool IsRestEnding { get; set; }

    public SetInputViewModel SetInput { get; } = new();

    public SetActionsViewModel SetActions { get; }

    public WorkoutSummaryViewModel Summary { get; }

    public string CurrentVolumeText => string.Format(CultureInfo.CurrentCulture, UiText.VolumeFormat, CurrentSets.TotalVolume());

    public ActiveWorkoutViewModel(
        IActiveWorkoutSessionService sessions,
        IWorkoutTimerService timer,
        IAudioCueService audioCues,
        IDialogService dialogs,
        IWorkoutSummaryService summaryService,
        INavigationStackService navigationStack,
        INavigationService navigation,
        IHapticService haptics,
        IErrorHandler errors)
    {
        _sessions = sessions;
        _timer = timer;
        _audioCues = audioCues;
        _dialogs = dialogs;
        _summaryService = summaryService;
        _navigationStack = navigationStack;
        _navigation = navigation;
        _haptics = haptics;
        _errors = errors;
        SetActions = new SetActionsViewModel(this, dialogs, errors);
        Summary = new WorkoutSummaryViewModel(timer);

        TimerText = ZeroTimeText;
        RestTimerText = ZeroTimeText;
        _timer.Ticked += OnTimerTicked;
        CurrentSets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CurrentVolumeText));
        SaveSetCommand.NotifyCanExecuteChangedWhen(SetInput, nameof(SetInputViewModel.IsValid));
        SetActions.ShowCommand.NotifyCanExecuteChangedWhen(this, nameof(IsBusy));
    }

    bool ISetActionsHost.CanShowSetActions => !IsBusy;

    Task ISetActionsHost.UpdateSetAsync(WorkoutSet set, SetValues values) =>
        _sessions.UpdateSetAsync(set.Id, values.Weight, values.Reps);

    Task ISetActionsHost.DeleteSetAsync(WorkoutSet set) => RemoveSetAsync(set);

    Task ISetActionsHost.RefreshSetsAsync(int sessionExerciseId) => LoadSetsForCurrentExerciseAsync();

    private void OnTimerTicked(object? sender, EventArgs e)
    {
        if (IsWorkoutActive)
        {
            TimerText = _timer.ElapsedSince(_workoutStartTimeUtc);
        }

        if (!IsResting)
        {
            return;
        }

        if (_timer.RemainingUntil(_breakEndTimeUtc).TotalSeconds > 0)
        {
            UpdateRestCountdown();
            return;
        }

        ClearRestState();
        _haptics.RestFinished();
        AppLog.LogFailures(_audioCues.PlayBreakEndAsync());
        _errors.ReportFailures(SaveStateAsync());
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.Workout, out var value) && value is Workout workout && !IsWorkoutActive)
        {
            _errors.ReportFailures(RunExclusiveAsync(() => StartWorkoutAsync(workout)));
        }
    }

    private async Task RunExclusiveAsync(Func<Task> operation)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await operation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StartWorkoutAsync(Workout workout)
    {
        ResetDisplayState();
        _workoutStartTimeUtc = DateTime.UtcNow;

        WorkoutTitle = workout.Name;
        _workoutId = workout.Id;

        var session = await _sessions.StartAsync(workout);
        _sessionId = session.SessionId;

        _timer.Start();

        await ShowExercisesAsync(session.Exercises, restoreIndex: false);

        await Task.Delay(UiTiming.NavigationAnimation);
        IsWorkoutActive = true;
        await SaveStateAsync();
    }

    private async Task SaveStateAsync()
    {
        if (!IsWorkoutActive)
        {
            return;
        }

        var state = new ActiveWorkoutState
        {
            SessionId = _sessionId,
            StartTimeUtc = _workoutStartTimeUtc,
            CurrentExerciseIndex = _currentExerciseIndex,
            IsResting = IsResting,
            BreakEndTimeUtc = _breakEndTimeUtc,
            RestDurationSeconds = _restDurationSeconds
        };
        await _sessions.SaveStateAsync(state);
    }

    public async Task LoadStateAsync()
    {
        try
        {
            await RunExclusiveAsync(RestoreStateAsync);
        }
        finally
        {
            IsStateRestored = true;
        }
    }

    private async Task RestoreStateAsync()
    {
        if (await _sessions.FindResumableAsync() is not { } resumable)
        {
            return;
        }

        var (state, session) = resumable;
        _sessionId = state.SessionId;
        _workoutStartTimeUtc = state.StartTimeUtc;
        _currentExerciseIndex = state.CurrentExerciseIndex;
        RestoreRest(state);
        IsWorkoutActive = true;
        Summary.Clear();
        WorkoutTitle = session.WorkoutName;
        _workoutId = session.WorkoutId;

        var performedExercises = await _sessions.GetExercisesAsync(_sessionId);
        await ShowExercisesAsync(performedExercises, restoreIndex: true);

        _timer.Start();
    }

    private void RestoreRest(ActiveWorkoutState state)
    {
        if (!state.IsResting || _timer.RemainingUntil(state.BreakEndTimeUtc) <= TimeSpan.Zero)
        {
            ClearRestState();
            return;
        }

        ShowRest(state.BreakEndTimeUtc, state.RestDurationSeconds);
    }

    private async Task ShowExercisesAsync(List<SessionExercise> performedExercises, bool restoreIndex)
    {
        Exercises.ReplaceAll(performedExercises);

        if (Exercises.Any())
        {
            IsExercisesEmpty = false;
            _currentExerciseIndex = restoreIndex ? Math.Clamp(_currentExerciseIndex, 0, Exercises.Count - 1) : 0;
            await AdvanceToExerciseAsync(_currentExerciseIndex);
        }
        else
        {
            IsExercisesEmpty = true;
            ResetCurrentExercise();
        }

        await UpdatePlanCompletionAsync();
    }

    private void ResetCurrentExercise()
    {
        _currentExerciseIndex = 0;
        _totalSetsForExercise = 0;
        CurrentExercise = new();
        CurrentSets.Clear();
        SetSegments.Clear();
        HasSavedSets = false;
        HasPreviousExercise = false;
        HasNextExercise = false;
        HasLastSession = false;
        ExerciseProgressText = string.Empty;
        SetProgressText = string.Empty;
        LastSessionResultsText = string.Empty;
        SetInput.Clear();
    }

    private async Task AdvanceToExerciseAsync(int index)
    {
        if (index < 0 || index >= Exercises.Count)
        {
            return;
        }

        _currentExerciseIndex = index;
        var exercise = Exercises[index];
        CurrentExercise = exercise;
        _totalSetsForExercise = exercise.PlannedSets;

        HasPreviousExercise = index > 0;
        HasNextExercise = index < Exercises.Count - 1;

        ExerciseProgressText = string.Format(CultureInfo.CurrentCulture, UiText.ExerciseProgressFormat, index + 1, Exercises.Count);

        var lastSessionSets = await _sessions.GetLastSessionSetsAsync(exercise.ExerciseId, _sessionId);
        HasLastSession = lastSessionSets.Count > 0;
        LastSessionResultsText = HasLastSession
            ? UiText.LastSessionPrefix + string.Join(UiText.ResultSeparator, lastSessionSets.Select(set => string.Format(CultureInfo.CurrentCulture, UiText.SetResultFormat, set.Weight, set.Reps)))
            : UiText.FirstTimePerformingExercise;

        await LoadSetsForCurrentExerciseAsync();
        PrefillInputs(CurrentSets.LastOrDefault() ?? lastSessionSets.FirstOrDefault());
        UpdateSetProgress();
        await SaveStateAsync();
    }

    private void PrefillInputs(WorkoutSet? source)
    {
        if (source is null)
        {
            SetInput.Clear();
            return;
        }

        SetInput.Fill(source.Weight, source.Reps);
    }

    private async Task LoadSetsForCurrentExerciseAsync()
    {
        CurrentSets.ReplaceAll(await _sessions.GetSetsAsync(CurrentExercise.Id));

        HasSavedSets = CurrentSets.Any();
    }

    private void UpdateSetProgress()
    {
        var currentSetNumber = CurrentSets.Count + 1;

        SetProgressText = currentSetNumber <= _totalSetsForExercise
            ? string.Format(CultureInfo.CurrentCulture, UiText.SetProgressWithTotalFormat, currentSetNumber, _totalSetsForExercise)
            : string.Format(CultureInfo.CurrentCulture, UiText.SetProgressFormat, currentSetNumber);

        if (_totalSetsForExercise > 0)
        {
            SetSegments.ReplaceAll(Enumerable.Range(1, _totalSetsForExercise)
                                             .Select(setNumber => new LevelSegment(setNumber, setNumber <= CurrentSets.Count)));
        }
        else
        {
            SetSegments.Clear();
        }
    }

    private bool HasJustMetPlan() => _totalSetsForExercise > 0 && CurrentSets.Count == _totalSetsForExercise;

    private async Task UpdatePlanCompletionAsync()
    {
        IsPlanComplete = await _sessions.IsPlanCompleteAsync([.. Exercises]);
    }

    [RelayCommand(CanExecute = nameof(CanSaveSet))]
    private Task SaveSetAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (IsExercisesEmpty || !SetInput.TryRead(out var values))
        {
            return;
        }

        var newSet = new WorkoutSet
        {
            SessionExerciseId = CurrentExercise.Id,
            Weight = values.Weight,
            Reps = values.Reps
        };

        await _sessions.AddSetAsync(newSet);
        _haptics.Click();
        SetInput.Fill(newSet.Weight, newSet.Reps);
        CurrentSets.Add(newSet);
        HasSavedSets = true;
        if (CurrentExercise.BreakTimeInSeconds > 0)
        {
            StartRest(CurrentExercise.BreakTimeInSeconds);
        }
        await SaveStateAsync();
        await UpdatePlanCompletionAsync();

        if (HasJustMetPlan() && HasNextExercise)
        {
            await Task.Delay(UiTiming.ExerciseAdvanceDelay);
            await AdvanceToExerciseAsync(_currentExerciseIndex + 1);
            return;
        }

        UpdateSetProgress();
    }));

    private void StartRest(int durationSeconds) => ShowRest(DateTime.UtcNow.AddSeconds(durationSeconds), durationSeconds);

    private void ShowRest(DateTime breakEndTimeUtc, int durationSeconds)
    {
        _restDurationSeconds = durationSeconds;
        _breakEndTimeUtc = breakEndTimeUtc;
        RestTotalText = string.Format(CultureInfo.CurrentCulture, UiText.RestTotalFormat, durationSeconds);
        IsResting = true;
        UpdateRestCountdown();
    }

    private void UpdateRestCountdown()
    {
        var remaining = _timer.RemainingUntil(_breakEndTimeUtc);
        RestTimerText = _timer.FormatDuration(remaining);
        IsRestEnding = remaining <= UiTiming.RestEndingPulse;
        RestProgress = _restDurationSeconds > 0
            ? Math.Clamp(remaining.TotalSeconds / _restDurationSeconds, 0, 1)
            : 0;
    }

    private async Task CompleteWorkoutAsync(WorkoutSummary summary)
    {
        await _sessions.FinishAsync(_sessionId);

        _timer.Stop();
        ClearRestState();
        _audioCues.Stop();

        if (!summary.HasLoggedSets)
        {
            IsWorkoutActive = false;
            await _navigation.GoToAsync(NavigationRoutes.GoBack);
            return;
        }

        Summary.Show(summary, _workoutStartTimeUtc);
        IsWorkoutActive = false;
    }

    private void ClearRestState()
    {
        IsResting = false;
        _breakEndTimeUtc = default;
        _restDurationSeconds = 0;
        RestProgress = 0;
        IsRestEnding = false;
        RestTimerText = ZeroTimeText;
    }

    [RelayCommand]
    private Task ExtendRestAsync() => _errors.RunAsync(async () =>
    {
        if (!IsResting)
        {
            return;
        }

        _restDurationSeconds += DomainDefaults.RestExtensionInSeconds;
        _breakEndTimeUtc = _breakEndTimeUtc.AddSeconds(DomainDefaults.RestExtensionInSeconds);
        RestTotalText = string.Format(CultureInfo.CurrentCulture, UiText.RestTotalFormat, _restDurationSeconds);
        UpdateRestCountdown();
        await SaveStateAsync();
    });

    [RelayCommand]
    private Task SkipRestAsync() => _errors.RunAsync(async () =>
    {
        ClearRestState();

        _audioCues.Stop();
        await SaveStateAsync();
    });

    private async Task RemoveSetAsync(WorkoutSet set)
    {
        await _sessions.DeleteSetAsync(set.Id);
        await LoadSetsForCurrentExerciseAsync();

        UpdateSetProgress();
        await UpdatePlanCompletionAsync();
    }

    [RelayCommand]
    private Task UndoLastSetAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (!CurrentSets.Any())
        {
            return;
        }

        await RemoveSetAsync(CurrentSets[^1]);
    }));

    [RelayCommand]
    private Task PreviousExerciseAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (HasPreviousExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex - 1);
        }
    }));

    [RelayCommand]
    private Task NextExerciseAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (HasNextExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex + 1);
        }
    }));

    [RelayCommand]
    private Task FinishWorkoutAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        var summary = await _summaryService.BuildAsync([.. Exercises]);

        var isConfirmed = await _dialogs.ConfirmAsync(
            UiText.TitleFinishWorkout,
            summary.HasLoggedSets ? UiText.BodyFinishWorkoutConfirmation : UiText.BodyFinishUnsavedWorkoutConfirmation,
            UiText.ButtonFinish,
            UiText.ButtonCancel);

        if (!isConfirmed)
        {
            return;
        }

        await CompleteWorkoutAsync(summary);
    }));

    [RelayCommand]
    private Task ResumeWorkoutAsync() => _errors.RunAsync(async () =>
    {
        if (await _navigationStack.ConfirmDiscardingChangesOnTabAsync(NavigationRoutes.WorkoutTab))
        {
            await _navigation.GoToAsync(NavigationRoutes.ActiveWorkoutOnWorkoutTab);
        }
    });

    [RelayCommand]
    private Task NavigateBackAsync() => _errors.RunAsync(async () =>
    {
        if (SetActions.IsAnySheetOpen)
        {
            SetActions.CloseSheets();
            return;
        }

        if (!Summary.IsVisible)
        {
            await _navigation.GoToAsync(NavigationRoutes.GoBack);
            return;
        }

        if (ExitWorkoutCommand.CanExecute(null))
        {
            await ExitWorkoutCommand.ExecuteAsync(null);
        }
    });

    [RelayCommand]
    private Task ExitWorkoutAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        await _navigation.GoToAsync(NavigationRoutes.GoBack);
        ClearCompletedSummary();
    }));

    [RelayCommand]
    private Task ViewCompletedHistoryAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutId, _workoutId },
            { QueryKeys.WorkoutName, WorkoutTitle }
        };
        await _navigation.GoToAsync($"{NavigationRoutes.GoBack}/{NavigationRoutes.WorkoutHistory}", navigationParameter);
        ClearCompletedSummary();
    }));

    private void ClearCompletedSummary()
    {
        if (Summary.IsVisible)
        {
            Summary.Clear();
        }
    }

    private void ResetDisplayState()
    {
        ClearRestState();
        SetActions.CloseSheets();
        ResetCurrentExercise();
        Summary.Clear();

        IsExercisesEmpty = false;
        IsPlanComplete = false;
        Exercises.Clear();

        WorkoutTitle = UiText.LoadingText;
        TimerText = ZeroTimeText;
        RestTotalText = string.Empty;
    }

    public void Reset()
    {
        _timer.Stop();
        _audioCues.Stop();

        ResetDisplayState();

        _sessionId = 0;
        _workoutId = 0;
        _workoutStartTimeUtc = default;

        IsWorkoutActive = false;
    }

    public void TrackCurrentPage(Shell shell)
    {
        shell.Navigated += (_, e) => IsOnActiveWorkoutPage = IsActiveWorkoutLocation(e.Current);
    }

    private static bool IsActiveWorkoutLocation(ShellNavigationState? state) =>
        state?.Location.OriginalString.Split(RouteSeparator)[^1] == NavigationRoutes.ActiveWorkout;
}
