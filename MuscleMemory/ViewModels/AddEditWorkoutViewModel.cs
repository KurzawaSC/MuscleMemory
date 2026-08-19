using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class AddEditWorkoutViewModel(DatabaseContext dbContext, IPopupService popupService) : ObservableObject, IQueryAttributable
{
    private readonly DatabaseContext _dbContext = dbContext;
    private readonly IPopupService _popupService = popupService;
    private Workout? _workoutToEdit;

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
    public ObservableCollection<WorkoutExercise> Exercises { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutToEdit, out var editable) && editable is Workout workout)
        {
            _workoutToEdit = workout;
            _ = LoadWorkoutAsync(workout);
        }
    }

    private async Task LoadWorkoutAsync(Workout workout)
    {
        WorkoutName = workout.Name;

        var exercisesFromDb = await _dbContext.GetExercisesForWorkoutAsync(workout.Id);
        Exercises.Clear();
        foreach (var ex in exercisesFromDb)
        {
            Exercises.Add(ex);
        }

        IsEmpty = !Exercises.Any();
        HasUnsavedChanges = false;
    }

    [RelayCommand]
    private void StartGuardingUnsavedChanges()
    {
        Shell.Current.Navigating += OnShellNavigating;
    }

    [RelayCommand]
    private void StopGuardingUnsavedChanges()
    {
        Shell.Current.Navigating -= OnShellNavigating;
    }

    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (!HasUnsavedChanges || !e.CanCancel || !IsLeavingEditor(e))
        {
            return;
        }

        var destination = e.Target;
        e.Cancel();

        bool discard = await Shell.Current.DisplayAlertAsync(UiText.TitleUnsavedChanges, UiText.BodyUnsavedChangesConfirmation, UiText.ButtonDiscard, UiText.ButtonCancel);
        if (!discard)
        {
            return;
        }

        HasUnsavedChanges = false;
        await Shell.Current.GoToAsync(NavigationRoutes.GoBack);

        if (destination != null && Shell.Current.CurrentState.Location != destination.Location)
        {
            await Shell.Current.GoToAsync(destination);
        }
    }

    private static bool IsLeavingEditor(ShellNavigatingEventArgs e) =>
        IsEditorLocation(e.Current) && !IsEditorLocation(e.Target);

    private static bool IsEditorLocation(ShellNavigationState? state) =>
        state?.Location.OriginalString.Contains(nameof(AddEditWorkoutPage), StringComparison.Ordinal) == true;

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        var allExercises = await _dbContext.GetExercisesAsync();

        if (!allExercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyNoExercisesForWorkout, UiText.ButtonOk);
            return;
        }

        var selection = await _popupService.ShowPopupAsync<SelectExercisePopup, Exercise?>(
            Shell.Current,
            shellParameters: new Dictionary<string, object> { [QueryKeys.AvailableExercises] = allExercises });

        if (selection.Result is not Exercise selectedExercise) return;

        await Task.Delay(UiTiming.SequentialPopupDelayMilliseconds);

        var configuration = await ShowConfigurationPopupAsync(QueryKeys.SelectedExercise, selectedExercise);
        if (configuration != null)
        {
            AddExerciseToWorkout(selectedExercise, configuration);
        }
    }

    [RelayCommand]
    private async Task EditExerciseAsync(WorkoutExercise exerciseToEdit)
    {
        if (exerciseToEdit == null) return;

        var configuration = await ShowConfigurationPopupAsync(QueryKeys.WorkoutExerciseToEdit, exerciseToEdit);
        if (configuration != null)
        {
            UpdateExerciseInWorkout(exerciseToEdit, configuration);
        }
    }

    private async Task<ExerciseConfiguration?> ShowConfigurationPopupAsync(string queryKey, object parameter)
    {
        var result = await _popupService.ShowPopupAsync<ConfigureExercisePopup, ExerciseConfiguration?>(
            Shell.Current,
            shellParameters: new Dictionary<string, object> { [queryKey] = parameter });

        return result.Result;
    }

    private void AddExerciseToWorkout(Exercise selectedExercise, ExerciseConfiguration configuration)
    {
        var newWorkoutExercise = new WorkoutExercise
        {
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = configuration.Sets,
            Reps = configuration.Reps,
            BreakTimeInSeconds = configuration.BreakTimeInSeconds,
            TargetRPE = configuration.TargetRPE
        };

        Exercises.Add(newWorkoutExercise);
        IsEmpty = !Exercises.Any();
        HasUnsavedChanges = true;
    }

    private void UpdateExerciseInWorkout(WorkoutExercise exercise, ExerciseConfiguration configuration)
    {
        var index = Exercises.IndexOf(exercise);
        if (index >= 0)
        {
            exercise.Sets = configuration.Sets;
            exercise.Reps = configuration.Reps;
            exercise.BreakTimeInSeconds = configuration.BreakTimeInSeconds;
            exercise.TargetRPE = configuration.TargetRPE;
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
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyEnterWorkoutName, UiText.ButtonOk);
            return;
        }

        if (!Exercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyAddAtLeastOneExercise, UiText.ButtonOk);
            return;
        }

        if (_workoutToEdit != null)
        {
            _workoutToEdit.Name = WorkoutName.Trim();
            await _dbContext.UpdateFullWorkoutAsync(_workoutToEdit, Exercises.ToList());
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
}
