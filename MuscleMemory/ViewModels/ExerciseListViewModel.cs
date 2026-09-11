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

public partial class ExerciseListViewModel(
    IExerciseRepository exerciseRepository,
    ActiveWorkoutViewModel activeWorkout,
    AddEditExerciseViewModel exerciseForm,
    IHapticService hapticService) : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository = exerciseRepository;
    private readonly IHapticService _hapticService = hapticService;
    private List<Exercise> _allExercises = [];

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [ObservableProperty]
    public partial bool HasNoMatches { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroupFilter SelectedFilter { get; set; } = MuscleGroupFilter.All;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsExerciseFormOpen { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSheetsCommand))]
    public partial bool IsActionSheetOpen { get; set; }

    [ObservableProperty]
    public partial Exercise? ActionExercise { get; set; }

    public ObservableCollection<Exercise> Exercises { get; } = [];

    public ObservableCollection<MuscleGroupFilter> Filters { get; } = [];

    public AddEditExerciseViewModel ExerciseForm { get; } = exerciseForm;

    public ActiveWorkoutViewModel ActiveWorkout { get; } = activeWorkout;

    private bool IsAnySheetOpen => IsExerciseFormOpen || IsActionSheetOpen;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedFilterChanged(MuscleGroupFilter value) => ApplyFilter();

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
        RebuildFilters();
        ApplyFilter();
    }

    private void RebuildFilters()
    {
        List<MuscleGroupFilter> filters =
        [
            MuscleGroupFilter.All,
            .. _allExercises
                .Select(exercise => exercise.TargetMuscleGroup)
                .Distinct()
                .Order()
                .Select(MuscleGroupFilter.For)
        ];

        if (!Filters.SequenceEqual(filters))
        {
            Filters.ReplaceAll(filters);
        }

        if (!Filters.Contains(SelectedFilter))
        {
            SelectedFilter = MuscleGroupFilter.All;
        }
    }

    private void ApplyFilter()
    {
        var matches = _allExercises
            .Where(SelectedFilter.Matches)
            .Where(exercise => exercise.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

        Exercises.ReplaceAll(matches);
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
        await Task.Delay((int)UiTiming.SheetCloseMilliseconds);
        return exercise;
    }

    [RelayCommand(CanExecute = nameof(IsAnySheetOpen))]
    private void CloseSheets()
    {
        IsExerciseFormOpen = false;
        IsActionSheetOpen = false;
    }
}
