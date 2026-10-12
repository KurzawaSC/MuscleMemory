using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Resources.Strings;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class WorkoutListViewModel : ObservableObject
{
    private readonly IWorkoutTemplateService _templates;
    private readonly IHapticService _haptics;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private readonly TimeProvider _clock;
    private int _latestLoad;

    public WorkoutListViewModel(
        IWorkoutTemplateService templates,
        ActiveWorkoutViewModel activeWorkout,
        IHapticService haptics,
        IDialogService dialogs,
        IDataChangeNotifier dataChanges,
        INavigationService navigation,
        IErrorHandler errors,
        TimeProvider clock)
    {
        _templates = templates;
        _haptics = haptics;
        _dialogs = dialogs;
        _navigation = navigation;
        _errors = errors;
        _clock = clock;
        ActiveWorkout = activeWorkout;
        dataChanges.Changed += OnDataChanged;
    }

    public ListLoadState ListState { get; } = new();

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
        var workouts = await _templates.GetAllAsync();
        var exercisesByWorkout = (await _templates.GetAllExercisesAsync()).ToLookup(exercise => exercise.WorkoutId);
        if (load != _latestLoad)
        {
            return;
        }

        Workouts.ReplaceAll(workouts.Select(workout => WorkoutListItem.Create(workout, exercisesByWorkout[workout.Id])));
        ListState.Complete(Workouts.Count);
    }

    [RelayCommand]
    private Task NavigateToAddWorkoutAsync() =>
        _errors.RunAsync(() => _navigation.GoToAsync(NavigationRoutes.AddEditWorkout));

    private bool CanStartWorkout(WorkoutListItem item) => item?.HasExercises == true;

    [RelayCommand(CanExecute = nameof(CanStartWorkout))]
    private Task StartWorkoutAsync(WorkoutListItem item) => _errors.RunAsync(async () =>
    {
        if (ActiveWorkout.IsBusy)
        {
            return;
        }

        if (ActiveWorkout.IsWorkoutActive)
        {
            await OfferToResumeActiveWorkoutAsync();
            return;
        }

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.Workout, item.Workout }
        };
        await _navigation.GoToAsync(NavigationRoutes.ActiveWorkout, navigationParameter);
    });

    private async Task OfferToResumeActiveWorkoutAsync()
    {
        var resume = await _dialogs.ConfirmAsync(
            AppResources.TitleHoldOn,
            string.Format(AppResources.WorkoutAlreadyActiveFormat, ActiveWorkout.WorkoutTitle),
            AppResources.ButtonResume,
            AppResources.ButtonCancel);

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
        _haptics.Click();
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
        if (await DismissActionSheetAsync() is not { } item)
        {
            return;
        }

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutToEdit, item.Workout }
        };
        await _navigation.GoToAsync(NavigationRoutes.AddEditWorkout, navigationParameter);
    });

    [RelayCommand]
    private Task ViewActionWorkoutHistoryAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissActionSheetAsync() is not { } item)
        {
            return;
        }

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.WorkoutId, item.Workout.Id },
            { QueryKeys.WorkoutName, item.Workout.Name }
        };
        await _navigation.GoToAsync(NavigationRoutes.WorkoutHistory, navigationParameter);
    });

    [RelayCommand]
    private Task DeleteActionWorkoutAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissActionSheetAsync() is not { } item)
        {
            return;
        }

        var answer = await _dialogs.ConfirmAsync(AppResources.TitleDeleteWorkout, string.Format(AppResources.DeleteWorkoutConfirmationFormat, item.Workout.Name), AppResources.ButtonDelete, AppResources.ButtonCancel);
        if (answer)
        {
            await _templates.DeleteAsync(item.Workout.Id);
            await ReloadAsync();
        }
    });

    private async Task<WorkoutListItem?> DismissActionSheetAsync()
    {
        var item = ActionWorkout;
        await SheetTransition.CloseAsync(CancelWorkoutActions, _clock);
        return item;
    }

    [RelayCommand(CanExecute = nameof(IsActionSheetOpen))]
    private void CloseSheets()
    {
        IsActionSheetOpen = false;
    }
}
