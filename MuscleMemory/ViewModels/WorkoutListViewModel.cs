using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Data;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class WorkoutListViewModel : ObservableObject
{
    private readonly DatabaseContext _dbContext;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool IsNotEmpty { get; set; } = false;

    // Kolekcja naszych treningów wyświetlanych na ekranie
    public ObservableCollection<Workout> Workouts { get; set; } = new();

    public WorkoutListViewModel(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Pobieranie treningów z bazy danych
    [RelayCommand]
    public async Task LoadWorkoutsAsync()
    {
        var workoutsFromDb = await _dbContext.GetWorkoutsAsync();

        Workouts.Clear();
        foreach (var workout in workoutsFromDb)
        {
            Workouts.Add(workout);
        }

        IsEmpty = !Workouts.Any();
        IsNotEmpty = Workouts.Any();
    }

    // Przejście do utworzonego wcześniej kreatora
    [RelayCommand]
    private async Task NavigateToAddWorkout()
    {
        await Shell.Current.GoToAsync("AddEditWorkoutPage");
    }

    // Przejście do właściwego treningu (przekazujemy ID wybranego treningu!)
    [RelayCommand]
    private async Task StartWorkout(Workout selectedWorkout)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "WorkoutId", selectedWorkout.Id }
        };

        // Z tym parametrem udamy się do ekranu ActiveWorkoutPage
        await Shell.Current.GoToAsync("ActiveWorkoutPage", navigationParameter);
    }
}