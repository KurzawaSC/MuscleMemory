using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(WorkoutToEdit), QueryKeys.WorkoutToEdit)]
public partial class AddEditWorkoutViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;
    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;
    
    partial void OnWorkoutNameChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; } = false;
    public ObservableCollection<WorkoutExercise> Exercises { get; set; } = new();

    public AddEditWorkoutViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    [ObservableProperty]
    public partial Workout WorkoutToEdit { get; set; } = null!;

    async partial void OnWorkoutToEditChanged(Workout value)
    {
        if (value != null)
        {
            WorkoutName = value.Name;
            var exercisesFromDb = await _dbContext.GetExercisesForWorkoutAsync(value.Id);
            Exercises.Clear();
            foreach (var ex in exercisesFromDb)
            {
                Exercises.Add(ex);
            }
            IsEmpty = !Exercises.Any();
            HasUnsavedChanges = false;
        }
    }
    public void AddExerciseToWorkout(Exercise selectedExercise, int sets, int reps, int breakTime, int targetRPE)
    {
        var newWorkoutExercise = new WorkoutExercise
        {
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = sets,
            Reps = reps,
            BreakTimeInSeconds = breakTime,
            TargetRPE = targetRPE
        };

        Exercises.Add(newWorkoutExercise);
        IsEmpty = !Exercises.Any();
        HasUnsavedChanges = true;
    }

    public void UpdateExerciseInWorkout(WorkoutExercise exercise, int sets, int reps, int breakTime, int targetRPE)
    {
        var index = Exercises.IndexOf(exercise);
        if (index >= 0)
        {
            exercise.Sets = sets;
            exercise.Reps = reps;
            exercise.BreakTimeInSeconds = breakTime;
            exercise.TargetRPE = targetRPE;
            Exercises[index] = exercise;
            HasUnsavedChanges = true;
        }
    }
    [RelayCommand]
    private void RemoveExercise(WorkoutExercise exerciseToRemove)
    {
        if (Exercises.Contains(exerciseToRemove))
        {
            Exercises.Remove(exerciseToRemove);
        }
        IsEmpty = !Exercises.Any();
        HasUnsavedChanges = true;
    }
    [RelayCommand]
    private async Task SaveWorkoutAsync()
    {
        if (string.IsNullOrWhiteSpace(WorkoutName))
        {
            await Shell.Current.DisplayAlertAsync("Hold on!", "Please enter a workout name.", "OK");
            return;
        }

        if (!Exercises.Any())
        {
            await Shell.Current.DisplayAlertAsync("Hold on!", "Add at least one exercise to your workout.", "OK");
            return;
        }

        if (WorkoutToEdit != null)
        {
            WorkoutToEdit.Name = WorkoutName.Trim();
            await _dbContext.UpdateFullWorkoutAsync(WorkoutToEdit, Exercises.ToList());
        }
        else
        {
            var newWorkout = new Workout
            {
                Name = WorkoutName.Trim()
            };
            await _dbContext.SaveFullWorkoutAsync(newWorkout, Exercises.ToList());
        }

        HasUnsavedChanges = false;
        await Shell.Current.GoToAsync(NavigationRoutes.GoBack);
    }
    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await _dbContext.GetExercisesAsync();
    }
}
