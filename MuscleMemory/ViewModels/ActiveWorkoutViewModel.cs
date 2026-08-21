using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Plugin.Maui.Audio;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class ActiveWorkoutViewModel : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IWorkoutSessionRepository _sessionRepository;
    private readonly ISessionExerciseRepository _sessionExerciseRepository;
    private readonly IWorkoutSetRepository _setRepository;
    private readonly IActiveWorkoutStateRepository _activeStateRepository;
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

    private IDispatcherTimer _timer;

    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _audioPlayer;

    public ActiveWorkoutViewModel(
        IWorkoutRepository workoutRepository,
        IWorkoutSessionRepository sessionRepository,
        ISessionExerciseRepository sessionExerciseRepository,
        IWorkoutSetRepository setRepository,
        IActiveWorkoutStateRepository activeStateRepository,
        IAudioManager audioManager)
    {
        _workoutRepository = workoutRepository;
        _sessionRepository = sessionRepository;
        _sessionExerciseRepository = sessionExerciseRepository;
        _setRepository = setRepository;
        _activeStateRepository = activeStateRepository;
        _audioManager = audioManager;

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            if (IsWorkoutActive)
            {
                TimerText = FormatElapsed(DateTime.UtcNow - _workoutStartTimeUtc);
            }

            if (IsResting)
            {
                var remaining = _breakEndTimeUtc - DateTime.UtcNow;
                if (remaining.TotalSeconds > 0)
                {
                    RestTimerText = remaining.ToString(UiText.ElapsedFormat);
                }
                else
                {
                    ClearRestState();
                    _ = PlayBreakEndSoundAsync();
                    _ = SaveStateAsync();
                }
            }
        };
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.ToString(elapsed.TotalHours >= 1 ? UiText.ElapsedWithHoursFormat : UiText.ElapsedFormat);

    private async Task PlayBreakEndSoundAsync()
    {
        try
        {
            var audioStream = await FileSystem.OpenAppPackageFileAsync("BreakEnd.mp3");
            _audioPlayer?.Dispose();
            _audioPlayer = _audioManager.CreatePlayer(audioStream);
            _audioPlayer.Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to play break sound: {ex.Message}");
        }
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
        Exercises.Clear();
        foreach (var performedExercise in performedExercises)
        {
            Exercises.Add(performedExercise);
        }

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
        var setsFromDb = await _setRepository.GetForSessionExerciseAsync(CurrentExercise.Id);
        CurrentSets.Clear();
        foreach (var set in setsFromDb)
        {
            CurrentSets.Add(set);
        }

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
        TotalTimeText = FormatElapsed(DateTime.UtcNow - _workoutStartTimeUtc);

        double volume = 0;
        CompletedExercises.Clear();

        foreach (var performedExercise in Exercises)
        {
            var sets = await _setRepository.GetForSessionExerciseAsync(performedExercise.Id);
            if (sets.Any())
            {
                foreach (var set in sets) volume += (set.Weight * set.Reps);

                CompletedExercises.Add(new CompletedExerciseSummary(performedExercise.ExerciseName, sets));
            }
        }

        TotalVolume = volume;

        await _sessionRepository.FinishAsync(_sessionId);
        IsWorkoutCompleted = true;
        IsWorkoutActive = false;
        await _activeStateRepository.ClearAsync();
    }

    private void ClearRestState()
    {
        IsResting = false;
        _breakEndTimeUtc = default;
        RestTimerText = FormatElapsed(TimeSpan.Zero);
    }

    [RelayCommand]
    private async Task SkipRestAsync()
    {
        ClearRestState();

        _audioPlayer?.Dispose();
        _audioPlayer = null;
        await SaveStateAsync();
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;

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

        string weightStr = await Shell.Current.DisplayPromptAsync(UiText.TitleEditSet, UiText.PromptEnterWeightKg, initialValue: set.Weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;

        string repsStr = await Shell.Current.DisplayPromptAsync(UiText.TitleEditSet, UiText.PromptEnterReps, initialValue: set.Reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            set.Weight = newWeight;
            set.Reps = newReps;

            await _setRepository.UpdateAsync(set);

            int index = CurrentSets.IndexOf(set);
            if (index >= 0)
            {
                CurrentSets[index] = set;
            }
        }
    }

    [RelayCommand]
    private async Task UndoLastSetAsync()
    {
        if (!CurrentSets.Any())
            return;

        var lastSet = CurrentSets.OrderByDescending(s => s.SetNumber).First();
        await DeleteSetAsync(lastSet);
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

        _audioPlayer?.Dispose();
        _audioPlayer = null;

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
        TotalTimeText = FormatElapsed(TimeSpan.Zero);
    }

    public void TrackCurrentPage(Shell shell)
    {
        shell.Navigated += (_, _) => IsOnActiveWorkoutPage = shell.CurrentPage is ActiveWorkoutPage;
    }
}
