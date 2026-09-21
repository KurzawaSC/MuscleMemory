using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly ISetEditService _setEditService;
    private readonly IWorkoutSummaryService _summaryService;
    private readonly INavigationStackService _navigationStack;
    private readonly IHapticService _haptics;
    private int _sessionId;
    private int _currentExerciseIndex;
    private int _totalSetsForExercise;
    private int _restDurationSeconds;
    private DateTime _workoutStartTimeUtc;
    private DateTime _breakEndTimeUtc;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    [NotifyPropertyChangedFor(nameof(CanAddItems))]
    public partial bool IsWorkoutActive { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    public partial bool IsOnActiveWorkoutPage { get; set; } = false;

    public bool IsBannerVisible => IsWorkoutActive && !IsOnActiveWorkoutPage;

    public bool CanAddItems => !IsWorkoutActive;

    [ObservableProperty]
    public partial string WorkoutTitle { get; set; } = UiText.LoadingWorkoutTitle;

    [ObservableProperty]
    public partial string TimerText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string TotalTimeText { get; set; } = "00:00";

    public ObservableCollection<SessionExercise> Exercises { get; } = [];
    public ObservableCollection<WorkoutSet> CurrentSets { get; } = [];
    public ObservableCollection<LevelSegment> SetSegments { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetText))]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial SessionExercise CurrentExercise { get; set; } = new();

    public string TargetText => CurrentExercise.PlannedReps > 0
        ? string.Format(UiText.TargetRepsFormat, CurrentExercise.PlannedReps, CurrentExercise.TargetRPE)
        : string.Format(UiText.TargetRpeFormat, CurrentExercise.TargetRPE);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial string ExerciseProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial string SetProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasSavedSets { get; set; } = false;
    [ObservableProperty]
    public partial bool IsExercisesEmpty { get; set; } = false;

    [ObservableProperty]
    public partial bool HasPreviousExercise { get; set; } = false;

    [ObservableProperty]
    public partial bool HasNextExercise { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial bool IsExerciseComplete { get; set; } = false;

    public string ProgressCaption => (IsResting, IsExerciseComplete) switch
    {
        (true, _) => string.Join(UiText.ListSeparator, SetProgressText, TargetText),
        (false, true) => ExerciseProgressText,
        (false, false) => string.Join(UiText.ListSeparator, ExerciseProgressText, SetProgressText)
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressCaption))]
    public partial bool IsResting { get; set; } = false;

    [ObservableProperty]
    public partial bool IsWorkoutCompleted { get; set; } = false;

    [ObservableProperty]
    public partial double TotalVolume { get; set; } = 0;

    public ObservableCollection<CompletedExerciseSummary> CompletedExercises { get; } = [];

    [ObservableProperty]
    public partial string LastSessionResultsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasLastSession { get; set; }

    [ObservableProperty]
    public partial string RestTimerText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string RestTotalText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double RestProgress { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveSet))]
    public partial string WeightInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveSet))]
    public partial string RepsInput { get; set; } = string.Empty;

    public bool CanSaveSet => TryReadWeight(out _) && TryReadReps(out _);

    public string CurrentVolumeText => string.Format(UiText.VolumeFormat, CurrentSets.Sum(set => set.Weight * set.Reps));

    [ObservableProperty]
    public partial bool IsSetActionSheetOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionSetTitle))]
    [NotifyPropertyChangedFor(nameof(ActionSetSubtitle))]
    public partial WorkoutSet? ActionSet { get; set; }

    public string ActionSetTitle => ActionSet is { } set ? string.Format(UiText.SetProgressFormat, set.SetNumber) : string.Empty;

    public string ActionSetSubtitle => ActionSet is { } set ? string.Format(UiText.LoggedSetFormat, set.Weight, set.Reps) : string.Empty;

    public ActiveWorkoutViewModel(
        IWorkoutRepository workoutRepository,
        IWorkoutSessionRepository sessionRepository,
        ISessionExerciseRepository sessionExerciseRepository,
        IWorkoutSetRepository setRepository,
        IActiveWorkoutStateRepository activeStateRepository,
        IWorkoutTimerService timer,
        IAudioCueService audioCues,
        ISetEditService setEditService,
        IWorkoutSummaryService summaryService,
        INavigationStackService navigationStack,
        IHapticService haptics)
    {
        _workoutRepository = workoutRepository;
        _sessionRepository = sessionRepository;
        _sessionExerciseRepository = sessionExerciseRepository;
        _setRepository = setRepository;
        _activeStateRepository = activeStateRepository;
        _timer = timer;
        _audioCues = audioCues;
        _setEditService = setEditService;
        _summaryService = summaryService;
        _navigationStack = navigationStack;
        _haptics = haptics;

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
        _ = _audioCues.PlayBreakEndAsync();
        _ = SaveStateAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.Workout, out var value) && value is Workout workout && !IsWorkoutActive)
        {
            _ = StartWorkoutAsync(workout);
        }
    }

    private async Task StartWorkoutAsync(Workout workout)
    {
        _workoutStartTimeUtc = DateTime.UtcNow;
        IsWorkoutCompleted = false;

        WorkoutTitle = workout.Name;

        var session = await _sessionRepository.CreateAsync(workout);
        _sessionId = session.Id;

        var template = await _workoutRepository.GetExercisesAsync(workout.Id);
        var performedExercises = await _sessionExerciseRepository.CreateSnapshotAsync(_sessionId, template);

        _timer.Start();

        await ShowExercisesAsync(performedExercises, restoreIndex: false);

        await Task.Delay(UiTiming.NavigationAnimationMilliseconds);
        IsWorkoutActive = true;
        await SaveStateAsync();
    }

    private async Task SaveStateAsync()
    {
        if (!IsWorkoutActive) return;
        var state = new ActiveWorkoutState
        {
            SessionId = _sessionId,
            StartTimeUtc = _workoutStartTimeUtc,
            CurrentExerciseIndex = _currentExerciseIndex,
            IsResting = IsResting,
            BreakEndTimeUtc = _breakEndTimeUtc
        };
        await _activeStateRepository.SaveAsync(state);
    }

    public async Task LoadStateAsync()
    {
        var state = await _activeStateRepository.GetAsync();
        if (state is null)
        {
            return;
        }

        var session = await _sessionRepository.GetAsync(state.SessionId);
        if (session is null)
        {
            return;
        }

        _sessionId = state.SessionId;
        _workoutStartTimeUtc = state.StartTimeUtc;
        _currentExerciseIndex = state.CurrentExerciseIndex;
        IsResting = state.IsResting;
        _breakEndTimeUtc = state.BreakEndTimeUtc;
        IsWorkoutActive = true;
        IsWorkoutCompleted = false;
        WorkoutTitle = session.WorkoutName;

        var performedExercises = await _sessionExerciseRepository.GetForSessionAsync(_sessionId);
        await ShowExercisesAsync(performedExercises, restoreIndex: true);

        if (IsResting)
        {
            var remainingSeconds = (int)Math.Ceiling(_timer.RemainingUntil(_breakEndTimeUtc).TotalSeconds);
            _restDurationSeconds = Math.Max(CurrentExercise.BreakTimeInSeconds, remainingSeconds);
            RestTotalText = string.Format(UiText.RestTotalFormat, _restDurationSeconds);
            UpdateRestCountdown();
        }

        _timer.Start();
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
            ExerciseProgressText = string.Empty;
            SetProgressText = string.Empty;
        }
    }

    private async Task AdvanceToExerciseAsync(int index)
    {
        if (index < 0 || index >= Exercises.Count)
            return;

        _currentExerciseIndex = index;
        var exercise = Exercises[index];
        CurrentExercise = exercise;
        _totalSetsForExercise = exercise.PlannedSets;

        HasPreviousExercise = index > 0;
        HasNextExercise = index < Exercises.Count - 1;

        ExerciseProgressText = string.Format(UiText.ExerciseProgressFormat, index + 1, Exercises.Count);

        var lastSessionSets = await _setRepository.GetLastSessionSetsAsync(exercise.ExerciseId, _sessionId);
        HasLastSession = lastSessionSets.Count > 0;
        LastSessionResultsText = HasLastSession
            ? UiText.LastSessionPrefix + string.Join(UiText.ResultSeparator, lastSessionSets.Select(set => string.Format(UiText.SetResultFormat, set.Weight, set.Reps)))
            : UiText.FirstTimePerformingExercise;

        await LoadSetsForCurrentExerciseAsync();
        PrefillInputs(CurrentSets.LastOrDefault() ?? lastSessionSets.FirstOrDefault());
        UpdateSetProgress();
        await SaveStateAsync();
    }

    private void PrefillInputs(WorkoutSet? source)
    {
        WeightInput = source is null ? string.Empty : FormatWeight(source.Weight);
        RepsInput = source?.Reps.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private async Task LoadSetsForCurrentExerciseAsync()
    {
        CurrentSets.ReplaceAll(await _setRepository.GetForSessionExerciseAsync(CurrentExercise.Id));

        HasSavedSets = CurrentSets.Any();
    }

    private void UpdateSetProgress()
    {
        int currentSetNumber = CurrentSets.Count + 1;

        if (_totalSetsForExercise > 0)
        {
            SetProgressText = string.Format(UiText.SetProgressWithTotalFormat, currentSetNumber, _totalSetsForExercise);
            IsExerciseComplete = CurrentSets.Count >= _totalSetsForExercise;
            SetSegments.ReplaceAll(Enumerable.Range(1, _totalSetsForExercise)
                                             .Select(setNumber => new LevelSegment(setNumber, setNumber <= CurrentSets.Count)));
        }
        else
        {
            SetProgressText = string.Format(UiText.SetProgressFormat, currentSetNumber);
            IsExerciseComplete = false;
            SetSegments.Clear();
        }
    }

    [RelayCommand]
    private void IncreaseWeight() =>
        WeightInput = FormatWeight(ReadWeightOrZero() + DomainDefaults.WeightStepInKg);

    [RelayCommand]
    private void DecreaseWeight() =>
        WeightInput = FormatWeight(Math.Max(0, ReadWeightOrZero() - DomainDefaults.WeightStepInKg));

    [RelayCommand]
    private void IncreaseReps() =>
        RepsInput = (ReadRepsOrZero() + DomainDefaults.RepsStep).ToString(CultureInfo.InvariantCulture);

    [RelayCommand]
    private void DecreaseReps() =>
        RepsInput = Math.Max(0, ReadRepsOrZero() - DomainDefaults.RepsStep).ToString(CultureInfo.InvariantCulture);

    private double ReadWeightOrZero() => TryReadWeight(out double weight) ? weight : 0;

    private int ReadRepsOrZero() => TryReadReps(out int reps) ? reps : 0;

    private bool TryReadWeight(out double weight) =>
        double.TryParse(WeightInput, NumberStyles.Any, CultureInfo.InvariantCulture, out weight);

    private bool TryReadReps(out int reps) => int.TryParse(RepsInput, out reps);

    private static string FormatWeight(double weight) => weight.ToString(CultureInfo.InvariantCulture);

    [RelayCommand]
    private async Task SaveSetAsync()
    {
        if (!TryReadWeight(out double weight) || !TryReadReps(out int reps))
        {
            return;
        }

        var newSet = new WorkoutSet
        {
            SessionExerciseId = CurrentExercise.Id,
            Weight = weight,
            Reps = reps
        };

        await _setRepository.AddAsync(newSet);
        _haptics.Click();
        CurrentSets.Add(newSet);
        HasSavedSets = true;
        if (CurrentExercise.BreakTimeInSeconds > 0)
        {
            StartRest(CurrentExercise.BreakTimeInSeconds);
        }
        await SaveStateAsync();

        if (_totalSetsForExercise > 0 && CurrentSets.Count >= _totalSetsForExercise)
        {
            int nextIndex = _currentExerciseIndex + 1;
            if (nextIndex < Exercises.Count)
            {
                await Task.Delay(UiTiming.ExerciseAdvanceDelayMilliseconds);
                await AdvanceToExerciseAsync(nextIndex);
                return;
            }

            UpdateSetProgress();
            await CompleteWorkoutAsync();
            return;
        }

        UpdateSetProgress();
    }

    private void StartRest(int durationSeconds)
    {
        _restDurationSeconds = durationSeconds;
        _breakEndTimeUtc = DateTime.UtcNow.AddSeconds(durationSeconds);
        RestTotalText = string.Format(UiText.RestTotalFormat, durationSeconds);
        IsResting = true;
        UpdateRestCountdown();
    }

    private void UpdateRestCountdown()
    {
        var remaining = _timer.RemainingUntil(_breakEndTimeUtc);
        RestTimerText = _timer.FormatCountdown(remaining);
        RestProgress = _restDurationSeconds > 0
            ? Math.Clamp(remaining.TotalSeconds / _restDurationSeconds, 0, 1)
            : 0;
    }

    private async Task CompleteWorkoutAsync()
    {
        _timer.Stop();
        ClearRestState();
        _audioCues.Stop();
        TotalTimeText = _timer.ElapsedSince(_workoutStartTimeUtc);

        var summary = await _summaryService.BuildAsync([.. Exercises]);
        CompletedExercises.ReplaceAll(summary.Exercises);
        TotalVolume = summary.TotalVolume;

        await _sessionRepository.FinishAsync(_sessionId);
        IsWorkoutCompleted = true;
        IsWorkoutActive = false;
        await _activeStateRepository.ClearAsync();
    }

    private void ClearRestState()
    {
        IsResting = false;
        _breakEndTimeUtc = default;
        _restDurationSeconds = 0;
        RestProgress = 0;
        RestTimerText = _timer.FormatElapsed(TimeSpan.Zero);
    }

    [RelayCommand]
    private async Task ExtendRestAsync()
    {
        if (!IsResting) return;

        _restDurationSeconds += DomainDefaults.RestExtensionInSeconds;
        _breakEndTimeUtc = _breakEndTimeUtc.AddSeconds(DomainDefaults.RestExtensionInSeconds);
        RestTotalText = string.Format(UiText.RestTotalFormat, _restDurationSeconds);
        UpdateRestCountdown();
        await SaveStateAsync();
    }

    [RelayCommand]
    private async Task SkipRestAsync()
    {
        ClearRestState();

        _audioCues.Stop();
        await SaveStateAsync();
    }

    [RelayCommand]
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
    private async Task EditActionSetAsync()
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        var values = await _setEditService.PromptForSetAsync(UiText.TitleEditSet, set.Weight, set.Reps);
        if (values is null) return;

        set.Weight = values.Weight;
        set.Reps = values.Reps;

        await _setRepository.UpdateAsync(set);

        int index = CurrentSets.IndexOf(set);
        if (index >= 0)
        {
            CurrentSets[index] = set;
        }
    }

    [RelayCommand]
    private async Task DeleteActionSetAsync()
    {
        if (await DismissSetActionsAsync() is not { } set)
        {
            return;
        }

        if (!await _setEditService.ConfirmDeleteAsync()) return;

        await RemoveSetAsync(set);
    }

    private async Task<WorkoutSet?> DismissSetActionsAsync()
    {
        var set = ActionSet;
        CancelSetActions();
        await Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));
        return set;
    }

    private async Task RemoveSetAsync(WorkoutSet set)
    {
        await _setRepository.DeleteAsync(set.Id);
        await LoadSetsForCurrentExerciseAsync();

        UpdateSetProgress();
    }

    [RelayCommand]
    private async Task UndoLastSetAsync()
    {
        if (!CurrentSets.Any())
            return;

        await RemoveSetAsync(CurrentSets[^1]);
    }

    [RelayCommand]
    private async Task PreviousExerciseAsync()
    {
        if (HasPreviousExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex - 1);
        }
    }

    [RelayCommand]
    private async Task NextExerciseAsync()
    {
        if (HasNextExercise)
        {
            await AdvanceToExerciseAsync(_currentExerciseIndex + 1);
        }
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync()
    {
        bool isConfirmed = await Shell.Current.DisplayAlertAsync(
            UiText.TitleFinishWorkout,
            UiText.BodyFinishWorkoutConfirmation,
            UiText.ButtonFinish,
            UiText.ButtonCancel);

        if (!isConfirmed)
            return;

        await CompleteWorkoutAsync();
    }

    [RelayCommand]
    public async Task ResumeWorkoutAsync()
    {
        await Shell.Current.GoToAsync(NavigationRoutes.ActiveWorkoutOnWorkoutTab);
    }

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        if (IsSetActionSheetOpen)
        {
            CancelSetActions();
            return;
        }

        await ExitWorkoutAsync();
    }

    [RelayCommand]
    private async Task ExitWorkoutAsync()
    {
        await Shell.Current.GoToAsync(NavigationRoutes.GoBack);
        ClearCompletedSummary();
    }

    private void ClearCompletedSummary()
    {
        if (!IsWorkoutCompleted)
            return;

        IsWorkoutCompleted = false;
        CompletedExercises.Clear();
        TotalVolume = 0;
        TotalTimeText = _timer.FormatElapsed(TimeSpan.Zero);
    }

    public void Reset()
    {
        _timer.Stop();
        _audioCues.Stop();
        ClearRestState();
        CancelSetActions();
        _navigationStack.RemoveFromAllTabs<ActiveWorkoutPage>();

        _sessionId = 0;
        _currentExerciseIndex = 0;
        _totalSetsForExercise = 0;
        _workoutStartTimeUtc = default;

        IsWorkoutActive = false;
        IsWorkoutCompleted = false;
        IsExerciseComplete = false;
        IsExercisesEmpty = false;
        HasSavedSets = false;
        HasPreviousExercise = false;
        HasNextExercise = false;
        HasLastSession = false;

        Exercises.Clear();
        CurrentSets.Clear();
        SetSegments.Clear();
        CompletedExercises.Clear();
        CurrentExercise = new();

        WorkoutTitle = UiText.LoadingWorkoutTitle;
        TimerText = _timer.FormatElapsed(TimeSpan.Zero);
        TotalTimeText = _timer.FormatElapsed(TimeSpan.Zero);
        ExerciseProgressText = string.Empty;
        SetProgressText = string.Empty;
        LastSessionResultsText = string.Empty;
        RestTotalText = string.Empty;
        WeightInput = string.Empty;
        RepsInput = string.Empty;
        TotalVolume = 0;
    }

    public void TrackCurrentPage(Shell shell)
    {
        shell.Navigated += (_, _) => IsOnActiveWorkoutPage = shell.CurrentPage is ActiveWorkoutPage;
    }
}
