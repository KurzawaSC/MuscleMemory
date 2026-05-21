using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool IsNotEmpty { get; set; } = false;

    public ObservableCollection<Exercise> Exercises { get; set; } = new();

    public ExerciseListViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    [RelayCommand]
    public async Task LoadExercisesAsync()
    {
        var exercisesFromDb = await _dbContext.GetExercisesAsync();
        Exercises.Clear();
        foreach (var exercise in exercisesFromDb)
        {
            Exercises.Add(exercise);
        }

        // DODANE: Aktualizacja stanu
        IsEmpty = !Exercises.Any();
    }

    // Ta funkcja zajmie się zapisem
    public async Task SaveNewExerciseAsync(string exerciseName)
    {
        var newDoc = new Exercise { Name = exerciseName };

        // Zapis do SQLite
        await _dbContext.AddExerciseAsync(newDoc);

        // Odświeżenie listy (automatycznie zmieni stan IsEmpty/IsNotEmpty)
        await LoadExercisesAsync();
    }
}