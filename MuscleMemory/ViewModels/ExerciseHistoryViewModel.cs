using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using MuscleMemory.Constants;
using MuscleMemory.Extensions;
using MuscleMemory.Services;

namespace MuscleMemory.ViewModels;

public partial class ExerciseHistoryViewModel(IWorkoutHistoryQueryService historyQueryService) : ObservableObject, IQueryAttributable
{
    private readonly IWorkoutHistoryQueryService _historyQueryService = historyQueryService;
    private int _exerciseId;

    [ObservableProperty]
    public partial string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    public ObservableCollection<ExerciseHistoryEntry> History { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(QueryKeys.ExerciseName, out var name) && name is string exerciseName)
        {
            ExerciseName = exerciseName;
        }

        if (query.TryGetValue(QueryKeys.ExerciseId, out var id) && id is int exerciseId && exerciseId > 0)
        {
            _exerciseId = exerciseId;
            _ = LoadHistoryAsync();
        }
    }

    private async Task LoadHistoryAsync()
    {
        History.ReplaceAll(await _historyQueryService.GetExerciseHistoryAsync(_exerciseId));
        IsEmpty = !History.Any();
    }
}
