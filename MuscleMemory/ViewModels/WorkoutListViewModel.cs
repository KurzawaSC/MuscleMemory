using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Models;
using MuscleMemory.Views;
using System.Collections.ObjectModel;

namespace MuscleMemory.ViewModels;

public partial class WorkoutListViewModel(IWorkoutRepository workoutRepository, ActiveWorkoutViewModel activeWorkout) : ObservableObject
{
    private readonly IWorkoutRepository _workoutRepository = workoutRepository;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<Workout> Workouts { get; } = [];

    public ActiveWorkoutViewModel ActiveWorkout { get; } = activeWorkout;
    [RelayCommand]
    private async Task LoadWorkoutsAsync()
    {
        var workoutsFromDb = await _workoutRepository.GetAllAsync();

        Workouts.Clear();
        foreach (var workout in workoutsFromDb)
        {
            Workouts.Add(workout);
        }

        IsEmpty = !Workouts.Any();
    }
    [RelayCommand]
    private async Task NavigateToAddWorkout()
    {
        await Shell.Current.GoToAsync(nameof(AddEditWorkoutPage));
    }
    [RelayCommand]
    private async Task StartWorkoutAsync(Workout selectedWorkout)
    {
        if (selectedWorkout == null) return;
        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.Workout, selectedWorkout }
        };
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), navigationParameter);
    }

    [RelayCommand]
    private async Task DeleteWorkoutAsync(Workout workout)
    {
        if (workout == null) return;

        bool answer = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteWorkout, string.Format(UiText.DeleteConfirmationFormat, workout.Name), UiText.ButtonYes, UiText.ButtonNo);
        if (answer)
        {
            await _workoutRepository.DeleteAsync(workout.Id);
            await LoadWorkoutsAsync();
        }
    }

    [RelayCommand]
    private async Task EditWorkoutAsync(Workout workout)
    {
        if (workout == null) return;
        
        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutToEdit, workout }
        };
        await Shell.Current.GoToAsync(nameof(AddEditWorkoutPage), navigationParameter);
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(Workout workout)
    {
        if (workout == null) return;
        
        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutId, workout.Id },
            { QueryKeys.WorkoutName, workout.Name }
        };
        await Shell.Current.GoToAsync(nameof(WorkoutHistoryPage), navigationParameter);
    }
}
