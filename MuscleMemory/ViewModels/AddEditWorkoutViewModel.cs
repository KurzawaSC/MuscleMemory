using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;
using MuscleMemory.Services;
using MuscleMemory.Views;

namespace MuscleMemory.ViewModels;

public partial class AddEditWorkoutViewModel(
    IWorkoutRepository workoutRepository,
    SelectExerciseViewModel exercisePicker,
    ConfigureExerciseViewModel exerciseConfiguration,
    AddEditExerciseViewModel exerciseForm,
    IDialogService dialogs,
    IErrorHandler errors) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutRepository _workoutRepository = workoutRepository;
    private readonly IDialogService _dialogs = dialogs;
    private readonly IErrorHandler _errors = errors;
    private Workout? _workoutToEdit;
    private Exercise? _exerciseToAdd;
    private WorkoutExercise? _exerciseBeingEdited;

    [ObservableProperty]
    public partial string HeaderTitle { get; set; } = UiText.HeaderNewWorkout;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExerciseCountCaption))]
    public partial int ExerciseCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalSetsCaption))]
    public partial int TotalSets { get; set; }

    public string ExerciseCountCaption => ExerciseCount == 1 ? UiText.CaptionExercise : UiText.CaptionExercises;

    public string TotalSetsCaption => TotalSets == 1 ? UiText.CaptionSet : UiText.CaptionSets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string WorkoutName { get; set; } = string.Empty;

    partial void OnWorkoutNameChanged(string value)
    {
        HasUnsavedChanges = true;
    }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; } = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsExercisePickerOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsExerciseFormOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsConfigurationOpen { get; set; }

    public ObservableCollection<WorkoutExercise> Exercises { get; } = [];

    public SelectExerciseViewModel ExercisePicker { get; } = exercisePicker;

    public ConfigureExerciseViewModel ExerciseConfiguration { get; } = exerciseConfiguration;

    public AddEditExerciseViewModel ExerciseForm { get; } = exerciseForm;

    public bool CanSave => !string.IsNullOrWhiteSpace(WorkoutName) && !IsEmpty;

    private bool IsAnySheetOpen => IsExercisePickerOpen || IsExerciseFormOpen || IsConfigurationOpen;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.WorkoutToEdit, out var editable) && editable is Workout workout)
        {
            _workoutToEdit = workout;
            HeaderTitle = UiText.HeaderEditWorkout;
            _ = LoadWorkoutAsync(workout);
        }
    }

    private async Task LoadWorkoutAsync(Workout workout)
    {
        WorkoutName = workout.Name;

        Exercises.ReplaceAll(await _workoutRepository.GetExercisesAsync(workout.Id));

        RefreshSummary();
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

    private async Task ConfirmLeavingEditorAsync(ShellNavigationState? destination)
    {
        bool discard = await _dialogs.ConfirmAsync(UiText.TitleUnsavedChanges, UiText.BodyUnsavedChangesConfirmation, UiText.ButtonDiscard, UiText.ButtonCancel);
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

    [RelayCommand]
    private Task GoBackAsync() =>
        _errors.RunAsync(() => Shell.Current.GoToAsync(NavigationRoutes.GoBack));

    private static bool IsLeavingEditor(ShellNavigatingEventArgs e) =>
        IsEditorLocation(e.Current) && !IsEditorLocation(e.Target) && !KeepsEditorOnStack(e);

    private static bool KeepsEditorOnStack(ShellNavigatingEventArgs e) =>
        e.Source is ShellNavigationSource.Push;

    private static bool IsEditorLocation(ShellNavigationState? state) =>
        state?.Location.OriginalString.Contains(nameof(AddEditWorkoutPage), StringComparison.Ordinal) == true;

    [RelayCommand]
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
        IsExercisePickerOpen = false;
        await WaitForSheetToCloseAsync();
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand]
    private Task CreateExerciseAsync() => _errors.RunAsync(async () =>
    {
        IsExercisePickerOpen = false;
        await WaitForSheetToCloseAsync();
        await ExerciseForm.BeginNewAsync();
        IsExerciseFormOpen = true;
    });

    [RelayCommand]
    private void CancelExerciseForm()
    {
        IsExerciseFormOpen = false;
    }

    [RelayCommand]
    private Task SaveNewExerciseAsync() => _errors.RunAsync(async () =>
    {
        if (!ExerciseForm.CanSave)
        {
            return;
        }

        var exercise = await ExerciseForm.SaveAsync();
        IsExerciseFormOpen = false;
        await WaitForSheetToCloseAsync();
        OpenConfigurationForNewExercise(exercise);
    });

    [RelayCommand]
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

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
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

    private static Task WaitForSheetToCloseAsync() =>
        Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));

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
    private Task SaveWorkoutAsync() => _errors.RunAsync(async () =>
    {
        if (!CanSave)
        {
            return;
        }

        if (_workoutToEdit != null)
        {
            _workoutToEdit.Name = WorkoutName.Trim();
            await _workoutRepository.UpdateWithExercisesAsync(_workoutToEdit, [.. Exercises]);
        }
        else
        {
            await _workoutRepository.SaveWithExercisesAsync(new Workout { Name = WorkoutName.Trim() }, [.. Exercises]);
        }

        HasUnsavedChanges = false;
        await Shell.Current.GoToAsync(NavigationRoutes.GoBack);
    });
}
