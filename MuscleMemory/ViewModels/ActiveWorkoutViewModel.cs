using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Diagnostics;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class ActiveWorkoutViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly ISessionExerciseRepository _sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository;
    private readonly IActiveWorkoutStateRepository _activeStateRepository;
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
    private WorkoutSet? _setBeingEdited;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    [NotifyPropertyChangedFor(nameof(CanAddItems))]
    public partial bool IsWorkoutActive { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    public partial bool IsOnActiveWorkoutPage { get; set; }

    public bool IsBannerVisible => IsWorkoutActive && !IsOnActiveWorkoutPage;

    public bool CanAddItems => !IsWorkoutActive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSetCommand))]
    [NotifyCanExecuteChangedFor(nameof(UndoLastSetCommand))]
    [NotifyCanExecuteChangedFor(nameof(PreviousExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(FinishWorkoutCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowSetActionsCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExitWorkoutCommand))]
    [NotifyCanExecuteChangedFor(nameof(ViewCompletedHistoryCommand))]
    public partial bool IsBusy { get; private set; }

    private bool IsIdle => !IsBusy;

    private string ZeroTimeText => _timer.FormatDuration(TimeSpan.Zero);

    [ObservableProperty]
    public partial string WorkoutTitle { get; set; } = UiText.LoadingWorkoutTitle;

    [ObservableProperty]
    public partial string TimerText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TotalTimeText { get; set; } = string.Empty;

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
    public partial bool IsWorkoutCompleted { get; set; }

    [ObservableProperty]
    public partial double TotalVolume { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSetsCaption))]
    public partial int TotalSets { get; set; }

    public string TotalSetsCaption => CountCaption.Sets(TotalSets);

    [ObservableProperty]
    public partial string SummaryDateText { get; set; } = string.Empty;

    public ObservableCollection<SummaryExerciseItem> CompletedExercises { get; } = [];

    [ObservableProperty]
    public partial string LastSessionResultsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLastSession { get; set; }

    [ObservableProperty]
    public partial string RestTimerText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RestTotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RestProgress { get; set; }

    [ObservableProperty]
    public partial bool IsRestEnding { get; set; }

    public SetInputViewModel SetInput { get; } = new();

    public SetInputViewModel SetEditor { get; } = new();

    [ObservableProperty]
    public partial bool IsSetEditorOpen { get; set; }

    public string CurrentVolumeText => string.Format(CultureInfo.CurrentCulture, UiText.VolumeFormat, CurrentSets.TotalVolume());

    [ObservableProperty]
    public partial bool IsSetActionSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionSetTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSetSubtitle))]
    public partial WorkoutSet? ActionSet { get; set; }

    public string ActionSetTitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.SetProgressFormat, set.SetNumber) : string.Empty;

    public string ActionSetSubtitle => ActionSet is { } set ? string.Format(CultureInfo.CurrentCulture, UiText.LoggedSetFormat, set.Weight, set.Reps) : string.Empty;

    public ActiveWorkoutViewModel(
        IWorkoutRepository workoutRepository,
        IWorkoutSessionRepository sessionRepository,
        ISessionExerciseRepository sessionExerciseRepository,
        IWorkoutSetRepository setRepository,
        IActiveWorkoutStateRepository activeStateRepository,
        IWorkoutTimerService timer,
        IAudioCueService audioCues,
        IDialogService dialogs,
        IWorkoutSummaryService summaryService,
        INavigationStackService navigationStack,
        INavigationService navigation,
        IHapticService haptics,
        IErrorHandler errors)
    {
        _workoutRepository = workoutRepository;
        _sessionRepository = sessionRepository;
        _sessionExerciseRepository = sessionExerciseRepository;
        _setRepository = setRepository;
        _activeStateRepository = activeStateRepository;
        _timer = timer;
        _audioCues = audioCues;
        _dialogs = dialogs;
        _summaryService = summaryService;
        _navigationStack = navigationStack;
        _navigation = navigation;
        _haptics = haptics;
        _errors = errors;

        TimerText = ZeroTimeText;
        TotalTimeText = ZeroTimeText;
        RestTimerText = ZeroTimeText;
        _timer.Ticked += OnTimerTicked;
        CurrentSets.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CurrentVolumeText));
    }

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
        _haptics.LongPress();
        AppLog.LogFailures(_audioCues.PlayBreakEndAsync());
        _errors.ReportFailures(SaveStateAsync());
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.Workout, out var value) && value is Workout workout && !IsWorkoutActive && IsIdle)
        {
            _errors.ReportFailures(RunExclusiveAsync(() => StartWorkoutAsync(workout)));
        }
    }

    private async Task RunExclusiveAsync(Func<Task> operation)
    {
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

        var template = await _workoutRepository.GetExercisesAsync(workout.Id);
        var session = await _sessionRepository.CreateWithSnapshotAsync(workout, template);
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
        await _activeStateRepository.SaveAsync(state);
    }

    public Task LoadStateAsync() => RunExclusiveAsync(RestoreStateAsync);

    private async Task RestoreStateAsync()
    {
        var state = await _activeStateRepository.GetAsync();
        if (state is null)
        {
            return;
        }

        var session = await _sessionRepository.GetAsync(state.SessionId);
        if (session is not { EndTimeUtc: null })
        {
            await _activeStateRepository.ClearAsync();
            return;
        }

        _sessionId = state.SessionId;
        _workoutStartTimeUtc = state.StartTimeUtc;
        _currentExerciseIndex = state.CurrentExerciseIndex;
        RestoreRest(state);
        IsWorkoutActive = true;
        IsWorkoutCompleted = false;
        WorkoutTitle = session.WorkoutName;
        _workoutId = session.WorkoutId;

        var performedExercises = await _sessionExerciseRepository.GetForSessionAsync(_sessionId);
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

        var lastSessionSets = await _setRepository.GetLastSessionSetsAsync(exercise.ExerciseId, _sessionId);
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
        CurrentSets.ReplaceAll(await _setRepository.GetForSessionExerciseAsync(CurrentExercise.Id));

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
        List<SessionExercise> plannedExercises = [.. Exercises.Where(exercise => exercise.PlannedSets > 0)];
        if (plannedExercises.Count == 0)
        {
            IsPlanComplete = false;
            return;
        }

        var loggedSets = await _setRepository.GetForSessionExercisesAsync([.. plannedExercises.Select(exercise => exercise.Id)]);
        var loggedCounts = loggedSets.CountBy(set => set.SessionExerciseId).ToDictionary();
        IsPlanComplete = plannedExercises.All(exercise => loggedCounts.GetValueOrDefault(exercise.Id) >= exercise.PlannedSets);
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
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

        await _setRepository.AddAsync(newSet);
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
        await _sessionRepository.FinishOrDiscardAsync(_sessionId);
        await _activeStateRepository.ClearAsync();

        _timer.Stop();
        ClearRestState();
        _audioCues.Stop();

        if (!summary.HasLoggedSets)
        {
            IsWorkoutActive = false;
            await _navigation.GoToAsync(NavigationRoutes.GoBack);
            return;
        }

        TotalTimeText = _timer.ElapsedSince(_workoutStartTimeUtc);
        ShowSummary(summary);
        IsWorkoutCompleted = true;
        IsWorkoutActive = false;
    }

    private void ShowSummary(WorkoutSummary summary)
    {
        CompletedExercises.ReplaceAll(summary.Exercises.Select(SummaryExerciseItem.Create));
        TotalVolume = summary.TotalVolume;
        TotalSets = summary.Exercises.Sum(exercise => exercise.Sets.Count);
        SummaryDateText = _workoutStartTimeUtc.ToLocalTime().ToString(UiText.SummaryDateFormat, CultureInfo.InvariantCulture);
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

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private void ShowSetActions(WorkoutSet set)
    {
        ActionSet = set;
        IsSetActionSheetOpen = true;
    }

    [RelayCommand]
    private void CancelSetActions()
    {
        IsSetActionSheetOpen = false;
        ActionSet = null;
    }

    [RelayCommand]
    private Task EditActionSetAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        _setBeingEdited = set;
        SetEditor.Fill(set.Weight, set.Reps);
        IsSetEditorOpen = true;
    });

    [RelayCommand]
    private Task SaveEditedSetAsync() => _errors.RunAsync(async () =>
    {
        if (_setBeingEdited is not { } set || !SetEditor.TryRead(out var values))
        {
            return;
        }

        await _setRepository.UpdateAsync(set.Id, values.Weight, values.Reps);
        CloseSetEditor();
        await LoadSetsForCurrentExerciseAsync();
    });

    [RelayCommand]
    private void CloseSetEditor()
    {
        IsSetEditorOpen = false;
        _setBeingEdited = null;
    }

    [RelayCommand]
    private Task DeleteActionSetAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        if (!await _dialogs.ConfirmAsync(UiText.TitleDeleteSet, UiText.BodyDeleteSetConfirmation, UiText.ButtonDelete, UiText.ButtonCancel))
        {
            return;
        }

        await RemoveSetAsync(set);
    });

    private async Task<WorkoutSet?> DismissSetActionsAsync()
    {
        var set = ActionSet;
        CancelSetActions();
        await Task.Delay(UiTiming.SheetClose);
        return set;
    }

    private async Task RemoveSetAsync(WorkoutSet set)
    {
        await _setRepository.DeleteAsync(set.Id);
        await LoadSetsForCurrentExerciseAsync();

        UpdateSetProgress();
        await UpdatePlanCompletionAsync();
    }

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task UndoLastSetAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (!CurrentSets.Any())
        {
            return;
        }

        await RemoveSetAsync(CurrentSets[^1]);
    }));

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task PreviousExerciseAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (HasPreviousExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex - 1);
        }
    }));

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task NextExerciseAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        if (HasNextExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex + 1);
        }
    }));

    [RelayCommand(CanExecute = nameof(IsIdle))]
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
        if (IsSetEditorOpen)
        {
            CloseSetEditor();
            return;
        }

        if (IsSetActionSheetOpen)
        {
            CancelSetActions();
            return;
        }

        if (!IsWorkoutCompleted)
        {
            await _navigation.GoToAsync(NavigationRoutes.GoBack);
            return;
        }

        if (ExitWorkoutCommand.CanExecute(null))
        {
            await ExitWorkoutCommand.ExecuteAsync(null);
        }
    });

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private Task ExitWorkoutAsync() => _errors.RunAsync(() => RunExclusiveAsync(async () =>
    {
        await _navigation.GoToAsync(NavigationRoutes.GoBack);
        ClearCompletedSummary();
    }));

    [RelayCommand(CanExecute = nameof(IsIdle))]
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
        if (IsWorkoutCompleted)
        {
            ClearSummary();
        }
    }

    private void ClearSummary()
    {
        IsWorkoutCompleted = false;
        CompletedExercises.Clear();
        TotalVolume = 0;
        TotalSets = 0;
        SummaryDateText = string.Empty;
        TotalTimeText = ZeroTimeText;
    }

    private void ResetDisplayState()
    {
        ClearRestState();
        CancelSetActions();
        CloseSetEditor();
        ResetCurrentExercise();
        ClearSummary();

        IsExercisesEmpty = false;
        IsPlanComplete = false;
        Exercises.Clear();

        WorkoutTitle = UiText.LoadingWorkoutTitle;
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
        shell.Navigated += (_, _) => IsOnActiveWorkoutPage = shell.CurrentPage is ActiveWorkoutPage;
    }
}
