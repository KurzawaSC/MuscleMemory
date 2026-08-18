using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(WorkoutId), QueryKeys.WorkoutId)]
[QueryProperty(nameof(WorkoutName), QueryKeys.WorkoutName)]
public partial class WorkoutHistoryViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial int WorkoutId { get; set; }

    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<WorkoutHistorySession> Sessions { get; set; } = new();

    public WorkoutHistoryViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    async partial void OnWorkoutIdChanged(int value)
    {
        if (value > 0)
        {
            await LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        var history = await _dbContext.GetWorkoutHistoryAsync(WorkoutId);
        Sessions.Clear();
        foreach (var session in history)
        {
            Sessions.Add(session);
        }
        IsEmpty = !Sessions.Any();
    }

    [RelayCommand]
    private async Task EditSetAsync(WorkoutSet set)
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
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSetAsync(WorkoutSet set)
    {
        if (set == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Delete Set", "Are you sure you want to delete this set?", "Yes", "No");
        if (!confirm) return;

        await _dbContext.DeleteSetAsync(set.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddSetAsync(WorkoutHistoryExercise parentExercise)
    {
        if (parentExercise == null) return;

        var lastSet = parentExercise.Sets.LastOrDefault();
        double weight = lastSet?.Weight ?? 0;
        int reps = lastSet?.Reps ?? 0;

        string weightStr = await Shell.Current.DisplayPromptAsync("Add Set", "Enter weight (kg):", initialValue: weight.ToString(), keyboard: Keyboard.Numeric);
        if (weightStr == null) return;
        
        string repsStr = await Shell.Current.DisplayPromptAsync("Add Set", "Enter reps:", initialValue: reps.ToString(), keyboard: Keyboard.Numeric);
        if (repsStr == null) return;

        if (double.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double newWeight) && int.TryParse(repsStr, out int newReps))
        {
            var newSet = new WorkoutSet
            {
                WorkoutExerciseId = parentExercise.WorkoutExerciseId,
                WorkoutSessionId = parentExercise.WorkoutSessionId,
                SetNumber = parentExercise.Sets.Count + 1,
                Weight = newWeight,
                Reps = newReps
            };

            await _dbContext.SaveSetAsync(newSet);
            await LoadHistoryAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteExerciseAsync(WorkoutHistoryExercise exercise)
    {
        if (exercise == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Delete Exercise", $"Are you sure you want to remove '{exercise.ExerciseName}'?", "Yes", "No");
        if (!confirm) return;

        await _dbContext.DeleteLoggedExerciseAsync(exercise.WorkoutExerciseId, exercise.WorkoutSessionId);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task AddExerciseAsync(WorkoutHistorySession session)
    {
        if (session == null) return;
        
        var allExercises = await _dbContext.GetExercisesAsync();
        if (!allExercises.Any())
        {
            await Shell.Current.DisplayAlert("No Exercises", "You must create an exercise in the library first.", "OK");
            return;
        }

        var exerciseNames = allExercises.Select(e => e.Name).ToArray();
        string selectedName = await Shell.Current.DisplayActionSheet("Select Exercise", "Cancel", null, exerciseNames);

        if (string.IsNullOrEmpty(selectedName) || selectedName == "Cancel")
            return;

        var selectedExercise = allExercises.First(e => e.Name == selectedName);

        var newWorkoutExercise = new WorkoutExercise
        {
            WorkoutId = this.WorkoutId,
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = 3,
            Reps = 10,
            BreakTimeInSeconds = 60,
            TargetRPE = 8
        };
        
        int workoutExerciseId = await _dbContext.AddLoggedExerciseAsync(newWorkoutExercise);
        
        var newSet = new WorkoutSet
        {
            WorkoutExerciseId = workoutExerciseId,
            WorkoutSessionId = session.SessionId,
            SetNumber = 1,
            Weight = 0,
            Reps = 0
        };
        await _dbContext.SaveSetAsync(newSet);

        await LoadHistoryAsync();
    }
}
