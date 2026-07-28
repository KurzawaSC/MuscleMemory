using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;
using MuscleMemory.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(CurrentWorkout), "Workout")]
public partial class ActiveWorkoutViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    // Dodanie '= null!' mówi systemowi: "Spokojnie, to zostanie uzupełnione później"
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

    // Inicjalizacja pustymi stringami
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
        };
    }

    // USUNIĘTO znak '?' przy słowie Workout. 
    // Teraz idealnie zgadza się to z wymogami generatora!
    async partial void OnCurrentWorkoutChanged(Workout value)
    {
        if (value != null)
        {
            WorkoutTitle = value.Name;
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
            CurrentExercise = Exercises.First();
        }
        else
        {
            CurrentExerciseName = "No exercises added!";
        }
    }

    // USUNIĘTO znak '?' przy słowie WorkoutExercise.
    async partial void OnCurrentExerciseChanged(WorkoutExercise value)
    {
        if (value != null)
        {
            CurrentExerciseName = value.ExerciseName;
            WeightInput = string.Empty;
            RepsInput = string.Empty;

            await LoadSetsForCurrentExerciseAsync();
        }
    }

    private async Task LoadSetsForCurrentExerciseAsync()
    {
        if (CurrentExercise == null) return;

        var setsFromDb = await _dbContext.GetSetsForWorkoutExerciseAsync(CurrentExercise.Id);
        CurrentSets.Clear();
        foreach (var set in setsFromDb)
        {
            CurrentSets.Add(set);
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

        if (!double.TryParse(WeightInput, out double weight) || !int.TryParse(RepsInput, out int reps))
        {
            await Shell.Current.DisplayAlert("Invalid Input", "Please enter valid numbers for weight and reps.", "OK");
            return;
        }

        var newSet = new WorkoutSet
        {
            WorkoutExerciseId = CurrentExercise.Id,
            Weight = weight,
            Reps = reps,
            SetNumber = CurrentSets.Count + 1
        };

        await _dbContext.SaveSetAsync(newSet);
        CurrentSets.Add(newSet);

        RepsInput = string.Empty;
    }

    [RelayCommand]
    private async Task FinishWorkoutAsync()
    {
        _stopwatch.Stop();
        _timer.Stop();
        await Shell.Current.GoToAsync("..");
    }
}