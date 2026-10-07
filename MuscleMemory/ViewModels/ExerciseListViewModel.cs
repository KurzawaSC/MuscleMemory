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
    private List<Exercise> _allExercises = [];
    private int _latestLoad;

    public ExerciseListViewModel(
        IExerciseRepository exerciseRepository,
        IWorkoutRepository workoutRepository,
        IExerciseCatalogService exerciseCatalog,
        ActiveWorkoutViewModel activeWorkout,
        AddEditExerciseViewModel exerciseForm,
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
    [NotifyPropertyChangedFor(nameof(ActionExerciseSubtitle))]
    public partial Exercise? ActionExercise { get; set; }

    public ObservableCollection<Exercise> Exercises { get; } = [];

    public ExerciseFilterViewModel Filter { get; }

    public AddEditExerciseViewModel ExerciseForm { get; }

    public ActiveWorkoutViewModel ActiveWorkout { get; }

    public string ActionExerciseSubtitle => ActionExercise is { } exercise
        ? string.Join(UiText.ListSeparator, exercise.TargetMuscleGroup.ToDisplayName(), exercise.Equipment.ToDisplayName())
        : string.Empty;

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

        _allExercises = exercises;
        ListState.Complete(_allExercises.Count);
        Filter.UpdateFilters(_allExercises);
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

    [RelayCommand]
    private Task SaveExerciseAsync() => _errors.RunAsync(async () =>
    {
        if (!ExerciseForm.CanSave)
        {
            return;
        }

        await ExerciseForm.SaveAsync();
        IsExerciseFormOpen = false;
    });

    [RelayCommand]
    private void CancelExerciseForm()
    {
        IsExerciseFormOpen = false;
    }

    [RelayCommand]
    private void ShowExerciseActions(Exercise exercise)
    {
        ActionExercise = exercise;
        IsActionSheetOpen = true;
    }

    [RelayCommand]
    private void LongPressExercise(Exercise exercise)
    {
        _haptics.Click();
        ShowExerciseActions(exercise);
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
        if (ActionExercise is not { } exercise)
        {
            return;
        }

        IsActionSheetOpen = false;

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
        var exercise = ActionExercise;
        IsActionSheetOpen = false;
        await Task.Delay(UiTiming.SheetClose);
        return exercise;
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsExerciseFormOpen = false;
        IsActionSheetOpen = false;
    }
}
