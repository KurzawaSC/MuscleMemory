using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data;
using MuscleMemory.Models;
using MuscleMemory.Views;

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
        bool isLeavingPage = e.Source is ShellNavigationSource.Pop or ShellNavigationSource.PopToRoot;
        if (!isLeavingPage || !HasUnsavedChanges)
        {
            return;
        }

        e.Cancel();

        bool discard = await Shell.Current.DisplayAlertAsync(UiText.TitleUnsavedChanges, UiText.BodyUnsavedChangesConfirmation, UiText.ButtonDiscard, UiText.ButtonCancel);
        if (discard)
        {
            HasUnsavedChanges = false;
            await Shell.Current.GoToAsync(NavigationRoutes.GoBack);
        }
    }

    [RelayCommand]
    private async Task AddExerciseAsync()
    {
        var allExercises = await _dbContext.GetExercisesAsync();

        if (!allExercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyNoExercisesForWorkout, UiText.ButtonOk);
            return;
        }

        var page = Shell.Current.CurrentPage;
        if (page == null) return;

        var selectPopup = new SelectExercisePopup(allExercises);
        await page.ShowPopupAsync(selectPopup);

        if (selectPopup.SelectedExercise is not Exercise selectedExercise) return;

        await Task.Delay(UiTiming.SequentialPopupDelayMilliseconds);

        var configPopup = new ConfigureExercisePopup(selectedExercise);
        await page.ShowPopupAsync(configPopup);

        if (configPopup.ReturnedConfig is { } configResult)
        {
            AddExerciseToWorkout(
                selectedExercise,
                configResult.Sets,
                configResult.Reps,
                configResult.BreakTime,
                configResult.TargetRPE);
        }
    }

    [RelayCommand]
    private async Task EditExerciseAsync(WorkoutExercise exerciseToEdit)
    {
        if (exerciseToEdit == null) return;

        var page = Shell.Current.CurrentPage;
        if (page == null) return;

        var configPopup = new ConfigureExercisePopup(exerciseToEdit);
        await page.ShowPopupAsync(configPopup);

        if (configPopup.ReturnedConfig is { } configResult)
        {
            UpdateExerciseInWorkout(
                exerciseToEdit,
                configResult.Sets,
                configResult.Reps,
                configResult.BreakTime,
                configResult.TargetRPE);
        }
    }

    private void AddExerciseToWorkout(Exercise selectedExercise, int sets, int reps, int breakTime, int targetRPE)
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

    private void UpdateExerciseInWorkout(WorkoutExercise exercise, int sets, int reps, int breakTime, int targetRPE)
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
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyEnterWorkoutName, UiText.ButtonOk);
            return;
        }

        if (!Exercises.Any())
        {
            await Shell.Current.DisplayAlertAsync(UiText.TitleHoldOn, UiText.BodyAddAtLeastOneExercise, UiText.ButtonOk);
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
}
