using CommunityToolkit.Mvvm.ComponentModel;
using MuscleMemory.Models;
using System.Collections.ObjectModel;

namespace MuscleMemory.ViewModels;

public partial class SelectExerciseViewModel : ObservableObject
{
    private readonly List<Exercise> _allExercises;

    [ObservableProperty]
    public partial ObservableCollection<Exercise> FilteredExercises { get; set; } = new();

    public List<string> MuscleGroupFilters { get; } = new List<string> { "All" };

    [ObservableProperty]
    public partial string SelectedMuscleGroupFilter { get; set; } = "All";

    public SelectExerciseViewModel(List<Exercise> availableExercises)
    {
        _allExercises = availableExercises;
        
        foreach (var mg in Enum.GetValues(typeof(MuscleGroup)))
        {
            MuscleGroupFilters.Add(mg.ToString()!);
        }

        FilterExercises();
    }

    partial void OnSelectedMuscleGroupFilterChanged(string value)
    {
        FilterExercises();
    }

    private void FilterExercises()
    {
        FilteredExercises.Clear();
        var filtered = _allExercises.AsEnumerable();

        if (SelectedMuscleGroupFilter != "All" && Enum.TryParse<MuscleGroup>(SelectedMuscleGroupFilter, out var selectedMg))
        {
            filtered = filtered.Where(e => e.TargetMuscleGroup == selectedMg);
        }

        foreach (var exercise in filtered)
        {
            FilteredExercises.Add(exercise);
        }
    }
}
