using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class AddEditWorkoutViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    // Pole na nazwę tworzonego treningu (z powiązaniem do XAML)
    [ObservableProperty]
    public partial string WorkoutName { get; set; } = string.Empty;

    // Tymczasowa lista ćwiczeń dla tego treningu (będzie odświeżać UI na żywo)
    public ObservableCollection<WorkoutExercise> Exercises { get; set; } = new();

    public AddEditWorkoutViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
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

        // 2. Przygotowanie głównego obiektu Treningu
        var newWorkout = new Workout
        {
            Name = WorkoutName.Trim()
        };

        // 3. Wysłanie całości do naszej transakcyjnej metody w bazie SQLite
        await _dbContext.SaveFullWorkoutAsync(newWorkout, Exercises.ToList());

        // 4. Sukces! Zamykamy ekran kreatora i wracamy do Listy Treningów
        await Shell.Current.GoToAsync("..");
    }
    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await _dbContext.GetExercisesAsync();
    }
}