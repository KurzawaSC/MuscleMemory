using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;
using MuscleMemory.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

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

    // ID sesji — epoch timestamp ustawiany raz przy starcie treningu.
    // Izoluje serie bieżącej sesji od historycznych wpisów dla tego samego ćwiczenia.
    private int _sessionId;

    // Indeks aktualnie aktywnego ćwiczenia na liście Exercises
    private int _currentExerciseIndex;

    [ObservableProperty]
    public partial Workout CurrentWorkout { get; set; } = null!;

    [ObservableProperty]
    public partial string WorkoutTitle { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string TimerText { get; set; } = "00:00";

    public ObservableCollection<WorkoutExercise> Exercises { get; } = new();
    public ObservableCollection<WorkoutSet> CurrentSets { get; } = new();

    [ObservableProperty]
    public partial WorkoutExercise CurrentExercise { get; set; } = null!;

    [ObservableProperty]
    public partial string CurrentExerciseName { get; set; } = "Loading exercises...";

    // Progress labels
    [ObservableProperty]
    public partial string ExerciseProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SetProgressText { get; set; } = string.Empty;

    // Następny numer serii do zapisania (np. "Set 2 of 3")
    [ObservableProperty]
    public partial int CurrentSetNumber { get; set; } = 1;

    // Docelowa liczba serii dla bieżącego ćwiczenia
    [ObservableProperty]
    public partial int TotalSetsForExercise { get; set; } = 0;

    // Widoczność listy zapisanych serii (zamiast CollectionView.EmptyView, które jest błędne na Androidzie)
    [ObservableProperty]
    public partial bool HasSavedSets { get; set; } = false;

    // Widoczność komunikatu "Brak ćwiczeń"
    [ObservableProperty]
    public partial bool IsExercisesEmpty { get; set; } = false;

    [ObservableProperty]
    public partial bool HasPreviousExercise { get; set; } = false;

    [ObservableProperty]
    public partial bool HasNextExercise { get; set; } = false;

    [ObservableProperty]
    public partial bool IsExerciseComplete { get; set; } = false;

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
    public partial int RestSecondsRemaining { get; set; } = 0;

    [ObservableProperty]
    public partial string RestTimerText { get; set; } = "00:00";

    [ObservableProperty]
    public partial string WeightInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RepsInput { get; set; } = string.Empty;

    private IDispatcherTimer _timer;
    private Stopwatch _stopwatch = new Stopwatch();

    public ActiveWorkoutViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;

        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            TimerText = _stopwatch.Elapsed.ToString(@"mm\:ss");

            if (IsResting)
            {
                if (RestSecondsRemaining > 0)
                {
                    RestSecondsRemaining--;
                    TimeSpan ts = TimeSpan.FromSeconds(RestSecondsRemaining);
                    RestTimerText = ts.ToString(@"mm\:ss");
                }
                else
                {
                    IsResting = false;
                }
            }
        };
    }

    async partial void OnCurrentWorkoutChanged(Workout value)
    {
        if (value != null)
        {
            WorkoutTitle = value.Name;

            // Tworzymy nową sesję treningową w bazie (zapisuje StartTime) i pobieramy jej ID
            _sessionId = await _dbContext.CreateWorkoutSessionAsync(value.Id);

            _stopwatch.Start();
            _timer.Start();

            await LoadExercisesAsync(value.Id);
        }
    }

    private async Task LoadExercisesAsync(int workoutId)
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
            _currentExerciseIndex = 0;
            await AdvanceToExerciseAsync(0);
        }
        else
        {
            IsExercisesEmpty = true;
            CurrentExerciseName = "No exercises added!";
            ExerciseProgressText = string.Empty;
            SetProgressText = string.Empty;
        }
    }

    /// <summary>
    /// Przechodzi do ćwiczenia o podanym indeksie i aktualizuje stan UI.
    /// </summary>
    private async Task AdvanceToExerciseAsync(int index)
    {
        if (index < 0 || index >= Exercises.Count)
            return;

        _currentExerciseIndex = index;
        var exercise = Exercises[index];

        // Zaktualizuj stan bez wywoływania OnCurrentExerciseChanged (które czyści inputy)
        CurrentExercise = exercise;
        CurrentExerciseName = exercise.ExerciseName;
        TotalSetsForExercise = exercise.Sets;

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
        CurrentSetNumber = CurrentSets.Count + 1;

        if (TotalSetsForExercise > 0)
        {
            SetProgressText = $"Set {CurrentSetNumber} of {TotalSetsForExercise}";
            IsExerciseComplete = CurrentSets.Count >= TotalSetsForExercise;
        }
        else
        {
            SetProgressText = $"Set {CurrentSetNumber}";
            IsExerciseComplete = false;
        }
    }

    [RelayCommand]
    private async Task SaveSetAsync()
    {
        if (CurrentExercise == null)
        {
            await Shell.Current.DisplayAlert("Error", "No exercise selected.", "OK");
            return;
        }

        if (!double.TryParse(WeightInput, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double weight)
            || !int.TryParse(RepsInput, out int reps))
        {
            await Shell.Current.DisplayAlert("Invalid Input", "Please enter valid numbers for weight and reps.", "OK");
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

        // Wyczyść tylko powtórzenia — waga często zostaje ta sama
        RepsInput = string.Empty;

        // Odtwórz rest timer jeśli jest zdefiniowany break time
        if (CurrentExercise.BreakTimeInSeconds > 0)
        {
            RestSecondsRemaining = CurrentExercise.BreakTimeInSeconds;
            TimeSpan ts = TimeSpan.FromSeconds(RestSecondsRemaining);
            RestTimerText = ts.ToString(@"mm\:ss");
            IsResting = true;
        }

        // Auto-advance logic
        if (TotalSetsForExercise > 0 && CurrentSets.Count >= TotalSetsForExercise)
        {
            int nextIndex = _currentExerciseIndex + 1;
            if (nextIndex < Exercises.Count)
            {
                // Krótkie opóźnienie dla lepszego UX — użytkownik widzi zapisaną serię
                await Task.Delay(400);
                await AdvanceToExerciseAsync(nextIndex);
                return;
            }
            else
            {
                // Ostatnia seria ostatniego ćwiczenia
                IsResting = false; // Brak przerwy po ostatnim ćwiczeniu
                UpdateSetProgress();
                _stopwatch.Stop();
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
                return;
            }
        }

        UpdateSetProgress();
    }

    [RelayCommand]
    private void SkipRest()
    {
        IsResting = false;
        RestSecondsRemaining = 0;
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;

        await _dbContext.DeleteSetAsync(set.Id);
        CurrentSets.Remove(set);
        HasSavedSets = CurrentSets.Any();

        // Przenumeruj pozostałe serie w UI i DB
        for (int i = 0; i < CurrentSets.Count; i++)
        {
            CurrentSets[i].SetNumber = i + 1;
        }

        UpdateSetProgress();
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
        _stopwatch.Stop();
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

        // Zapisz czas zakończenia w bazie
        await _dbContext.FinishWorkoutSessionAsync(_sessionId);
        
        IsWorkoutCompleted = true;
    }

    [RelayCommand]
    private async Task ExitWorkoutAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}