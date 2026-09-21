using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class SelectExerciseViewModel : ObservableObject
{
    private readonly IExerciseRepository _exerciseRepository;
    private List<Exercise> _allExercises = [];

    public SelectExerciseViewModel(IExerciseRepository exerciseRepository, ExerciseFilterViewModel filter)
    {
        _exerciseRepository = exerciseRepository;
        Filter = filter;
        Filter.Changed += (_, _) => ApplyFilter();
    }

    public ExerciseFilterViewModel Filter { get; }

    public ObservableCollection<Exercise> Exercises { get; } = [];

    public async Task LoadAsync()
    {
        _allExercises = await _exerciseRepository.GetAllAsync();
        Filter.Reset();
        Filter.UpdateFilters(_allExercises);
        ApplyFilter();
    }

    private void ApplyFilter() => Exercises.ReplaceAll(Filter.Apply(_allExercises));
}
