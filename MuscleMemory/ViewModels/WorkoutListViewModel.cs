using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Views;
using System.Collections.ObjectModel;

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
    private async Task StartWorkoutAsync(Workout selectedWorkout)
    {
        if (selectedWorkout == null) return;

        // Pakujemy nasz trening do słownika pod kluczem "Workout"
        var navigationParameter = new Dictionary<string, object>
        {
            { "Workout", selectedWorkout }
        };

        // Przechodzimy na ActiveWorkoutPage, przekazując paczkę
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), navigationParameter);
    }

    [RelayCommand]
    public async Task DeleteWorkoutAsync(Workout workout)
    {
        if (workout == null) return;

        bool answer = await Shell.Current.DisplayAlert("Delete Workout", $"Are you sure you want to delete '{workout.Name}'?", "Yes", "No");
        if (answer)
        {
            await _dbContext.DeleteWorkoutAsync(workout.Id);
            await LoadWorkoutsAsync();
        }
    }

    [RelayCommand]
    public async Task EditWorkoutAsync(Workout workout)
    {
        if (workout == null) return;
        
        var navigationParameter = new Dictionary<string, object>
        {
            { "WorkoutToEdit", workout }
        };
        await Shell.Current.GoToAsync("AddEditWorkoutPage", navigationParameter);
    }
}