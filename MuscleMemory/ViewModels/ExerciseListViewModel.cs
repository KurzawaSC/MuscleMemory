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

public partial class ExerciseListViewModel : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IHapticService _hapticService;
    private List<Exercise> _allExercises = [];

    public ExerciseListViewModel(
        IExerciseRepository exerciseRepository,
        ActiveWorkoutViewModel activeWorkout,
        AddEditExerciseViewModel exerciseForm,
        ExerciseFilterViewModel filter,
        IHapticService hapticService)
    {
        _exerciseRepository = exerciseRepository;
        _hapticService = hapticService;
        ActiveWorkout = activeWorkout;
        ExerciseForm = exerciseForm;
        Filter = filter;
        Filter.Changed += (_, _) => ApplyFilter();
    }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

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

    [RelayCommand]
    private async Task LoadExercisesAsync()
    {
        _allExercises = await _exerciseRepository.GetAllAsync();
        IsEmpty = _allExercises.Count == 0;
        Filter.UpdateFilters(_allExercises);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Exercises.ReplaceAll(Filter.Apply(_allExercises));
        HasNoMatches = !IsEmpty && Exercises.Count == 0;
    }

    [RelayCommand]
    private void AddExercise()
    {
        ExerciseForm.BeginNew();
        IsExerciseFormOpen = true;
    }

    [RelayCommand]
    private async Task SaveExerciseAsync()
    {
        if (!ExerciseForm.CanSave)
        {
            return;
        }

        await ExerciseForm.SaveAsync();
        IsExerciseFormOpen = false;
        await LoadExercisesAsync();
    }

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
        _hapticService.Click();
        ShowExerciseActions(exercise);
    }

    [RelayCommand]
    private void CancelExerciseActions()
    {
        IsActionSheetOpen = false;
    }

    [RelayCommand]
    private async Task EditActionExerciseAsync()
    {
        if (await DismissActionSheetAsync() is not { } exercise)
        {
            return;
        }

        ExerciseForm.BeginEdit(exercise);
        IsExerciseFormOpen = true;
    }

    [RelayCommand]
    private async Task ViewActionExerciseHistoryAsync()
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
        await Shell.Current.GoToAsync(nameof(ExerciseHistoryPage), navigationParameter);
    }

    [RelayCommand]
    private async Task DeleteActionExerciseAsync()
    {
        if (await DismissActionSheetAsync() is not { } exercise)
        {
            return;
        }

        bool answer = await Shell.Current.DisplayAlertAsync(UiText.TitleDeleteExercise, string.Format(UiText.DeleteConfirmationFormat, exercise.Name), UiText.ButtonYes, UiText.ButtonNo);
        if (answer)
        {
            await _exerciseRepository.DeleteAsync(exercise.Id);
            await LoadExercisesAsync();
        }
    }

    private async Task<Exercise?> DismissActionSheetAsync()
    {
        var exercise = ActionExercise;
        IsActionSheetOpen = false;
        await Task.Delay(TimeSpan.FromMilliseconds(UiTiming.SheetCloseMilliseconds));
        return exercise;
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsExerciseFormOpen = false;
        IsActionSheetOpen = false;
    }
}
