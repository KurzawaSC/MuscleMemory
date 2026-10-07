using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class AddEditWorkoutViewModel(
    IWorkoutRepository workoutRepository,
    ExercisePickerViewModel exercisePicker,
    ConfigureExerciseViewModel exerciseConfiguration,
    ExerciseFormViewModel exerciseForm,
    IDialogService dialogs,
    INavigationStackService navigationStack,
    INavigationService navigation,
    IErrorHandler errors) : ObservableObject, IQueryAttributable, IUnsavedChangesGuard
{
    private bool _isGuardingUnsavedChanges;
    private Workout? _workoutToEdit;
    private Exercise? _exerciseToAdd;
    private WorkoutExercise? _exerciseBeingEdited;

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = UiText.HeaderNewWorkout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsEmptyHintVisible))]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsEmptyHintVisible))]
    [NotifyCanExecuteChangedFor(nameof(AddExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    public partial bool IsLoading { get; set; }

    public bool IsEditable => !IsLoading;

    public bool IsEmptyHintVisible => IsEmpty && !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExerciseCountCaption))]
    public partial int ExerciseCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSetsCaption))]
    public partial int TotalSets { get; set; }

    public string ExerciseCountCaption => CountCaption.Exercises(ExerciseCount);

    public string TotalSetsCaption => CountCaption.Sets(TotalSets);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string WorkoutName { get; set; } = string.Empty;

    partial void OnWorkoutNameChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    [ObservableProperty]
    public partial bool IsExercisePickerOpen { get; set; }

    [ObservableProperty]
    public partial bool IsExerciseFormOpen { get; set; }

    [ObservableProperty]
    public partial bool IsConfigurationOpen { get; set; }

    public ObservableCollection<WorkoutExercise> Exercises { get; } = [];

    public ExercisePickerViewModel ExercisePicker { get; } = exercisePicker;

    public ConfigureExerciseViewModel ExerciseConfiguration { get; } = exerciseConfiguration;

    public ExerciseFormViewModel ExerciseForm { get; } = exerciseForm;

    public bool CanSave => IsEditable && !string.IsNullOrWhiteSpace(WorkoutName) && !IsEmpty;

    private bool IsAnySheetOpen => IsExercisePickerOpen || IsExerciseFormOpen || IsConfigurationOpen;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutToEdit, out var editable) && editable is Workout workout)
        {
            _workoutToEdit = workout;
            HeaderTitle = UiText.HeaderEditWorkout;
            IsLoading = true;
            errors.ReportFailures(LoadWorkoutAsync(workout));
        }
    }

    private async Task LoadWorkoutAsync(Workout workout)
    {
        var exercises = await workoutRepository.GetExercisesAsync(workout.Id);

        WorkoutName = workout.Name;
        Exercises.ReplaceAll(exercises);
        RefreshSummary();
        HasUnsavedChanges = false;
        IsLoading = false;
    }

    [RelayCommand]
    private void StartGuardingUnsavedChanges()
    {
        if (_isGuardingUnsavedChanges)
        {
            return;
        }

        _isGuardingUnsavedChanges = true;
        navigation.Navigating += OnShellNavigating;
        navigation.Navigated += OnShellNavigated;
    }

    [RelayCommand]
    private void StopGuardingUnsavedChanges()
    {
        if (!_isGuardingUnsavedChanges)
        {
            return;
        }

        navigation.Navigating -= OnShellNavigating;
        navigation.Navigated -= OnShellNavigated;
        _isGuardingUnsavedChanges = false;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (!navigationStack.ContainsPageBoundTo(this))
        {
            StopGuardingUnsavedChanges();
        }
    }

    private void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (!HasUnsavedChanges || !e.CanCancel || !IsLeavingEditor(e))
        {
            return;
        }

        var destination = e.Target;
        e.Cancel();
        errors.ReportFailures(ConfirmLeavingEditorAsync(destination));
    }

    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var discard = await dialogs.ConfirmAsync(UiText.TitleUnsavedChanges, UiText.BodyUnsavedChangesConfirmation, UiText.ButtonDiscard, UiText.ButtonCancel);
        if (discard)
        {
            HasUnsavedChanges = false;
        }

        return discard;
    }

    private async Task ConfirmLeavingEditorAsync(ShellNavigationState? destination)
    {
        if (!await ConfirmDiscardAsync())
        {
            return;
        }

        await navigation.GoToAsync(NavigationRoutes.GoBack);

        if (destination is not null && navigation.CurrentState.Location != destination.Location)
        {
            await navigation.GoToAsync(destination);
        }
    }

    [RelayCommand]
    private Task NavigateBackAsync() => errors.RunAsync(async () =>
    {
        if (IsAnySheetOpen)
        {
            CloseSheets();
            return;
        }

        if (await ConfirmDiscardAsync())
        {
            await navigation.GoToAsync(NavigationRoutes.GoBack);
        }
    });

    private static bool IsLeavingEditor(ShellNavigatingEventArgs e) =>
        IsEditorLocation(e.Current) && !IsEditorLocation(e.Target) && !KeepsEditorOnStack(e);

    private static bool KeepsEditorOnStack(ShellNavigatingEventArgs e) =>
        e.Source is ShellNavigationSource.Push;

    private static bool IsEditorLocation(ShellNavigationState? state) =>
        state?.Location.OriginalString.Contains(NavigationRoutes.AddEditWorkout, StringComparison.Ordinal) == true;

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task AddExerciseAsync() => errors.RunAsync(async () =>
    {
        await ExercisePicker.LoadAsync();
        IsExercisePickerOpen = true;
    });

    [RelayCommand]
    private void CloseExercisePicker()
    {
        IsExercisePickerOpen = false;
    }

    [RelayCommand]
    private Task PickExerciseAsync(Exercise exercise) => errors.RunAsync(async () =>
    {
        await SheetTransition.CloseAsync(CloseExercisePicker);
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand]
    private Task CreateExerciseAsync() => errors.RunAsync(async () =>
    {
        await SheetTransition.CloseAsync(CloseExercisePicker);
        await ExerciseForm.BeginNewAsync();
        IsExerciseFormOpen = true;
    });

    [RelayCommand]
    private void CancelExerciseForm()
    {
        IsExerciseFormOpen = false;
    }

    [RelayCommand]
    private Task SaveNewExerciseAsync() => errors.RunAsync(async () =>
    {
        if (!ExerciseForm.CanSave)
        {
            return;
        }

        var exercise = await ExerciseForm.SaveAsync();
        await SheetTransition.CloseAsync(CancelExerciseForm);
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private void EditExercise(WorkoutExercise exercise)
    {
        _exerciseToAdd = null;
        _exerciseBeingEdited = exercise;
        ExerciseConfiguration.BeginEdit(exercise);
        IsConfigurationOpen = true;
    }

    [RelayCommand]
    private void CancelConfiguration()
    {
        IsConfigurationOpen = false;
    }

    [RelayCommand]
    private void ConfirmConfiguration()
    {
        var configuration = ExerciseConfiguration.ToConfiguration();

        if (_exerciseBeingEdited is { } editedExercise)
        {
            UpdateExerciseInWorkout(editedExercise, configuration);
        }
        else if (_exerciseToAdd is { } newExercise)
        {
            AddExerciseToWorkout(newExercise, configuration);
        }

        IsConfigurationOpen = false;
    }

    [RelayCommand]
    private void RemoveEditedExercise()
    {
        if (_exerciseBeingEdited is { } exercise && Exercises.Remove(exercise))
        {
            RefreshSummary();
            HasUnsavedChanges = true;
        }

        IsConfigurationOpen = false;
    }

    private void CloseSheets()
    {
        IsExercisePickerOpen = false;
        IsExerciseFormOpen = false;
        IsConfigurationOpen = false;
    }

    private void OpenConfigurationForNewExercise(Exercise exercise)
    {
        _exerciseBeingEdited = null;
        _exerciseToAdd = exercise;
        ExerciseConfiguration.BeginNew(exercise.Name);
        IsConfigurationOpen = true;
    }

    private void AddExerciseToWorkout(Exercise selectedExercise, ExerciseConfiguration configuration)
    {
        Exercises.Add(new WorkoutExercise
        {
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = configuration.Sets,
            Reps = configuration.Reps,
            BreakTimeInSeconds = configuration.BreakTimeInSeconds,
            TargetRPE = configuration.TargetRPE
        });

        RefreshSummary();
        HasUnsavedChanges = true;
    }

    private void UpdateExerciseInWorkout(WorkoutExercise exercise, ExerciseConfiguration configuration)
    {
        var index = Exercises.IndexOf(exercise);
        if (index < 0)
        {
            return;
        }

        exercise.Sets = configuration.Sets;
        exercise.Reps = configuration.Reps;
        exercise.BreakTimeInSeconds = configuration.BreakTimeInSeconds;
        exercise.TargetRPE = configuration.TargetRPE;
        Exercises[index] = exercise;

        RefreshSummary();
        HasUnsavedChanges = true;
    }

    private void RefreshSummary()
    {
        ExerciseCount = Exercises.Count;
        IsEmpty = ExerciseCount == 0;
        TotalSets = Exercises.Sum(exercise => exercise.Sets);
    }

    [RelayCommand]
    private Task SaveWorkoutAsync() => errors.RunAsync(async () =>
    {
        if (!CanSave)
        {
            return;
        }

        var name = WorkoutName.Trim();

        if (_workoutToEdit is not null)
        {
            await workoutRepository.UpdateWithExercisesAsync(new Workout { Id = _workoutToEdit.Id, Name = name }, [.. Exercises]);
            _workoutToEdit.Name = name;
        }
        else
        {
            var workout = new Workout { Name = name };
            await workoutRepository.SaveWithExercisesAsync(workout, [.. Exercises]);
            _workoutToEdit = workout;
        }

        HasUnsavedChanges = false;
        await navigation.GoToAsync(NavigationRoutes.GoBack);
    });
}
