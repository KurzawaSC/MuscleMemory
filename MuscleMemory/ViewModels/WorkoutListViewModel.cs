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

public partial class WorkoutListViewModel : ObservableObject
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IHapticService _hapticService;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private int _latestLoad;

    public WorkoutListViewModel(
        IWorkoutRepository workoutRepository,
        ActiveWorkoutViewModel activeWorkout,
        IHapticService hapticService,
        IDialogService dialogs,
        IDataChangeNotifier dataChanges,
        INavigationService navigation,
        IErrorHandler errors)
    {
        _workoutRepository = workoutRepository;
        _hapticService = hapticService;
        _dialogs = dialogs;
        _navigation = navigation;
        _errors = errors;
        ActiveWorkout = activeWorkout;
        dataChanges.Changed += OnDataChanged;
    }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial WorkoutListItem? ActionWorkout { get; set; }

    public ObservableCollection<WorkoutListItem> Workouts { get; } = [];

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    partial void OnIsActionSheetOpenChanged(bool value)
    {
        if (!value)
        {
            ActionWorkout = null;
        }
    }

    private void OnDataChanged(object? sender, DataArea areas)
    {
        if (areas.HasFlag(DataArea.Workouts))
        {
            _errors.ReportFailures(ReloadAsync());
        }
    }

    [RelayCommand]
    private Task LoadWorkoutsAsync() => _errors.RunAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var load = ++_latestLoad;
        var workouts = await _workoutRepository.GetAllAsync();
        var exercisesByWorkout = (await _workoutRepository.GetAllExercisesAsync()).ToLookup(exercise => exercise.WorkoutId);
        if (load != _latestLoad)
        {
            return;
        }

        Workouts.ReplaceAll(workouts.Select((workout, index) =>
            WorkoutListItem.Create(workout, exercisesByWorkout[workout.Id], isFeatured: index == 0)));
        IsEmpty = Workouts.Count == 0;
    }

    [RelayCommand]
    private Task NavigateToAddWorkout() =>
        _errors.RunAsync(() => _navigation.GoToAsync(nameof(AddEditWorkoutPage)));

    private bool CanStartWorkout(WorkoutListItem item) => item?.HasExercises == true && !ActiveWorkout.IsBusy;

    [RelayCommand(CanExecute = nameof(CanStartWorkout))]
    private Task StartWorkoutAsync(WorkoutListItem item) => _errors.RunAsync(async () =>
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
        await _navigation.GoToAsync(nameof(ActiveWorkoutPage), navigationParameter);
    });

    private async Task OfferToResumeActiveWorkoutAsync()
    {
        bool resume = await _dialogs.ConfirmAsync(
            UiText.TitleHoldOn,
            string.Format(UiText.WorkoutAlreadyActiveFormat, ActiveWorkout.WorkoutTitle),
            UiText.ButtonResume,
            UiText.ButtonCancel);

        if (resume && ActiveWorkout.ResumeWorkoutCommand.CanExecute(null))
        {
            await ActiveWorkout.ResumeWorkoutCommand.ExecuteAsync(null);
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
    private Task EditActionWorkoutAsync() => _errors.RunAsync(async () =>
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
        await _navigation.GoToAsync(nameof(AddEditWorkoutPage), navigationParameter);
    });

    [RelayCommand]
    private Task ViewActionWorkoutHistoryAsync() => _errors.RunAsync(async () =>
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
        await _navigation.GoToAsync(nameof(WorkoutHistoryPage), navigationParameter);
    });

    [RelayCommand]
    private Task DeleteActionWorkoutAsync() => _errors.RunAsync(async () =>
    {
        if (ActionWorkout is not { } item)
        {
            return;
        }

        IsActionSheetOpen = false;
        await Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));

        bool answer = await _dialogs.ConfirmAsync(string.Format(UiText.DeleteWorkoutTitleFormat, item.Workout.Name), UiText.BodyDeleteWorkout, UiText.ButtonDelete, UiText.ButtonCancel);
        if (answer)
        {
            await _workoutRepository.DeleteAsync(item.Workout.Id);
            await ReloadAsync();
        }
    });

    [RelayCommand(CanExecute = nameof(IsActionSheetOpen))]
    private void CloseSheets()
    {
        IsActionSheetOpen = false;
    }
}
