using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

[QueryProperty(nameof(WorkoutToEdit), "WorkoutToEdit")]
public partial class AddEditWorkoutViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    // Pole na nazwę tworzonego treningu (z powiązaniem do XAML)
    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;
    
    partial void OnWorkoutNameChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; } = false;

    // Tymczasowa lista ćwiczeń dla tego treningu (będzie odświeżać UI na żywo)
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

    // Funkcja wywoływana z Code-Behind po tym, jak użytkownik skonfiguruje ćwiczenie w Pop-upie
    public void AddExerciseToWorkout(Exercise selectedExercise, int sets, int reps, int breakTime)
    {
        var newWorkoutExercise = new WorkoutExercise
        {
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name, // Nasze zignorowane przez SQL pole dla widoku
            Sets = sets,
            Reps = reps,
            BreakTimeInSeconds = breakTime
        };

        Exercises.Add(newWorkoutExercise);
        IsEmpty = !Exercises.Any();
        HasUnsavedChanges = true;
    }

    public void UpdateExerciseInWorkout(WorkoutExercise exercise, int sets, int reps, int breakTime)
    {
        var index = Exercises.IndexOf(exercise);
        if (index >= 0)
        {
            exercise.Sets = sets;
            exercise.Reps = reps;
            exercise.BreakTimeInSeconds = breakTime;
            
            // Wymuś odświeżenie UI poprzez podmianę
            Exercises[index] = exercise;
            HasUnsavedChanges = true;
        }
    }

    // Komenda do usuwania ćwiczenia z tymczasowej listy (ikona "X" lub Swipe)
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

    // Zapis całości do bazy danych
    [RelayCommand]
    private async Task SaveWorkoutAsync()
    {
        // 1. Podstawowa walidacja
        if (string.IsNullOrWhiteSpace(WorkoutName))
        {
            await Shell.Current.DisplayAlert("Hold on!", "Please enter a workout name.", "OK");
            return;
        }

        if (!Exercises.Any())
        {
            await Shell.Current.DisplayAlert("Hold on!", "Add at least one exercise to your workout.", "OK");
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
        
        // 4. Sukces! Zamykamy ekran kreatora i wracamy do Listy Treningów
        await Shell.Current.GoToAsync("..");
    }
    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await _dbContext.GetExercisesAsync();
    }
}