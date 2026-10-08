using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IExerciseCatalogService _exerciseCatalog;
    private readonly IHapticService _haptics;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private List<ExerciseItem> _allExercises = [];
    private int _latestLoad;

    public ExerciseListViewModel(
        IExerciseRepository exerciseRepository,
        IWorkoutRepository workoutRepository,
        IExerciseCatalogService exerciseCatalog,
        ActiveWorkoutViewModel activeWorkout,
        ExerciseFormViewModel exerciseForm,
        ExerciseFilterViewModel filter,
        IHapticService haptics,
        IDialogService dialogs,
        IDataChangeNotifier dataChanges,
        INavigationService navigation,
        IErrorHandler errors)
    {
        _exerciseRepository = exerciseRepository;
        _workoutRepository = workoutRepository;
        _exerciseCatalog = exerciseCatalog;
        _haptics = haptics;
        _dialogs = dialogs;
        _navigation = navigation;
        _errors = errors;
        ActiveWorkout = activeWorkout;
        ExerciseForm = exerciseForm;
        Filter = filter;
        Filter.Changed += (_, _) => ApplyFilter();
        dataChanges.Changed += OnDataChanged;
        SaveExerciseCommand.NotifyCanExecuteChangedWhen(ExerciseForm, nameof(ExerciseFormViewModel.CanSave));
    }

    public ListLoadState ListState { get; } = new();

    [ObservableProperty]
    public partial bool HasNoMatches { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsExerciseFormOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial ExerciseItem? ActionExercise { get; set; }

    public ObservableCollection<ExerciseItem> Exercises { get; } = [];

    public ExerciseFilterViewModel Filter { get; }

    public ExerciseFormViewModel ExerciseForm { get; }

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    private bool IsAnySheetOpen => IsExerciseFormOpen || IsActionSheetOpen;

    partial void OnIsActionSheetOpenChanged(bool value)
    {
        if (!value)
        {
            ActionExercise = null;
        }
    }

    private void OnDataChanged(object? sender, DataArea areas)
    {
        if (areas.HasFlag(DataArea.Exercises))
        {
            _errors.ReportFailures(ReloadAsync());
        }
    }

    [RelayCommand]
    private Task LoadExercisesAsync() => _errors.RunAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var load = ++_latestLoad;
        var exercises = await _exerciseRepository.GetAllAsync();
        if (load != _latestLoad)
        {
            return;
        }

        _allExercises = [.. exercises.Select(ExerciseItem.Create)];
        ListState.Complete(_allExercises.Count);
        Filter.UpdateFilters(exercises);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Exercises.ReplaceAll(Filter.Apply(_allExercises));
        HasNoMatches = ListState.HasItems && Exercises.Count == 0;
    }

    [RelayCommand]
    private Task AddExerciseAsync() => _errors.RunAsync(async () =>
    {
        await ExerciseForm.BeginNewAsync();
        IsExerciseFormOpen = true;
    });

    private bool CanSaveExercise => ExerciseForm.CanSave;

    [RelayCommand(CanExecute = nameof(CanSaveExercise))]
    private Task SaveExerciseAsync() => _errors.RunAsync(async () =>
    {
        await ExerciseForm.SaveAsync();
        IsExerciseFormOpen = false;
    });

    [RelayCommand]
    private void CancelExerciseForm()
    {
        IsExerciseFormOpen = false;
    }

    [RelayCommand]
    private void ShowExerciseActions(ExerciseItem item)
    {
        ActionExercise = item;
        IsActionSheetOpen = true;
    }

    [RelayCommand]
    private void LongPressExercise(ExerciseItem item)
    {
        _haptics.Click();
        ShowExerciseActions(item);
    }

    [RelayCommand]
    private void CancelExerciseActions()
    {
        IsActionSheetOpen = false;
    }

    [RelayCommand]
    private Task EditActionExerciseAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissActionSheetAsync() is not { } exercise)
        {
            return;
        }

        await ExerciseForm.BeginEditAsync(exercise);
        IsExerciseFormOpen = true;
    });

    [RelayCommand]
    private Task ViewActionExerciseHistoryAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissActionSheetAsync() is not { } exercise)
        {
            return;
        }

        var navigationParameter = new Dictionary<string, object>
        {
            { QueryKeys.ExerciseId, exercise.Id },
            { QueryKeys.ExerciseName, exercise.Name }
        };
        await _navigation.GoToAsync(NavigationRoutes.ExerciseHistory, navigationParameter);
    });

    [RelayCommand]
    private Task DeleteActionExerciseAsync() => _errors.RunAsync(async () =>
    {
        if (await DismissActionSheetAsync() is not { } exercise)
        {
            return;
        }

        var workoutCount = await _workoutRepository.CountWorkoutsContainingAsync(exercise.Id);
        var answer = await _dialogs.ConfirmAsync(UiText.TitleDeleteExercise, DeleteConfirmationText(exercise.Name, workoutCount), UiText.ButtonDelete, UiText.ButtonCancel);
        if (answer)
        {
            await _exerciseCatalog.DeleteAsync(exercise.Id);
        }
    });

    private static string DeleteConfirmationText(string exerciseName, int workoutCount) => workoutCount > 0
        ? string.Format(CultureInfo.CurrentCulture, UiText.DeleteExerciseFromWorkoutsFormat, exerciseName, workoutCount, CountCaption.Workouts(workoutCount))
        : string.Format(UiText.DeleteConfirmationFormat, exerciseName);

    private async Task<Exercise?> DismissActionSheetAsync()
    {
        var exercise = ActionExercise?.Exercise;
        await SheetTransition.CloseAsync(CancelExerciseActions);
        return exercise;
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsExerciseFormOpen = false;
        IsActionSheetOpen = false;
    }
}
