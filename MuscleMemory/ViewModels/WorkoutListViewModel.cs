using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Views;
using System.Collections.ObjectModel;

namespace MuscleMemory.ViewModels;

public partial class WorkoutListViewModel(
    IWorkoutRepository workoutRepository,
    ActiveWorkoutViewModel activeWorkout,
    IHapticService hapticService) : ObservableObject
{
    private readonly IWorkoutRepository _workoutRepository = workoutRepository;
    private readonly IHapticService _hapticService = hapticService;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial WorkoutListItem? ActionWorkout { get; set; }

    public ObservableCollection<WorkoutListItem> Workouts { get; } = [];

    public ActiveWorkoutViewModel ActiveWorkout { get; } = activeWorkout;

    partial void OnIsActionSheetOpenChanged(bool value)
    {
        if (!value)
        {
            ActionWorkout = null;
        }
    }

    [RelayCommand]
    private async Task LoadWorkoutsAsync()
    {
        var workouts = await _workoutRepository.GetAllAsync();
        var exercisesByWorkout = (await _workoutRepository.GetAllExercisesAsync()).ToLookup(exercise => exercise.WorkoutId);

        Workouts.ReplaceAll(workouts.Select((workout, index) =>
            WorkoutListItem.Create(workout, exercisesByWorkout[workout.Id], isFeatured: index == 0)));
        IsEmpty = Workouts.Count == 0;
    }

    [RelayCommand]
    private async Task NavigateToAddWorkout()
    {
        await Shell.Current.GoToAsync(nameof(AddEditWorkoutPage));
    }

    [RelayCommand]
    private async Task StartWorkoutAsync(WorkoutListItem item)
    {
        if (ActiveWorkout.IsWorkoutActive)
        {
            await OfferToResumeActiveWorkoutAsync();
            return;
        }

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.Workout, item.Workout }
        };
        await Shell.Current.GoToAsync(nameof(ActiveWorkoutPage), navigationParameter);
    }

    private async Task OfferToResumeActiveWorkoutAsync()
    {
        bool resume = await Shell.Current.DisplayAlertAsync(
            UiText.TitleHoldOn,
            string.Format(UiText.WorkoutAlreadyActiveFormat, ActiveWorkout.WorkoutTitle),
            UiText.ButtonResume,
            UiText.ButtonCancel);

        if (resume)
        {
            await ActiveWorkout.ResumeWorkoutAsync();
        }
    }

    [RelayCommand]
    private void ShowWorkoutActions(WorkoutListItem item)
    {
        ActionWorkout = item;
        IsActionSheetOpen = true;
    }

    [RelayCommand]
    private void LongPressWorkout(WorkoutListItem item)
    {
        _hapticService.Click();
        ShowWorkoutActions(item);
    }

    [RelayCommand]
    private void CancelWorkoutActions()
    {
        IsActionSheetOpen = false;
    }

    [RelayCommand]
    private async Task EditActionWorkoutAsync()
    {
        if (ActionWorkout is not { } item)
        {
            return;
        }

        IsActionSheetOpen = false;

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutToEdit, item.Workout }
        };
        await Shell.Current.GoToAsync(nameof(AddEditWorkoutPage), navigationParameter);
    }

    [RelayCommand]
    private async Task ViewActionWorkoutHistoryAsync()
    {
        if (ActionWorkout is not { } item)
        {
            return;
        }

        IsActionSheetOpen = false;

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutId, item.Workout.Id },
            { QueryKeys.WorkoutName, item.Workout.Name }
        };
        await Shell.Current.GoToAsync(nameof(WorkoutHistoryPage), navigationParameter);
    }

    [RelayCommand]
    private async Task DeleteActionWorkoutAsync()
    {
        if (ActionWorkout is not { } item)
        {
            return;
        }

        IsActionSheetOpen = false;
        await Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));

        bool answer = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteWorkout, string.Format(UiText.DeleteConfirmationFormat, item.Workout.Name), UiText.ButtonYes, UiText.ButtonNo);
        if (answer)
        {
            await _workoutRepository.DeleteAsync(item.Workout.Id);
            await LoadWorkoutsAsync();
        }
    }

    [RelayCommand(CanExecute = nameof(IsActionSheetOpen))]
    private void CloseSheets()
    {
        IsActionSheetOpen = false;
    }
}
