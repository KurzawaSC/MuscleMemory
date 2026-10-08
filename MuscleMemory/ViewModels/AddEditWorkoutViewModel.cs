using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class AddEditWorkoutViewModel : ObservableObject, IQueryAttributable, IUnsavedChangesGuard
{
    private readonly IWorkoutRepository _workoutRepository;
    private readonly IDialogService _dialogs;
    private readonly INavigationStackService _navigationStack;
    private readonly INavigationService _navigation;
    private readonly IErrorHandler _errors;
    private bool _isGuardingUnsavedChanges;
    private Workout? _workoutToEdit;
    private Exercise? _exerciseToAdd;
    private WorkoutExerciseItem? _exerciseBeingEdited;

    public AddEditWorkoutViewModel(
        IWorkoutRepository workoutRepository,
        ExercisePickerViewModel exercisePicker,
        ExerciseConfigurationViewModel exerciseConfiguration,
        ExerciseFormViewModel exerciseForm,
        IDialogService dialogs,
        INavigationStackService navigationStack,
        INavigationService navigation,
        IErrorHandler errors)
    {
        _workoutRepository = workoutRepository;
        _dialogs = dialogs;
        _navigationStack = navigationStack;
        _navigation = navigation;
        _errors = errors;
        ExercisePicker = exercisePicker;
        ExerciseConfiguration = exerciseConfiguration;
        ExerciseForm = exerciseForm;
        SaveNewExerciseCommand.NotifyCanExecuteChangedWhen(ExerciseForm, nameof(ExerciseFormViewModel.CanSave));
    }

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = UiText.HeaderNewWorkout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsEmptyHintVisible))]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkoutCommand))]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(IsEmptyHintVisible))]
    [NotifyCanExecuteChangedFor(nameof(AddExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveWorkoutCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(SaveWorkoutCommand))]
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

    public ObservableCollection<WorkoutExerciseItem> Exercises { get; } = [];

    public ExercisePickerViewModel ExercisePicker { get; }

    public ExerciseConfigurationViewModel ExerciseConfiguration { get; }

    public ExerciseFormViewModel ExerciseForm { get; }

    public bool CanSave => IsEditable && !string.IsNullOrWhiteSpace(WorkoutName) && !IsEmpty;

    private bool CanSaveNewExercise => ExerciseForm.CanSave;

    private bool IsAnySheetOpen => IsExercisePickerOpen || IsExerciseFormOpen || IsConfigurationOpen;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutToEdit, out var editable) && editable is Workout workout)
        {
            _workoutToEdit = workout;
            HeaderTitle = UiText.HeaderEditWorkout;
            IsLoading = true;
            _errors.ReportFailures(LoadWorkoutAsync(workout));
        }
    }

    private async Task LoadWorkoutAsync(Workout workout)
    {
        var exercises = await _workoutRepository.GetExercisesAsync(workout.Id);

        WorkoutName = workout.Name;
        Exercises.ReplaceAll(exercises.Select(exercise => new WorkoutExerciseItem(exercise)));
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
        _navigation.Navigating += OnShellNavigating;
        _navigation.Navigated += OnShellNavigated;
    }

    [RelayCommand]
    private void StopGuardingUnsavedChanges()
    {
        if (!_isGuardingUnsavedChanges)
        {
            return;
        }

        _navigation.Navigating -= OnShellNavigating;
        _navigation.Navigated -= OnShellNavigated;
        _isGuardingUnsavedChanges = false;
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        if (!_navigationStack.ContainsPageBoundTo(this))
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
        _errors.ReportFailures(ConfirmLeavingEditorAsync(destination));
    }

    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        var discard = await _dialogs.ConfirmAsync(UiText.TitleUnsavedChanges, UiText.BodyUnsavedChangesConfirmation, UiText.ButtonDiscard, UiText.ButtonCancel);
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

        await _navigation.GoToAsync(NavigationRoutes.GoBack);

        if (destination is not null && _navigation.CurrentState.Location != destination.Location)
        {
            await _navigation.GoToAsync(destination);
        }
    }

    [RelayCommand]
    private Task NavigateBackAsync() => _errors.RunAsync(async () =>
    {
        if (IsAnySheetOpen)
        {
            CloseSheets();
            return;
        }

        if (await ConfirmDiscardAsync())
        {
            await _navigation.GoToAsync(NavigationRoutes.GoBack);
        }
    });

    private static bool IsLeavingEditor(ShellNavigatingEventArgs e) =>
        IsEditorLocation(e.Current) && !IsEditorLocation(e.Target) && !KeepsEditorOnStack(e);

    private static bool KeepsEditorOnStack(ShellNavigatingEventArgs e) =>
        e.Source is ShellNavigationSource.Push;

    private static bool IsEditorLocation(ShellNavigationState? state) =>
        state?.Location.OriginalString.Contains(NavigationRoutes.AddEditWorkout, StringComparison.Ordinal) == true;

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private Task AddExerciseAsync() => _errors.RunAsync(async () =>
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
    private Task PickExerciseAsync(Exercise exercise) => _errors.RunAsync(async () =>
    {
        await SheetTransition.CloseAsync(CloseExercisePicker);
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand]
    private Task CreateExerciseAsync() => _errors.RunAsync(async () =>
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

    [RelayCommand(CanExecute = nameof(CanSaveNewExercise))]
    private Task SaveNewExerciseAsync() => _errors.RunAsync(async () =>
    {
        var exercise = await ExerciseForm.SaveAsync();
        await SheetTransition.CloseAsync(CancelExerciseForm);
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand(CanExecute = nameof(IsEditable))]
    private void EditExercise(WorkoutExerciseItem item)
    {
        _exerciseToAdd = null;
        _exerciseBeingEdited = item;
        ExerciseConfiguration.BeginEdit(item.Exercise);
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
        if (_exerciseBeingEdited is { } item && Exercises.Remove(item))
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
        Exercises.Add(new WorkoutExerciseItem(new WorkoutExercise
        {
            ExerciseId = selectedExercise.Id,
            ExerciseName = selectedExercise.Name,
            Sets = configuration.Sets,
            Reps = configuration.Reps,
            BreakTimeInSeconds = configuration.BreakTimeInSeconds,
            TargetRPE = configuration.TargetRPE
        }));

        RefreshSummary();
        HasUnsavedChanges = true;
    }

    private void UpdateExerciseInWorkout(WorkoutExerciseItem item, ExerciseConfiguration configuration)
    {
        if (!Exercises.Contains(item))
        {
            return;
        }

        item.Apply(configuration);

        RefreshSummary();
        HasUnsavedChanges = true;
    }

    private void RefreshSummary()
    {
        ExerciseCount = Exercises.Count;
        IsEmpty = ExerciseCount == 0;
        TotalSets = Exercises.Sum(item => item.Sets);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveWorkoutAsync() => _errors.RunAsync(async () =>
    {
        var name = WorkoutName.Trim();
        List<WorkoutExercise> exercises = [.. Exercises.Select(item => item.Exercise)];

        if (_workoutToEdit is not null)
        {
            await _workoutRepository.UpdateWithExercisesAsync(new Workout { Id = _workoutToEdit.Id, Name = name }, exercises);
            _workoutToEdit.Name = name;
        }
        else
        {
            var workout = new Workout { Name = name };
            await _workoutRepository.SaveWithExercisesAsync(workout, exercises);
            _workoutToEdit = workout;
        }

        HasUnsavedChanges = false;
        await _navigation.GoToAsync(NavigationRoutes.GoBack);
    });
}
