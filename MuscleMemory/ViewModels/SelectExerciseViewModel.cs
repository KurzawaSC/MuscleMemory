using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuscleMemory.Constants;
using MuscleMemory.Models;
using System.Collections.ObjectModel;

namespace MuscleMemory.ViewModels;

public partial class SelectExerciseViewModel(IPopupService popupService) : ObservableObject, IQueryAttributable
{
    private const string MuscleGroupFilterAll = "All";

    private readonly IPopupService _popupService = popupService;

    private List<Exercise> _allExercises = [];

    public ObservableCollection<Exercise> FilteredExercises { get; } = [];

    public List<string> MuscleGroupFilters { get; } = [MuscleGroupFilterAll, .. Enum.GetValues<MuscleGroup>().Select(muscleGroup => muscleGroup.ToString())];

    [ObservableProperty]
    public partial string SelectedMuscleGroupFilter { get; set; } = MuscleGroupFilterAll;

    [ObservableProperty]
    public partial Exercise? SelectedExercise { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.AvailableExercises, out var available) && available is List<Exercise> exercises)
        {
            _allExercises = exercises;
            FilterExercises();
        }
    }

    partial void OnSelectedMuscleGroupFilterChanged(string value)
    {
        FilterExercises();
    }

    private void FilterExercises()
    {
        FilteredExercises.Clear();
        var filtered = _allExercises.AsEnumerable();

        if (SelectedMuscleGroupFilter != MuscleGroupFilterAll && Enum.TryParse<MuscleGroup>(SelectedMuscleGroupFilter, out var selectedMg))
        {
            filtered = filtered.Where(e => e.TargetMuscleGroup == selectedMg);
        }

        foreach (var exercise in filtered)
        {
            FilteredExercises.Add(exercise);
        }
    }

    [RelayCommand]
    private async Task ConfirmSelectionAsync()
    {
        if (SelectedExercise is not Exercise chosen)
        {
            return;
        }

        SelectedExercise = null;
        await Task.Delay(UiTiming.PopupSelectionCloseDelayMilliseconds);
        await _popupService.ClosePopupAsync<Exercise?>(Shell.Current.Navigation, chosen);
    }
}
