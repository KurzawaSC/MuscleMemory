using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;

namespace MuscleMemory.ViewModels;

public partial class ExercisePickerViewModel : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository;
    private List<ExerciseItem> _allExercises = [];

    public ExercisePickerViewModel(IExerciseRepository exerciseRepository, ExerciseFilterViewModel filter)
    {
        _exerciseRepository = exerciseRepository;
        Filter = filter;
        Filter.Changed += (_, _) => ApplyFilter();
    }

    public ExerciseFilterViewModel Filter { get; }

    public ObservableCollection<ExerciseItem> Exercises { get; } = [];

    [ObservableProperty]
    public partial bool HasNoMatches { get; set; }

    public async Task LoadAsync()
    {
        var exercises = await _exerciseRepository.GetAllAsync();
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
