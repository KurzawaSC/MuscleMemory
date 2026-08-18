using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;
using MuscleMemory.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Plugin.Maui.Audio;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public class ExerciseBestSet
{
    public string ExerciseName { get; set; } = string.Empty;
    public string BestSetText { get; set; } = string.Empty;
}

[QueryProperty(nameof(CurrentWorkout), "Workout")]
public partial class ActiveWorkoutViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;
    private int _sessionId;
    private int _currentExerciseIndex;
    private int _totalSetsForExercise;
    private DateTime _workoutStartTime;
    private DateTime _breakEndTime;

    public static ActiveWorkoutViewModel? Current { get; private set; }

    [ObservableProperty]
    public partial Workout CurrentWorkout { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    public partial bool IsWorkoutActive { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBannerVisible))]
    public partial bool IsOnActiveWorkoutPage { get; set; } = false;

    public bool IsBannerVisible => IsWorkoutActive && !IsOnActiveWorkoutPage;

    [ObservableProperty]
    public partial string WorkoutTitle { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string TimerText { get; set; } = "00:00";

    public ObservableCollection<WorkoutExercise> Exercises { get; } = new();
    public ObservableCollection<WorkoutSet> CurrentSets { get; } = new();

    [ObservableProperty]
    public partial WorkoutExercise CurrentExercise { get; set; } = new();

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

    public ObservableCollection<ExerciseBestSet> BestSets { get; } = new();

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

    public ActiveWorkoutViewModel(DatabaseContext dbContext, IAudioManager audioManager)
    {
        Current = this;
        _dbContext = dbContext;
        _audioManager = audioManager;

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            if (IsWorkoutActive)
            {
                TimerText = (DateTime.Now - _workoutStartTime).ToString(@"mm\:ss");
            }

            if (IsResting)
            {
                var remaining = _breakEndTime - DateTime.Now;
                if (remaining.TotalSeconds > 0)
                {
                    RestTimerText = remaining.ToString(@"mm\:ss");
                }
                else
                {
                    IsResting = false;
                    _ = PlayBreakEndSoundAsync();
                    _ = SaveStateAsync();
                }
            }
        };
    }

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

    async partial void OnCurrentWorkoutChanged(Workout value)
    {
        if (value != null && !IsWorkoutActive) // Only start if not already active (resuming)
        {
            _workoutStartTime = DateTime.Now;
            IsWorkoutCompleted = false;
            
            WorkoutTitle = value.Name;
            _sessionId = await _dbContext.CreateWorkoutSessionAsync(value.Id);

            _timer.Start();

            await LoadExercisesAsync(value.Id, restoreIndex: false);
            
            await Task.Delay(300); // Wait for navigation animation to finish
            IsWorkoutActive = true;
            await SaveStateAsync();
        }
    }

    private async Task SaveStateAsync()
    {
        if (!IsWorkoutActive) return;
        var state = new ActiveWorkoutState
        {
            WorkoutId = CurrentWorkout?.Id ?? 0,
            SessionId = _sessionId,
            StartTime = _workoutStartTime,
            CurrentExerciseIndex = _currentExerciseIndex,
            IsResting = IsResting,
            BreakEndTime = _breakEndTime
        };
        await _dbContext.SaveActiveWorkoutStateAsync(state);
    }

    public async Task LoadStateAsync()
    {
        var state = await _dbContext.GetActiveWorkoutStateAsync();
        if (state != null)
        {
            _sessionId = state.SessionId;
            _workoutStartTime = state.StartTime;
            _currentExerciseIndex = state.CurrentExerciseIndex;
            IsResting = state.IsResting;
            _breakEndTime = state.BreakEndTime;
            IsWorkoutActive = true;
            IsWorkoutCompleted = false;
            
            var workout = (await _dbContext.GetWorkoutsAsync()).FirstOrDefault(w => w.Id == state.WorkoutId);
            if (workout != null)
            {
                CurrentWorkout = workout;
                WorkoutTitle = workout.Name;
                await LoadExercisesAsync(workout.Id, restoreIndex: true);
                _timer.Start();
            }
        }
    }

    private async Task LoadExercisesAsync(int workoutId, bool restoreIndex = false)
    {
        var exercisesFromDb = await _dbContext.GetExercisesForWorkoutAsync(workoutId);

        Exercises.Clear();
        foreach (var ex in exercisesFromDb)
        {
            Exercises.Add(ex);
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
        _totalSetsForExercise = exercise.Sets;

        HasPreviousExercise = index > 0;
        HasNextExercise = index < Exercises.Count - 1;

        ExerciseProgressText = $"Exercise {index + 1} of {Exercises.Count}: {exercise.ExerciseName}";

        WeightInput = string.Empty;
        RepsInput = string.Empty;

        var lastSessionSets = await _dbContext.GetLastSessionSetsForExerciseAsync(exercise.Id, _sessionId);
        if (lastSessionSets.Any())
        {
            var setStrings = lastSessionSets.Select(s => $"{s.Weight} kg × {s.Reps}");
            LastSessionResultsText = "Last Session: " + string.Join(", ", setStrings);
        }
        else
        {
            LastSessionResultsText = "First time performing this exercise!";
        }

        await LoadSetsForCurrentExerciseAsync();
        UpdateSetProgress();
        await SaveStateAsync();
    }

    private async Task LoadSetsForCurrentExerciseAsync()
    {
        if (CurrentExercise == null) return;

        var setsFromDb = await _dbContext.GetSetsForWorkoutExerciseAsync(CurrentExercise.Id, _sessionId);
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
            SetProgressText = $"Set {currentSetNumber} of {_totalSetsForExercise}";
            IsExerciseComplete = CurrentSets.Count >= _totalSetsForExercise;
        }
        else
        {
            SetProgressText = $"Set {currentSetNumber}";
            IsExerciseComplete = false;
        }
    }

    [RelayCommand]
    private async Task SaveSetAsync()
    {
        if (CurrentExercise == null)
        {
            await Shell.Current.DisplayAlertAsync("Error", "No exercise selected.", "OK");
            return;
        }

        if (!double.TryParse(WeightInput, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double weight)
            || !int.TryParse(RepsInput, out int reps))
        {
            await Shell.Current.DisplayAlertAsync("Invalid Input", "Please enter valid numbers for weight and reps.", "OK");
            return;
        }

        var newSet = new WorkoutSet
        {
            WorkoutExerciseId = CurrentExercise.Id,
            WorkoutSessionId = _sessionId,
            Weight = weight,
            Reps = reps,
            SetNumber = CurrentSets.Count + 1
        };

        await _dbContext.SaveSetAsync(newSet);
        CurrentSets.Add(newSet);
        HasSavedSets = true;
        RepsInput = string.Empty;
        if (CurrentExercise.BreakTimeInSeconds > 0)
        {
            _breakEndTime = DateTime.Now.AddSeconds(CurrentExercise.BreakTimeInSeconds);
            IsResting = true;
        }
        await SaveStateAsync();
        
        if (_totalSetsForExercise > 0 && CurrentSets.Count >= _totalSetsForExercise)
        {
            int nextIndex = _currentExerciseIndex + 1;
            if (nextIndex < Exercises.Count)
            {
                await Task.Delay(400);
                await AdvanceToExerciseAsync(nextIndex);
                return;
            }
            else
            {
                IsResting = false;
                UpdateSetProgress();
                _timer.Stop();
                
                double volume = 0;
                BestSets.Clear();

                foreach (var ex in Exercises)
                {
                    var sets = await _dbContext.GetSetsForWorkoutExerciseAsync(ex.Id, _sessionId);
                    if (sets.Any())
                    {
                        foreach (var s in sets) volume += (s.Weight * s.Reps);

                        var bestSet = sets.OrderByDescending(s => s.Weight).ThenByDescending(s => s.Reps).First();
                        BestSets.Add(new ExerciseBestSet
                        {
                            ExerciseName = ex.ExerciseName,
                            BestSetText = $"{bestSet.Weight} kg × {bestSet.Reps} reps"
                        });
                    }
                }
                
                TotalVolume = volume;
                
                await _dbContext.FinishWorkoutSessionAsync(_sessionId);
                IsWorkoutCompleted = true;
                IsWorkoutActive = false;
                await _dbContext.ClearActiveWorkoutStateAsync();
                return;
            }
        }

        UpdateSetProgress();
    }

    [RelayCommand]
    private async Task SkipRestAsync()
    {
        IsResting = false;
        
        _audioPlayer?.Dispose();
        _audioPlayer = null;
        await SaveStateAsync();
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;

        await _dbContext.DeleteSetAsync(set.Id);
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
        
        string weightStr = await Shell.Current.DisplayPromptAsync("Edit Set", "Enter weight (kg):", initialValue: set.Weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;
        
        string repsStr = await Shell.Current.DisplayPromptAsync("Edit Set", "Enter reps:", initialValue: set.Reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            set.Weight = newWeight;
            set.Reps = newReps;
            
            await _dbContext.UpdateSetAsync(set);
            
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
            "Finish Workout", 
            "Are you sure you want to finish and save this workout?", 
            "Finish", 
            "Cancel");
            
        if (!isConfirmed)
            return;

        _audioPlayer?.Dispose();
        _audioPlayer = null;
        
        _timer.Stop();
        
        double volume = 0;
        BestSets.Clear();

        foreach (var ex in Exercises)
        {
            var sets = await _dbContext.GetSetsForWorkoutExerciseAsync(ex.Id, _sessionId);
            if (sets.Any())
            {
                foreach (var s in sets) volume += (s.Weight * s.Reps);

                var bestSet = sets.OrderByDescending(s => s.Weight).ThenByDescending(s => s.Reps).First();
                BestSets.Add(new ExerciseBestSet
                {
                    ExerciseName = ex.ExerciseName,
                    BestSetText = $"{bestSet.Weight} kg × {bestSet.Reps} reps"
                });
            }
        }
        
        TotalVolume = volume;
        await _dbContext.FinishWorkoutSessionAsync(_sessionId);
        
        IsWorkoutCompleted = true;
        IsWorkoutActive = false;
        await _dbContext.ClearActiveWorkoutStateAsync();
        
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ResumeWorkoutAsync()
    {
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage));
    }

    [RelayCommand]
    private async Task ExitWorkoutAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
