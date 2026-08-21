using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using System.Collections.ObjectModel;
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
    private int _sessionId;
    private int _currentExerciseIndex;
    private int _totalSetsForExercise;
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

    [ObservableProperty]
    public partial SessionExercise CurrentExercise { get; set; } = new();

    [ObservableProperty]
    public partial string ExerciseProgressText { get; set; } = string.Empty;

    [ObservableProperty]
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
    [NotifyPropertyChangedFor(nameof(IsSetCounterVisible))]
    public partial bool IsExerciseComplete { get; set; } = false;

    public bool IsSetCounterVisible => !IsExerciseComplete;

    [ObservableProperty]
    public partial bool IsResting { get; set; } = false;

    [ObservableProperty]
    public partial bool IsWorkoutCompleted { get; set; } = false;

    [ObservableProperty]
    public partial double TotalVolume { get; set; } = 0;

    public ObservableCollection<CompletedExerciseSummary> CompletedExercises { get; } = [];

    [ObservableProperty]
    public partial string LastSessionResultsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RestTimerText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string WeightInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RepsInput { get; set; } = string.Empty;

    public ActiveWorkoutViewModel(
        IWorkoutRepository workoutRepository,
        IWorkoutSessionRepository sessionRepository,
        ISessionExerciseRepository sessionExerciseRepository,
        IWorkoutSetRepository setRepository,
        IActiveWorkoutStateRepository activeStateRepository,
        IWorkoutTimerService timer,
        IAudioCueService audioCues,
        ISetEditService setEditService,
        IWorkoutSummaryService summaryService)
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

        _timer.Ticked += OnTimerTicked;
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

        var remaining = _timer.RemainingUntil(_breakEndTimeUtc);
        if (remaining.TotalSeconds > 0)
        {
            RestTimerText = _timer.FormatCountdown(remaining);
            return;
        }

        ClearRestState();
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

        _timer.Start();
    }

    private async Task ShowExercisesAsync(List<SessionExercise> performedExercises, bool restoreIndex)
    {
        Exercises.ReplaceAll(performedExercises);

        if (Exercises.Any())
        {
            IsExercisesEmpty = false;
            if (!restoreIndex) _currentExerciseIndex = 0;
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

        ExerciseProgressText = string.Format(UiText.ExerciseProgressFormat, index + 1, Exercises.Count, exercise.ExerciseName);

        WeightInput = string.Empty;
        RepsInput = string.Empty;

        var lastSessionSets = await _setRepository.GetLastSessionSetsAsync(exercise.ExerciseId, _sessionId);
        if (lastSessionSets.Any())
        {
            var setStrings = lastSessionSets.Select(s => $"{s.Weight}{UiText.KgTimesSeparator}{s.Reps}");
            LastSessionResultsText = UiText.LastSessionPrefix + string.Join(", ", setStrings);
        }
        else
        {
            LastSessionResultsText = UiText.FirstTimePerformingExercise;
        }

        await LoadSetsForCurrentExerciseAsync();
        UpdateSetProgress();
        await SaveStateAsync();
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
        }
        else
        {
            SetProgressText = string.Format(UiText.SetProgressFormat, currentSetNumber);
            IsExerciseComplete = false;
        }
    }

    [RelayCommand]
    private async Task SaveSetAsync()
    {
        if (!double.TryParse(WeightInput, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double weight)
            || !int.TryParse(RepsInput, out int reps))
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleInvalidInput, UiText.BodyInvalidWeightReps, UiText.ButtonOk);
            return;
        }

        var newSet = new WorkoutSet
        {
            SessionExerciseId = CurrentExercise.Id,
            Weight = weight,
            Reps = reps,
            SetNumber = CurrentSets.Count + 1
        };

        await _setRepository.AddAsync(newSet);
        CurrentSets.Add(newSet);
        HasSavedSets = true;
        RepsInput = string.Empty;
        if (CurrentExercise.BreakTimeInSeconds > 0)
        {
            _breakEndTimeUtc = DateTime.UtcNow.AddSeconds(CurrentExercise.BreakTimeInSeconds);
            IsResting = true;
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
        RestTimerText = _timer.FormatElapsed(TimeSpan.Zero);
    }

    [RelayCommand]
    private async Task SkipRestAsync()
    {
        ClearRestState();

        _audioCues.Stop();
        await SaveStateAsync();
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;
        if (!await _setEditService.ConfirmDeleteAsync()) return;

        await RemoveSetAsync(set);
    }

    private async Task RemoveSetAsync(WorkoutSet set)
    {
        await _setRepository.DeleteAsync(set.Id);
        CurrentSets.Remove(set);
        HasSavedSets = CurrentSets.Any();
        for (int i = 0; i < CurrentSets.Count; i++)
        {
            CurrentSets[i].SetNumber = i + 1;
        }

        UpdateSetProgress();
    }

    [RelayCommand]
    private async Task EditLoggedSetAsync(WorkoutSet set)
    {
        if (set == null) return;

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
    private async Task UndoLastSetAsync()
    {
        if (!CurrentSets.Any())
            return;

        var lastSet = CurrentSets.OrderByDescending(s => s.SetNumber).First();
        await RemoveSetAsync(lastSet);
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
    private async Task ResumeWorkoutAsync()
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
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

        Exercises.Clear();
        CurrentSets.Clear();
        CompletedExercises.Clear();
        CurrentExercise = new();

        WorkoutTitle = UiText.LoadingWorkoutTitle;
        TimerText = _timer.FormatElapsed(TimeSpan.Zero);
        TotalTimeText = _timer.FormatElapsed(TimeSpan.Zero);
        ExerciseProgressText = string.Empty;
        SetProgressText = string.Empty;
        LastSessionResultsText = string.Empty;
        WeightInput = string.Empty;
        RepsInput = string.Empty;
        TotalVolume = 0;
    }

    public void TrackCurrentPage(Shell shell)
    {
        shell.Navigated += (_, _) => IsOnActiveWorkoutPage = shell.CurrentPage is ActiveWorkoutPage;
    }
}
