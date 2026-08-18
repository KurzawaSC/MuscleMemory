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
    private readonly ActiveWorkoutViewModel _activeWorkoutViewModel;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool IsNotEmpty { get; set; } = false;
    
    public bool CanAddItems => !(_activeWorkoutViewModel?.IsWorkoutActive ?? false);
    
    public ObservableCollection<Workout> Workouts { get; set; } = new();

    public WorkoutListViewModel(DatabaseContext dbContext, ActiveWorkoutViewModel activeWorkoutViewModel)
    {
        _dbContext = dbContext;
        _activeWorkoutViewModel = activeWorkoutViewModel;
        
        _activeWorkoutViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ActiveWorkoutViewModel.IsWorkoutActive))
            {
                OnPropertyChanged(nameof(CanAddItems));
            }
        };
    }
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
    [RelayCommand]
    private async Task NavigateToAddWorkout()
    {
        await Shell.Current.GoToAsync("AddEditWorkoutPage");
    }
    [RelayCommand]
    private async Task StartWorkoutAsync(Workout selectedWorkout)
    {
        if (selectedWorkout == null) return;
        var navigationParameter = new Dictionary<string, object>
        {
            { "Workout", selectedWorkout }
        };
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), navigationParameter);
    }

    [RelayCommand]
    public async Task DeleteWorkoutAsync(Workout workout)
    {
        if (workout == null) return;

        bool answer = await Shell.Current.DisplayAlertAsync("Delete Workout", $"Are you sure you want to delete '{workout.Name}'?", "Yes", "No");
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

    [RelayCommand]
    public async Task ViewHistoryAsync(Workout workout)
    {
        if (workout == null) return;
        
        await Shell.Current.GoToAsync($"{nameof(WorkoutHistoryPage)}?WorkoutId={workout.Id}&WorkoutName={workout.Name}");
    }
}
