using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Extensions;
using MuscleMemory.Models;

namespace MuscleMemory.ViewModels;

public partial class ExerciseFilterViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial MuscleGroupFilter SelectedFilter { get; set; } = MuscleGroupFilter.All;

    public ObservableCollection<MuscleGroupFilter> Filters { get; } = [];

    public event EventHandler? Changed;

    partial void OnSearchTextChanged(string value) => Changed?.Invoke(this, EventArgs.Empty);

    partial void OnSelectedFilterChanged(MuscleGroupFilter value) => Changed?.Invoke(this, EventArgs.Empty);

    public void Reset()
    {
        SearchText = string.Empty;
        SelectedFilter = MuscleGroupFilter.All;
    }

    public void UpdateFilters(IEnumerable<Exercise> exercises)
    {
        List<MuscleGroupFilter> filters =
        [
            MuscleGroupFilter.All,
            .. exercises
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

    public IEnumerable<Exercise> Apply(IEnumerable<Exercise> exercises) =>
        exercises
            .Where(SelectedFilter.Matches)
            .Where(exercise => exercise.Name.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));
}
