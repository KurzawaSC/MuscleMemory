using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ExercisePickerViewModel : ObservableObject
{
    private readonly IExerciseCatalogService _exerciseCatalog;
    private List<ExerciseItem> _allExercises = [];

    public ExercisePickerViewModel(IExerciseCatalogService exerciseCatalog, ExerciseFilterViewModel filter)
    {
        _exerciseCatalog = exerciseCatalog;
        Filter = filter;
        Filter.Changed += (_, _) => ApplyFilter();
    }

    public ExerciseFilterViewModel Filter { get; }

    public ObservableCollection<ExerciseItem> Exercises { get; } = [];

    [ObservableProperty]
    public partial bool HasNoMatches { get; set; }

    public async Task LoadAsync()
    {
        var exercises = await _exerciseCatalog.GetAllAsync();
        _allExercises = [.. exercises.Select(ExerciseItem.Create)];
        Filter.Reset();
        Filter.UpdateFilters(exercises);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Exercises.ReplaceAll(Filter.Apply(_allExercises));
        HasNoMatches = _allExercises.Count > 0 && Exercises.Count == 0;
    }
}
